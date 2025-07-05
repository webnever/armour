using Godot;

public partial class RotatingLight : DirectionalLight3D
{
    [Export] public float RotationSpeed = 30.0f; // Degrees per second
    [Export] public Vector3 RotationAxis = Vector3.Up; // Which axis to rotate around
    [Export] public bool AutoStart = true;
    
    private bool isRotating = true;
    
    public override void _Ready()
    {
        isRotating = AutoStart;
    }
    
    public override void _Process(double delta)
    {
        if (isRotating)
        {
            // Rotate around the specified axis
            RotateObjectLocal(RotationAxis.Normalized(), Mathf.DegToRad(RotationSpeed * (float)delta));
        }
    }
    
    public void ToggleRotation()
    {
        isRotating = !isRotating;
    }
    
    public void SetRotationSpeed(float speed)
    {
        RotationSpeed = speed;
    }
}