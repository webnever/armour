using Godot;

public partial class RotateY : Node3D
{
    [Export] public float RotationSpeed = 90.0f; // Degrees per second

    public override void _Process(double delta)
    {
        // Rotate around Y axis
        Rotate(Vector3.Up, Mathf.DegToRad(RotationSpeed * (float)delta));
    }
}