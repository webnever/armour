using Godot;

public partial class RotateCube : Node3D
{
    // Rotation speeds for each axis (in radians per second)
    [Export] public float RotationSpeedX = 1.0f;
    [Export] public float RotationSpeedY = 1.5f;
    [Export] public float RotationSpeedZ = 0.8f;
    
    public override void _Ready()
    {
        // Any initialization code can go here
    }
    
    public override void _Process(double delta)
    {
        // Rotate the object on all three axes
        RotateX(RotationSpeedX * (float)delta);
        RotateY(RotationSpeedY * (float)delta);
        RotateZ(RotationSpeedZ * (float)delta);
    }
}