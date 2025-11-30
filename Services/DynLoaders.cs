using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DynViewer.Services
{
    public static class DynLoader
    {
        // Minimal reader that covers common Dynamo 2.x graphs
        public static DynGraph Load(string path)
        {
            var json = JsonNode.Parse(File.ReadAllText(path))
                       ?? throw new InvalidOperationException("Invalid JSON");

            var graph = new DynGraph();
            graph.Uuid = json["Uuid"]?.ToString() ?? "";
            graph.Name = json["Name"]?.ToString() ?? "";

            // 1) Nodes
            var nodesArray = json["Nodes"] as JsonArray;
            
            // Helper map for Port ID -> (NodeId, Index)
            var portLookup = new Dictionary<string, (string NodeId, int Index)>();

            if (nodesArray != null)
            {
                foreach (var node in nodesArray.OfType<JsonNode>())
                {
                    var dn = new DynNode
                    {
                        Id = node["Id"]?.ToString() ?? "",
                        Name = node["Name"]?.ToString() ?? "",
                        NickName = node["Nickname"]?.ToString() ?? node["NickName"]?.ToString() ?? "",
                        NodeType = node["NodeType"]?.ToString() ?? "",
                        Code = node["Code"]?.ToString() ?? "",
                        InputValue = node["InputValue"]?.ToString() ?? ""
                    };

                    // Ports (optional)
                    var inPorts = node["Inputs"] as JsonArray ?? node["InPorts"] as JsonArray;
                    var outPorts = node["Outputs"] as JsonArray ?? node["OutPorts"] as JsonArray;

                    if (inPorts != null)
                    {
                        int idx = 0;
                        foreach (var p in inPorts.OfType<JsonNode>())
                        {
                            var pid = p["Id"]?.ToString() ?? "";
                            var pName = p["Name"]?.ToString();
                            if (string.IsNullOrWhiteSpace(pName)) pName = p["Description"]?.ToString(); // Fallback
                            
                            dn.InPorts.Add(new DynPort
                            {
                                Name = pName ?? $"In{idx}",
                                Index = idx,
                                Id = pid
                            });
                            if (!string.IsNullOrEmpty(pid)) portLookup[pid] = (dn.Id, idx);
                            idx++;
                        }
                    }
                    if (outPorts != null)
                    {
                        int idx = 0;
                        foreach (var p in outPorts.OfType<JsonNode>())
                        {
                            var pid = p["Id"]?.ToString() ?? "";
                            var pName = p["Name"]?.ToString();
                            if (string.IsNullOrWhiteSpace(pName)) pName = p["Description"]?.ToString(); // Fallback

                            dn.OutPorts.Add(new DynPort
                            {
                                Name = pName ?? $"Out{idx}",
                                Index = idx,
                                Id = pid
                            });
                            if (!string.IsNullOrEmpty(pid)) portLookup[pid] = (dn.Id, idx);
                            idx++;
                        }
                    }

                    graph.Nodes.Add(dn);
                }
            }

            // 2) Connectors
            var connsArray = json["Connectors"] as JsonArray;
            if (connsArray != null)
            {
                foreach (var c in connsArray.OfType<JsonNode>())
                {
                    var start = c["Start"];
                    var end = c["End"];
                    if (start == null || end == null) continue;

                    string startNodeId = "", endNodeId = "";
                    int startIndex = 0, endIndex = 0;

                    // Check if Start is an object (Old Schema) or String (New Schema - Port ID)
                    if (start.GetValueKind() == JsonValueKind.Object)
                    {
                        startNodeId = start["NodeId"]?.ToString() ?? start["Guid"]?.ToString() ?? "";
                        startIndex = start["Index"]?.GetValue<int>() ?? 0;
                    }
                    else if (start.GetValueKind() == JsonValueKind.String)
                    {
                        var pid = start.ToString();
                        if (portLookup.TryGetValue(pid, out var info))
                        {
                            startNodeId = info.NodeId;
                            startIndex = info.Index;
                        }
                    }

                    if (end.GetValueKind() == JsonValueKind.Object)
                    {
                        endNodeId = end["NodeId"]?.ToString() ?? end["Guid"]?.ToString() ?? "";
                        endIndex = end["Index"]?.GetValue<int>() ?? 0;
                    }
                    else if (end.GetValueKind() == JsonValueKind.String)
                    {
                        var pid = end.ToString();
                        if (portLookup.TryGetValue(pid, out var info))
                        {
                            endNodeId = info.NodeId;
                            endIndex = info.Index;
                        }
                    }

                    if (!string.IsNullOrEmpty(startNodeId) && !string.IsNullOrEmpty(endNodeId))
                    {
                        graph.Connectors.Add(new DynConnector
                        {
                            StartNodeId = startNodeId,
                            StartIndex = startIndex,
                            EndNodeId = endNodeId,
                            EndIndex = endIndex
                        });
                    }
                }
            }
            else
            {
                // Some versions: connectors nested under "Connectors": [{Start:..., End:...}]
                var ws = json["Workspace"] ?? json["View"] ?? json;
                var connectors = ws?["Connectors"] as JsonArray;
                if (connectors != null)
                {
                    foreach (var c in connectors.OfType<JsonNode>())
                    {
                        var start = c["Start"];
                        var end = c["End"];
                        if (start == null || end == null) continue;

                        graph.Connectors.Add(new DynConnector
                        {
                            StartNodeId = start["NodeId"]?.ToString() ?? start["Guid"]?.ToString() ?? "",
                            StartIndex = start["Index"]?.GetValue<int>() ?? 0,
                            EndNodeId = end["NodeId"]?.ToString() ?? end["Guid"]?.ToString() ?? "",
                            EndIndex = end["Index"]?.GetValue<int>() ?? 0
                        });
                    }
                }
            }

            // 3) View (Coordinates)
            var viewJson = json["View"];
            if (viewJson != null)
            {
                graph.View.X = viewJson["X"]?.GetValue<double>() ?? 0;
                graph.View.Y = viewJson["Y"]?.GetValue<double>() ?? 0;
                graph.View.Zoom = viewJson["Zoom"]?.GetValue<double>() ?? 1.0;

                var nodeViews = viewJson["NodeViews"] as JsonArray;
                if (nodeViews != null)
                {
                    foreach (var nv in nodeViews.OfType<JsonNode>())
                    {
                        var id = nv["Id"]?.ToString() ?? "";
                        var x = nv["X"]?.GetValue<double>() ?? 0;
                        var y = nv["Y"]?.GetValue<double>() ?? 0;
                        var collapsed = nv["IsCollapsed"]?.GetValue<bool>() ?? false;
                        var viewName = nv["Name"]?.ToString() ?? ""; // Name from View

                        graph.View.NodeViews.Add(new DynNodeView
                        {
                            Id = id,
                            X = x,
                            Y = y,
                            IsCollapsed = collapsed
                        });

                        // Apply to node directly for easier access
                        var node = graph.Nodes.FirstOrDefault(n => n.Id == id);
                        if (node != null)
                        {
                            node.X = x;
                            node.Y = y;
                            // If Node didn't have a name (newer format), use the one from View
                            if (string.IsNullOrWhiteSpace(node.Name))
                            {
                                node.Name = viewName;
                            }
                        }
                    }
                }
            }

            return graph;
        }
    }
}
