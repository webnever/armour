using Godot;

public partial class Rotate : Node3D
{
    [Export]
    public float RotationSpeed = 1.0f; // Rotations per second

    public override void _Process(double delta)
    {
        // Rotate around Y axis
        Rotation += new Vector3(0, RotationSpeed * (float)delta * Mathf.Tau, 0);
    }
}