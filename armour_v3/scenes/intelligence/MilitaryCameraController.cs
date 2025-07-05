using Godot;
using System.Collections.Generic;

public partial class MilitaryCameraController : Node3D
{
    [Export] public float OrbitSpeed = 1.0f;
    [Export] public float OrbitRadius = 80.0f;
    [Export] public float MouseSensitivity = 0.5f;
    [Export] public float ZoomSpeed = 5.0f;
    [Export] public float MinZoom = 20.0f;
    [Export] public float MaxZoom = 150.0f;
    [Export] public float CameraYOffset = 0.5f; // Y offset for camera position
    
    private Camera3D camera;
    private Vector3 centerPoint = Vector3.Zero;
    private float orbitAngleH = 0.0f;
    private float orbitAngleV = 0.3f; // Default downward angle
    private float currentRadius;
    private bool isOrbiting = true; // Start auto-orbit by default
    private bool isMouseControlled = false;
    
    public override void _Ready()
    {
        camera = GetNode<Camera3D>("Camera3D");
        currentRadius = OrbitRadius;
        
        // Initial camera position
        UpdateCameraPosition();
    }
    
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                isMouseControlled = mouseButton.Pressed;
                if (isMouseControlled)
                {
                    Input.MouseMode = Input.MouseModeEnum.Captured;
                }
                else
                {
                    Input.MouseMode = Input.MouseModeEnum.Visible;
                }
            }
            
            // Zoom with mouse wheel
            if (mouseButton.ButtonIndex == MouseButton.WheelUp)
            {
                currentRadius = Mathf.Max(MinZoom, currentRadius - ZoomSpeed);
            }
            else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
            {
                currentRadius = Mathf.Min(MaxZoom, currentRadius + ZoomSpeed);
            }
        }
        
        if (@event is InputEventMouseMotion mouseMotion && isMouseControlled)
        {
            orbitAngleH -= mouseMotion.Relative.X * MouseSensitivity * 0.01f;
            orbitAngleV = Mathf.Clamp(
                orbitAngleV - mouseMotion.Relative.Y * MouseSensitivity * 0.01f,
                -Mathf.Pi / 3, Mathf.Pi / 3
            );
        }
        
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            switch (keyEvent.Keycode)
            {
                case Key.Space:
                    ToggleAutoOrbit();
                    break;
            }
        }
    }
    
    public override void _Process(double delta)
    {
        if (isOrbiting && !isMouseControlled)
        {
            orbitAngleH += OrbitSpeed * (float)delta;
        }
        
        UpdateCameraPosition();
    }
    
    private void UpdateCameraPosition()
    {
        // Calculate camera position based on spherical coordinates with Y offset
        var x = centerPoint.X + currentRadius * Mathf.Cos(orbitAngleV) * Mathf.Cos(orbitAngleH);
        var y = centerPoint.Y + currentRadius * Mathf.Sin(orbitAngleV) + CameraYOffset;
        var z = centerPoint.Z + currentRadius * Mathf.Cos(orbitAngleV) * Mathf.Sin(orbitAngleH);
        
        camera.Position = new Vector3(x, y, z);
        camera.LookAt(centerPoint, Vector3.Up);
    }
    
    private void ToggleAutoOrbit()
    {
        isOrbiting = !isOrbiting;
        GD.Print($"Auto-orbit: {(isOrbiting ? "ON" : "OFF")}");
    }
    
    public void FocusOnPoint(Vector3 position)
    {
        centerPoint = position;
    }
    
    public void SetOrbitRadius(float radius)
    {
        currentRadius = Mathf.Clamp(radius, MinZoom, MaxZoom);
    }
}