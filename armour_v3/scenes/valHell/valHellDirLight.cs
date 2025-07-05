using Godot;

public partial class valHellDirLight : Node3D
{
    [Export] public float RotationSpeed = 90.0f; // degrees per second
    
    public override void _Process(double delta)
    {
        RotateY(Mathf.DegToRad(RotationSpeed * (float)delta));
    }
}