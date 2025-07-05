using Godot;
using System;
using System.Collections.Generic;

public partial class CoordinateSystemController : Node3D
{
    [Export] public PackedScene PointMarkerScene;
    [Export] public Material LineMaterial;
    [Export] public Material GridMaterial;
    [Export] public float SystemSize = 50.0f;
    [Export] public Color GridColor = Colors.Green;
    [Export] public Color PointColor = Colors.Green;
    [Export] public Color LineColor = Colors.Green;
    [Export] public Color AxisColor = Colors.Red;
    
    private string[] pointLabels = {
        "ARMOUR", "AETHER WEBNEVER", "ZER0", "PEACE CORPS ILLUMINED", "6-DRAGON", "VAL", "ETA-C", "CHROME", "VOID-C", "DNSCRYPT", "BLUE BEAM", "XMI", "HIJACK", "WIRESHARK" 
    };
    
    private List<PointMarker> activePoints = new List<PointMarker>();
    private Node3D pointsContainer;
    private Node3D gridContainer;
    private Node3D axisContainer;
    private Node3D effectsContainer;
    
    // Military effects
    private float scanlineProgress = 0.0f;
    private List<MeshInstance3D> connectionLines = new List<MeshInstance3D>();
    private float pulseTime = 0.0f;
    
    public override void _Ready()
    {
        pointsContainer = GetNode<Node3D>("Points");
        gridContainer = GetNode<Node3D>("GridLines");
        
        // Create axis container if it doesn't exist
        axisContainer = new Node3D();
        axisContainer.Name = "AxisLabels";
        AddChild(axisContainer);
        
        // Create effects container
        effectsContainer = new Node3D();
        effectsContainer.Name = "Effects";
        AddChild(effectsContainer);
        
        CreateCoordinateGrid();
        CreateRandomPoints();
        ConnectAllPoints();
        CreateMilitaryEffects();
    }
    
    public override void _Process(double delta)
    {
        UpdateMilitaryEffects((float)delta);
    }
    
    private void CreateCoordinateGrid()
    {
        CreateAxisGrid();
        CreateAxisLabels();
        CreateMainAxes();
    }
    
    private void CreateAxisGrid()
    {
        var gridStep = 10.0f;
        var halfSize = SystemSize / 2;
        
        // Simplified grid - just major lines
        for (float coord = -halfSize; coord <= halfSize; coord += gridStep)
        {
            if (Math.Abs(coord) < 0.1f) continue; // Skip center lines (main axes)
            
            // X lines
            CreateGridLine(
                new Vector3(coord, -halfSize, -halfSize),
                new Vector3(coord, halfSize, halfSize), 
                GridColor * 0.3f
            );
            
            // Y lines  
            CreateGridLine(
                new Vector3(-halfSize, coord, -halfSize),
                new Vector3(halfSize, coord, halfSize),
                GridColor * 0.3f
            );
            
            // Z lines
            CreateGridLine(
                new Vector3(-halfSize, -halfSize, coord),
                new Vector3(halfSize, halfSize, coord),
                GridColor * 0.3f
            );
        }
    }
    
    private void CreateMainAxes()
    {
        var halfSize = SystemSize / 2;
        
        // X-axis (Red)
        CreateGridLine(
            new Vector3(-halfSize, 0, 0),
            new Vector3(halfSize, 0, 0),
            Colors.Red
        );
        
        // Y-axis (Green) 
        CreateGridLine(
            new Vector3(0, -halfSize, 0),
            new Vector3(0, halfSize, 0),
            Colors.Green
        );
        
        // Z-axis (Blue)
        CreateGridLine(
            new Vector3(0, 0, -halfSize),
            new Vector3(0, 0, halfSize),
            Colors.Blue
        );
    }
    
    private void CreateAxisLabels()
    {
        var halfSize = SystemSize / 2;
        
        // X-axis labels
        CreateAxisLabel(new Vector3(halfSize + 2, 0, 0), "X+", Colors.Red);
        CreateAxisLabel(new Vector3(-halfSize - 2, 0, 0), "X-", Colors.Red);
        
        // Y-axis labels
        CreateAxisLabel(new Vector3(0, halfSize + 2, 0), "Y+", Colors.Green);
        CreateAxisLabel(new Vector3(0, -halfSize - 2, 0), "Y-", Colors.Green);
        
        // Z-axis labels
        CreateAxisLabel(new Vector3(0, 0, halfSize + 2), "Z+", Colors.Blue);
        CreateAxisLabel(new Vector3(0, 0, -halfSize - 2), "Z-", Colors.Blue);
    }
    
    private void CreateAxisLabel(Vector3 position, string text, Color color)
    {
        var label = new Label3D();
        label.Text = text;
        label.Position = position;
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        label.Modulate = color;
        label.OutlineSize = 2;
        label.OutlineModulate = Colors.Black;
        
        axisContainer.AddChild(label);
    }
    
    private void CreateGridLine(Vector3 start, Vector3 end, Color color)
    {
        var meshInstance = new MeshInstance3D();
        var arrayMesh = new ArrayMesh();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        
        var vertices = new Vector3[] { start, end };
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        meshInstance.Mesh = arrayMesh;
        
        // Fixed material properties
        var material = new StandardMaterial3D();
        material.AlbedoColor = color;
        material.EmissionEnabled = true;
        material.Emission = color * 0.3f;
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        meshInstance.MaterialOverride = material;
        
        gridContainer.AddChild(meshInstance);
    }
    
    private void CreateRandomPoints()
    {
        var random = new Random();
        var halfSize = SystemSize / 2 * 0.8f; // Keep points slightly inside grid
        
        foreach (string label in pointLabels)
        {
            var position = new Vector3(
                (float)(random.NextDouble() * SystemSize - halfSize),
                (float)(random.NextDouble() * SystemSize - halfSize),
                (float)(random.NextDouble() * SystemSize - halfSize)
            );
            
            CreatePoint(position, label);
        }
    }
    
    private void CreatePoint(Vector3 position, string label)
    {
        var pointMarker = new PointMarker();
        pointMarker.Position = position;
        pointMarker.SetLabel(label);
        pointMarker.SetColors(PointColor);
        
        pointsContainer.AddChild(pointMarker);
        activePoints.Add(pointMarker);
    }
    
    private void ConnectAllPoints()
    {
        for (int i = 0; i < activePoints.Count; i++)
        {
            for (int j = i + 1; j < activePoints.Count; j++)
            {
                var connectionLine = CreateConnectionLine(
                    activePoints[i].Position,
                    activePoints[j].Position
                );
                connectionLines.Add(connectionLine);
            }
        }
    }
    
    private MeshInstance3D CreateConnectionLine(Vector3 start, Vector3 end)
    {
        var meshInstance = new MeshInstance3D();
        var arrayMesh = new ArrayMesh();
        var arrays = new Godot.Collections.Array();
        arrays.Resize((int)Mesh.ArrayType.Max);
        
        var vertices = new Vector3[] { start, end };
        arrays[(int)Mesh.ArrayType.Vertex] = vertices;
        
        arrayMesh.AddSurfaceFromArrays(Mesh.PrimitiveType.Lines, arrays);
        meshInstance.Mesh = arrayMesh;
        
        var material = new StandardMaterial3D();
        material.AlbedoColor = LineColor;
        material.EmissionEnabled = true;
        material.Emission = LineColor * 0.2f;
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        meshInstance.MaterialOverride = material;
        
        pointsContainer.AddChild(meshInstance);
        return meshInstance;
    }
    
    private void CreateMilitaryEffects()
    {
        // Create scanning plane effect
        CreateScanningPlane();
    }
    
    private void CreateScanningPlane()
    {
        var planeMesh = new PlaneMesh();
        planeMesh.Size = new Vector2(SystemSize, SystemSize);
        
        var meshInstance = new MeshInstance3D();
        meshInstance.Mesh = planeMesh;
        
        var material = new StandardMaterial3D();
        material.AlbedoColor = new Color(0, 1, 0, 0.1f);
        material.EmissionEnabled = true;
        material.Emission = new Color(0, 1, 0, 0.3f);
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled; // Show both sides
        material.NoDepthTest = true; // Optional: prevents depth issues
        meshInstance.MaterialOverride = material;
        
        meshInstance.Name = "ScanPlane";
        effectsContainer.AddChild(meshInstance);
    }
    
    private void UpdateMilitaryEffects(float delta)
    {
        pulseTime += delta;
        
        // Animate scanning plane
        var scanPlane = effectsContainer.GetNode<MeshInstance3D>("ScanPlane");
        if (scanPlane != null)
        {
            scanlineProgress += delta * 0.5f;
            if (scanlineProgress > 1.0f) scanlineProgress = 0.0f;
            
            var halfSize = SystemSize / 2;
            scanPlane.Position = new Vector3(0, -halfSize + scanlineProgress * SystemSize, 0);
        }
        
        // Pulse connection lines
        foreach (var line in connectionLines)
        {
            if (line != null && line.MaterialOverride is StandardMaterial3D material)
            {
                var pulse = (Mathf.Sin(pulseTime * 3.0f) + 1.0f) * 0.5f;
                material.Emission = LineColor * (0.1f + pulse * 0.3f);
            }
        }
        
        // Pulse points
        foreach (var point in activePoints)
        {
            if (point != null)
            {
                point.UpdatePulse(pulseTime);
            }
        }
    }
    
    public List<PointMarker> GetActivePoints()
    {
        return activePoints;
    }
}