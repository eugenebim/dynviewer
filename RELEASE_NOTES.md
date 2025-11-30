# DynViewer v1.0.0 Release Notes

We are excited to announce the first release of **DynViewer**, a standalone viewer for Dynamo `.dyn` files.

## 🚀 Features

### 1. Standalone Viewing
- View `.dyn` files without installing Dynamo or Revit.
- Lightweight and fast, built with WPF and .NET.

### 2. Authentic Visualization
- **Nodes**: Renders nodes with accurate colors, headers, and ports.
- **Connectors**: Draws cubic Bezier curves for wires, mimicking the Dynamo look and feel.
- **Content**: Displays Code Block content and Input Node values directly in the graph.

### 3. Navigation
- **Zoom**: Use the Mouse Wheel to zoom in and out, centered on your cursor.
- **Pan**: Click and drag (Left or Middle mouse button) to navigate large graphs.
- **Infinite Canvas**: No size limits, the canvas expands as needed.

### 4. Compatibility
- Supports both **Dynamo 2.x** (modern) and older file schemas.
- Handles missing node names by falling back to View data.
- Supports both Object-based and String-based (Port ID) connector definitions.

## 📝 License Information

This project is an independent implementation and does not contain source code from the DynamoDS/Dynamo repository.
- **License**: Apache License 2.0
- **Third-Party**: Uses `System.Text.Json` for parsing.

## 📦 Installation

1. **Prerequisite**: Install **[.NET 8 Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/8.0)** (Windows x64).
2. Download `DynViewer.exe`.
3. Run the application.
