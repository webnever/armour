using Godot;

[Tool]
public partial class HijackFlamePlacer   : Node3D
{
    [Export] public int QuadCount { get; set; } = 5;
    [Export] public float ScaleFactor { get; set; } = 1.0f;
    [Export] public float BaseZPosition { get; set; } = -2.0f;
    [Export] public PackedScene QuadScene { get; set; }
    
    private MeshInstance3D[] quads;
    
    public override void _Ready()
    {
        CreateQuadArray();
    }
    
    private void CreateQuadArray()
    {
        // Clear existing quads
        if (quads != null)
        {
            foreach (var quad in quads)
            {
                if (IsInstanceValid(quad))
                    quad.QueueFree();
            }
        }
        
        quads = new MeshInstance3D[QuadCount];
        
        for (int i = 0; i < QuadCount; i++)
        {
            CreateQuad(i);
        }
    }
    
    private void CreateQuad(int index)
    {
        if (QuadScene == null)
        {
            GD.PrintErr("QuadScene is not assigned! Please assign your quad scene in the inspector.");
            return;
        }
        
        var quad = QuadScene.Instantiate<MeshInstance3D>();
        
        // Calculate position and rotation
        var posRot = CalculateQuadTransform(index);
        quad.Position = posRot.Position * ScaleFactor;
        quad.RotationDegrees = posRot.Rotation;
        quad.Scale = new Vector3(ScaleFactor, ScaleFactor, -ScaleFactor); // Flip Z to face forward
        
        // Set name for easy identification
        quad.Name = $"Quad_{index}";
        
        AddChild(quad);
        quads[index] = quad;
    }
    
    private (Vector3 Position, Vector3 Rotation) CalculateQuadTransform(int index)
    {
        if (QuadCount == 1)
        {
            return (new Vector3(0, 0, BaseZPosition), Vector3.Zero);
        }
        
        // Calculate angle for this quad
        float totalArc = 90.0f; // Total arc span in degrees
        float angleStep = totalArc / (QuadCount - 1);
        float angle = (index - (QuadCount - 1) / 2.0f) * angleStep;
        
        // Convert to radians for calculation
        float angleRad = Mathf.DegToRad(angle);
        
        float quadHalfWidth = 0.5f;
        
        Vector3 position;
        if (Mathf.Abs(angle) < 0.001f) // Center quad
        {
            position = new Vector3(0, 0, BaseZPosition);
        }
        else
        {
            // Each quad should be positioned so that adjacent quads touch at their edges
            // The distance between quad centers should be: quadWidth / cos(angleStep/2)
            float adjacentDistance = 1.0f / Mathf.Cos(Mathf.DegToRad(angleStep) / 2.0f);
            
            // Position relative to center, with each quad spaced by adjacentDistance
            float distanceFromCenter = Mathf.Abs(index - (QuadCount - 1) / 2.0f) * adjacentDistance;
            
            float xOffset = Mathf.Sin(angleRad) * distanceFromCenter;
            float zOffset = (1.0f - Mathf.Cos(Mathf.Abs(angleRad))) * distanceFromCenter;
            
            position = new Vector3(xOffset, 0, BaseZPosition - zOffset);
        }
        
        Vector3 rotation = new Vector3(0, angle, 0);
        
        return (position, rotation);
    }
    
    // Method to update array when properties change in editor
    public void UpdateQuadArray()
    {
        if (Engine.IsEditorHint())
        {
            CreateQuadArray();
        }
    }
    
    // Editor-only method to trigger updates
    public override void _ValidateProperty(Godot.Collections.Dictionary property)
    {
        if (Engine.IsEditorHint() && 
            (property["name"].AsString() == "QuadCount" || 
             property["name"].AsString() == "ScaleFactor" ||
             property["name"].AsString() == "BaseZPosition"))
        {
            CallDeferred(nameof(UpdateQuadArray));
        }
    }
}