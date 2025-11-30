using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using Microsoft.Win32;
using System;
using System.Linq;

using DynViewer.Services;

namespace DynViewer;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window
{
    const double NodeWidth = 160;
    const double NodeHeight = 60;
    const double GridX = 240;
    const double GridY = 140;

    public MainWindow()
    {
        InitializeComponent();
    }

    private Point _lastMousePosition;
    private bool _isDragging;

    private void GraphCanvas_MouseWheel(object sender, MouseWheelEventArgs e)
    {
        var matrix = GraphTransform.Matrix;
        var scale = e.Delta > 0 ? 1.1 : 0.9;

        // Get position relative to the container (Border), not the transformed Canvas
        var position = e.GetPosition((IInputElement)sender);

        matrix.ScaleAt(scale, scale, position.X, position.Y);
        GraphTransform.Matrix = matrix;
    }

    private void GraphCanvas_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Middle)
        {
            _lastMousePosition = e.GetPosition(this);
            _isDragging = true;
            Cursor = Cursors.SizeAll;
        }
    }

    private void GraphCanvas_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        // Optional: Allow left click drag if not clicking on a node (hit testing is complex, so maybe just stick to middle or ctrl+left)
        // For now, let's enable left drag for simplicity as we don't have node selection yet
        _lastMousePosition = e.GetPosition(this);
        _isDragging = true;
        GraphCanvas.CaptureMouse();
        Cursor = Cursors.Hand;
    }

    private void GraphCanvas_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        _isDragging = false;
        GraphCanvas.ReleaseMouseCapture();
        Cursor = Cursors.Arrow;
    }

    private void GraphCanvas_MouseMove(object sender, MouseEventArgs e)
    {
        if (_isDragging)
        {
            var currentPosition = e.GetPosition(this);
            var offset = currentPosition - _lastMousePosition;
            _lastMousePosition = currentPosition;

            var matrix = GraphTransform.Matrix;
            matrix.Translate(offset.X, offset.Y);
            GraphTransform.Matrix = matrix;
        }
        
        // Handle Middle button release if it happened outside
        if (e.MiddleButton == MouseButtonState.Released && Cursor == Cursors.SizeAll)
        {
            _isDragging = false;
            Cursor = Cursors.Arrow;
        }
    }

    private void OpenDyn_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Filter = "Dynamo graph (*.dyn)|*.dyn|JSON (*.json)|*.json|All files (*.*)|*.*"
        };
        if (dlg.ShowDialog() == true)
        {
            try
            {
                var graph = DynLoader.Load(dlg.FileName);
                StatusText.Text = $"Loaded: {System.IO.Path.GetFileName(dlg.FileName)} | Nodes: {graph.Nodes.Count}, Connectors: {graph.Connectors.Count}";
                RenderGraph(graph);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    private void RenderGraph(DynGraph graph)
    {
        GraphCanvas.Children.Clear();
        
        // Reset Transform
        GraphTransform.Matrix = Matrix.Identity;

        // 2. Draw Nodes
        foreach (var n in graph.Nodes)
        {
            // Calculate dynamic width based on text length
            double maxInWidth = 0;
            foreach (var p in n.InPorts) maxInWidth = Math.Max(maxInWidth, (p.Name?.Length ?? 0) * 7); // Approx 7px per char

            double maxOutWidth = 0;
            foreach (var p in n.OutPorts) maxOutWidth = Math.Max(maxOutWidth, (p.Name?.Length ?? 0) * 7);

            double titleWidth = (string.IsNullOrWhiteSpace(n.NickName) ? n.Name.Length : n.NickName.Length) * 8 + 30;

            double currentWidth = Math.Max(NodeWidth, Math.Max(titleWidth, maxInWidth + maxOutWidth + 40));

            // Main Node Body
            var nodeGroup = new Canvas();
            Canvas.SetLeft(nodeGroup, n.X);
            Canvas.SetTop(nodeGroup, n.Y);

            // Calculate height based on ports AND content
            double portsHeight = (Math.Max(n.InPorts.Count, n.OutPorts.Count) * 20) + 40;
            double contentHeight = 0;
            if (!string.IsNullOrEmpty(n.Code) || !string.IsNullOrEmpty(n.InputValue)) contentHeight = 40; // minimal extra space
            
            var nodeRect = new Rectangle
            {
                Width = currentWidth,
                Height = Math.Max(NodeHeight, portsHeight + contentHeight),
                RadiusX = 4,
                RadiusY = 4,
                Fill = new SolidColorBrush(Color.FromRgb(50, 50, 50)), // Darker background
                Stroke = new SolidColorBrush(Color.FromRgb(100, 100, 100)),
                StrokeThickness = 1,
                Effect = new System.Windows.Media.Effects.DropShadowEffect { BlurRadius = 10, ShadowDepth = 2, Opacity = 0.3 }
            };
            nodeGroup.Children.Add(nodeRect);

            // Header Background
            var headerRect = new Rectangle
            {
                Width = currentWidth,
                Height = 25,
                RadiusX = 4,
                RadiusY = 4,
                Fill = new SolidColorBrush(Color.FromRgb(70, 70, 70)),
                VerticalAlignment = VerticalAlignment.Top
            };
            // Clip the bottom to make it square there
            headerRect.Clip = new RectangleGeometry(new Rect(0, 0, currentWidth, 25));
            nodeGroup.Children.Add(headerRect);

            // Title
            var title = new TextBlock
            {
                Text = string.IsNullOrWhiteSpace(n.NickName) ? n.Name : n.NickName,
                Foreground = Brushes.WhiteSmoke,
                FontSize = 12,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(8, 5, 8, 0),
                TextTrimming = TextTrimming.CharacterEllipsis,
                Width = currentWidth - 16
            };
            nodeGroup.Children.Add(title);

            // Content (Code or InputValue)
            if (!string.IsNullOrEmpty(n.Code) || !string.IsNullOrEmpty(n.InputValue))
            {
                var contentText = !string.IsNullOrEmpty(n.Code) ? n.Code : n.InputValue;
                var contentBlock = new TextBlock
                {
                    Text = contentText,
                    Foreground = Brushes.LightGreen,
                    FontSize = 11,
                    FontFamily = new FontFamily("Consolas"),
                    Margin = new Thickness(8, 30, 8, 0),
                    TextTrimming = TextTrimming.CharacterEllipsis,
                    MaxHeight = 60,
                    TextWrapping = TextWrapping.Wrap,
                    Width = currentWidth - 16
                };
                nodeGroup.Children.Add(contentBlock);
            }

            // Ports
            double portStartY = 35 + contentHeight; // Push ports down if there is content
            
            // Inputs
            for (int i = 0; i < n.InPorts.Count; i++)
            {
                var p = n.InPorts[i];
                double py = portStartY + (i * 20);
                
                // Port shape
                var portRect = new Rectangle
                {
                    Width = 10, Height = 10,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(portRect, -5);
                Canvas.SetTop(portRect, py);
                nodeGroup.Children.Add(portRect);

                // Port Name
                var portName = new TextBlock
                {
                    Text = p.Name,
                    Foreground = Brushes.LightGray,
                    FontSize = 10,
                    Margin = new Thickness(8, py - 2, 0, 0)
                };
                nodeGroup.Children.Add(portName);
            }

            // Outputs
            for (int i = 0; i < n.OutPorts.Count; i++)
            {
                var p = n.OutPorts[i];
                double py = portStartY + (i * 20);

                // Port shape
                var portRect = new Rectangle
                {
                    Width = 10, Height = 10,
                    Fill = Brushes.LightGray,
                    Stroke = Brushes.Gray,
                    StrokeThickness = 1
                };
                Canvas.SetLeft(portRect, currentWidth - 5);
                Canvas.SetTop(portRect, py);
                nodeGroup.Children.Add(portRect);

                // Port Name
                var portName = new TextBlock
                {
                    Text = p.Name,
                    Foreground = Brushes.LightGray,
                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    TextAlignment = TextAlignment.Right,
                    Width = currentWidth - 15
                };
                Canvas.SetLeft(portName, 5);
                Canvas.SetTop(portName, py - 2);
                nodeGroup.Children.Add(portName);
            }

            GraphCanvas.Children.Add(nodeGroup);
        }

        // 3. Draw Connectors (Bezier)
        foreach (var c in graph.Connectors)
        {
            var from = graph.Nodes.FirstOrDefault(n => n.Id == c.StartNodeId);
            var to = graph.Nodes.FirstOrDefault(n => n.Id == c.EndNodeId);
            if (from == null || to == null) continue;

            // Re-calculate width for 'from' node to find startX correctly
            // (Ideally we should store the calculated width in the view model, but recalculating here is cheap enough)
            double maxInWidth = 0;
            foreach (var p in from.InPorts) maxInWidth = Math.Max(maxInWidth, (p.Name?.Length ?? 0) * 7);
            double maxOutWidth = 0;
            foreach (var p in from.OutPorts) maxOutWidth = Math.Max(maxOutWidth, (p.Name?.Length ?? 0) * 7);
            double titleWidth = (string.IsNullOrWhiteSpace(from.NickName) ? from.Name.Length : from.NickName.Length) * 8 + 30;
            double fromNodeWidth = Math.Max(NodeWidth, Math.Max(titleWidth, maxInWidth + maxOutWidth + 40));


            // Calculate port positions
            // Default to center if index out of range (shouldn't happen often)
            double contentHeight = (!string.IsNullOrEmpty(from.Code) || !string.IsNullOrEmpty(from.InputValue)) ? 40 : 0;
            double startY = from.Y + 35 + contentHeight + (c.StartIndex * 20) + 5; 
            double startX = from.X + fromNodeWidth;

            contentHeight = (!string.IsNullOrEmpty(to.Code) || !string.IsNullOrEmpty(to.InputValue)) ? 40 : 0;
            double endY = to.Y + 35 + contentHeight + (c.EndIndex * 20) + 5;
            double endX = to.X;

            var path = new Path
            {
                Stroke = new SolidColorBrush(Color.FromRgb(180, 180, 180)),
                StrokeThickness = 2,
                Opacity = 0.8
            };

            var geometry = new PathGeometry();
            var figure = new PathFigure { StartPoint = new Point(startX, startY) };
            
            // Cubic Bezier
            double dist = Math.Abs(endX - startX) / 2;
            // Ensure a minimum curve distance so it doesn't look flat for close nodes
            if (dist < 50) dist = 50; 

            var p1 = new Point(startX + dist, startY);
            var p2 = new Point(endX - dist, endY);
            var p3 = new Point(endX, endY);

            figure.Segments.Add(new BezierSegment(p1, p2, p3, true));
            geometry.Figures.Add(figure);
            path.Data = geometry;

            // Add to canvas - insert at 0 to be behind nodes
            GraphCanvas.Children.Insert(0, path);
        }
    }
}
