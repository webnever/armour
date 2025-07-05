using Godot;
using System;

public partial class MotorcycleController : Node3D
{
    [Export] public float MaxSpeed = 30.0f;
    [Export] public float Acceleration = 15.0f;
    [Export] public float Brake = 30.0f;
    [Export] public float MaxLeanAngle = 45.0f;
    [Export] public float LeanSpeed = 3.0f;
    [Export] public float TurnSpeed = 2.0f;
    [Export] public float GravityForce = 9.8f;
    
    private float currentSpeed = 0.0f;
    private float currentLeanAngle = 0.0f;
    private Vector3 velocity = Vector3.Zero;
    
    private Node3D motorcycleMesh;
    private CharacterBody3D parentMotorcycle;
    
    public override void _Ready()
    {
        motorcycleMesh = GetNode<Node3D>("../MotorcycleMesh");
        parentMotorcycle = GetParent<CharacterBody3D>();
        
        if (motorcycleMesh == null)
        {
            GD.PrintErr("MotorcycleMesh node not found!");
        }
        
        if (parentMotorcycle == null)
        {
            GD.PrintErr("Parent motorcycle node not found!");
        }
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (parentMotorcycle == null) return;
        
        HandleInput(delta);
        ApplyPhysics(delta);
        ApplyMovement();
    }
    
    private void HandleInput(double delta)
    {
        // Acceleration and braking
        if (Input.IsActionPressed("accelerate"))
        {
            currentSpeed = Mathf.MoveToward(currentSpeed, MaxSpeed, (float)delta * Acceleration);
        }
        else if (Input.IsActionPressed("brake"))
        {
            currentSpeed = Mathf.MoveToward(currentSpeed, 0, (float)delta * Brake);
        }
        else
        {
            currentSpeed = Mathf.MoveToward(currentSpeed, 0, (float)delta * (Acceleration * 0.5f));
        }
        
        // Leaning and turning
        float targetLean = 0;
        if (Input.IsActionPressed("turn_left"))
        {
            targetLean = MaxLeanAngle;
            parentMotorcycle.Rotate(Vector3.Up, -TurnSpeed * (float)delta * (currentSpeed / MaxSpeed));
        }
        else if (Input.IsActionPressed("turn_right"))
        {
            targetLean = -MaxLeanAngle;
            parentMotorcycle.Rotate(Vector3.Up, TurnSpeed * (float)delta * (currentSpeed / MaxSpeed));
        }
        
        // Smooth lean angle transition
        currentLeanAngle = Mathf.MoveToward(currentLeanAngle, targetLean, LeanSpeed * (float)delta);
        
        if (motorcycleMesh != null)
        {
            motorcycleMesh.Rotation = new Vector3(0, 0, currentLeanAngle);
        }
    }
    
    private void ApplyPhysics(double delta)
    {
        if (!parentMotorcycle.IsOnFloor())
        {
            velocity.Y -= GravityForce * (float)delta;
        }
        else
        {
            velocity.Y = 0;
        }
        
        var forward = -parentMotorcycle.Transform.Basis.Z;
        velocity.X = forward.X * currentSpeed;
        velocity.Z = forward.Z * currentSpeed;
    }
    
    private void ApplyMovement()
    {
        parentMotorcycle.Velocity = velocity;
        parentMotorcycle.MoveAndSlide();
    }
}