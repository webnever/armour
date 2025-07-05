using Godot;
using System;

public partial class MachineElfCircleRotation : Control
{
    // Speed of rotation in degrees per second
    [Export]
    public float RotationSpeed = -1.0f;

    public override void _Process(double delta)
    {
        // Calculate the amount of rotation for this frame
        float rotationAmount = RotationSpeed * (float)delta;

        // Rotate the Control node
        Rotation += rotationAmount;
    }
}
