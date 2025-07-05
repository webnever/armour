using Godot;

public partial class IntelligenceScene : Node3D
{
    public override void _Ready()
    {
        // Setup the scene
        CreateSceneStructure();
        
        // Set initial camera position
        var camera = GetNode<Camera3D>("CameraController/Camera3D");
        if (camera != null)
        {
            camera.Position = new Vector3(0, 20, 50);
            camera.LookAt(Vector3.Zero, Vector3.Up);
        }
        
        GD.Print("Military 3D Coordinate System Initialized");
        GD.Print("Controls:");
        GD.Print("- Left Click + Drag: Manual camera control");
        GD.Print("- Mouse Wheel: Zoom");
        GD.Print("- SPACE: Toggle auto-orbit");
    }
    
    private void CreateSceneStructure()
    {
        // Scene structure is set up in the Godot editor
        // No flower needed
    }
}