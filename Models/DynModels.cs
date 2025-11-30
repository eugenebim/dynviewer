using System.Collections.Generic;

namespace DynViewer
{
    // Minimal shapes of Dynamo's JSON schema for nodes/connectors
    public class DynGraph
    {
        public string Uuid { get; set; } = "";
        public string Name { get; set; } = "";
        public List<DynNode> Nodes { get; set; } = new();
        public List<DynConnector> Connectors { get; set; } = new();
        
        // View information (coordinates, zoom, etc.)
        public DynView View { get; set; } = new();
    }

    public class DynNode
    {
        public string Id { get; set; } = "";
        public string Name { get; set; } = "";
        public string NickName { get; set; } = "";
        public string NodeType { get; set; } = "";
        public string Code { get; set; } = "";       // For Code Blocks
        public string InputValue { get; set; } = ""; // For Input Nodes
        
        // Logical ports
        public List<DynPort> InPorts { get; set; } = new();
        public List<DynPort> OutPorts { get; set; } = new();

        // View data linked by Id
        public double X { get; set; }
        public double Y { get; set; }
    }

    public class DynPort
    {
        public string Name { get; set; } = "";
        public int Index { get; set; }
        public string Id { get; set; } = ""; // Some versions have Port IDs
    }

    public class DynConnector
    {
        public string StartNodeId { get; set; } = "";
        public int StartIndex { get; set; }
        public string EndNodeId { get; set; } = "";
        public int EndIndex { get; set; }
    }

    // View section of the JSON
    public class DynView
    {
        public List<DynNodeView> NodeViews { get; set; } = new();
        public double X { get; set; }
        public double Y { get; set; }
        public double Zoom { get; set; } = 1.0;
    }

    public class DynNodeView
    {
        public string Id { get; set; } = ""; // Matches Node.Id
        public double X { get; set; }
        public double Y { get; set; }
        public bool IsCollapsed { get; set; }
    }
}
