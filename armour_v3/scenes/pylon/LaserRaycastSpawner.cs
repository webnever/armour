using Godot;
using System;
using System.Collections.Generic;

public partial class LaserRaycastSpawner : Node3D
{
    [Export] public int LaserCount { get; set; } = 50;
    [Export] public float SpawnRadius { get; set; } = 10.0f;
    [Export] public float LaserRange { get; set; } = 1000.0f;
    [Export] public float LaserWidth { get; set; } = 0.05f;
    [Export] public Color LaserColor { get; set; } = new Color(0.0f, 1.0f, 0.2f, 0.8f); // Green with transparency
    [Export] public bool RandomDirections { get; set; } = true;
    [Export] public bool AnimateLasers { get; set; } = true;
    [Export] public float AnimationSpeed { get; set; } = 2.0f;
    [Export] public float RespawnInterval { get; set; } = 0.5f; // Respawn every 0.5 seconds
    
    private List<LaserData> _lasers = new List<LaserData>();
    private Random _random = new Random();
    private float _respawnTimer = 0.0f;
    
    private struct LaserData
    {
        public RayCast3D RayCast;
        public MeshInstance3D Visual;
        public float AnimationOffset;
        public Vector3 OriginalDirection;
        public Vector3 RotationAxis; // Random rotation axis for each laser
        public float RotationSpeed; // Random rotation speed for each laser
    }

    public override void _Ready()
    {
        SpawnLasers();
    }

    private void SpawnLasers()
    {
        for (int i = 0; i < LaserCount; i++)
        {
            CreateLaser(i);
        }
    }

    private void CreateLaser(int index)
    {
        // Create RayCast3D
        var rayCast = new RayCast3D();
        AddChild(rayCast);
        
        // Random position within spawn radius
        Vector3 randomPos = GetRandomPositionInRadius();
        rayCast.Position = randomPos;
        
        // Random direction or downward
        Vector3 direction;
        if (RandomDirections)
        {
            direction = GetRandomDirection();
        }
        else
        {
            direction = Vector3.Down;
        }
        
        rayCast.TargetPosition = direction * LaserRange;
        rayCast.Enabled = true;
        rayCast.CollideWithAreas = true;
        rayCast.CollideWithBodies = true;
        
        // Create visual laser beam
        var laserVisual = CreateLaserVisual();
        rayCast.AddChild(laserVisual);
        
        // Store laser data
        var laserData = new LaserData
        {
            RayCast = rayCast,
            Visual = laserVisual,
            AnimationOffset = (float)_random.NextDouble() * Mathf.Pi * 2, // Random phase
            OriginalDirection = direction,
            RotationAxis = GetRandomDirection(), // Random rotation axis
            RotationSpeed = (float)_random.NextDouble() * 4.0f + 1.0f // Random speed between 1-5
        };
        
        _lasers.Add(laserData);
    }

    private MeshInstance3D CreateLaserVisual()
    {
        var meshInstance = new MeshInstance3D();
        
        // Create cylinder mesh for laser beam
        var cylinderMesh = new CylinderMesh();
        cylinderMesh.Height = 1.0f; // Will be scaled based on hit distance
        cylinderMesh.TopRadius = LaserWidth;
        cylinderMesh.BottomRadius = LaserWidth;
        cylinderMesh.RadialSegments = 8;
        cylinderMesh.Rings = 1;
        
        meshInstance.Mesh = cylinderMesh;
        
        // Create glowing material
        var material = new StandardMaterial3D();
        material.AlbedoColor = LaserColor;
        material.EmissionEnabled = true;
        material.Emission = new Color(LaserColor.R, LaserColor.G, LaserColor.B) * 2.0f; // Bright emission
        material.EmissionEnergyMultiplier = 1.5f;
        material.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        material.NoDepthTest = false; // Enable depth testing for proper 3D rendering
        material.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        
        meshInstance.MaterialOverride = material;
        
        return meshInstance;
    }

    public override void _Process(double delta)
    {
        UpdateLasers((float)delta);
        
        // Handle respawning
        _respawnTimer += (float)delta;
        if (_respawnTimer >= RespawnInterval)
        {
            _respawnTimer = 0.0f;
            RespawnLasers();
        }
    }

    private void UpdateLasers(float delta)
    {
        for (int i = 0; i < _lasers.Count; i++)
        {
            var laser = _lasers[i];
            
            // Animation: rotate laser direction around random axis
            if (AnimateLasers)
            {
                float time = (float)Time.GetUnixTimeFromSystem() * AnimationSpeed * laser.RotationSpeed + laser.AnimationOffset;
                
                // Create rotation around the random axis
                var transform = Transform3D.Identity;
                transform = transform.Rotated(laser.RotationAxis, time);
                Vector3 rotatedDirection = transform * laser.OriginalDirection;
                
                laser.RayCast.TargetPosition = rotatedDirection * LaserRange;
            }
            
            // Update visual based on raycast hit
            UpdateLaserVisual(laser);
        }
    }

    private void UpdateLaserVisual(LaserData laser)
    {
        if (laser.RayCast.IsColliding())
        {
            // Hit something - scale beam to hit point
            Vector3 hitPoint = laser.RayCast.GetCollisionPoint();
            Vector3 localHitPoint = laser.RayCast.ToLocal(hitPoint);
            float distance = localHitPoint.Length();
            
            // Scale and position the visual beam
            laser.Visual.Scale = new Vector3(1.0f, distance, 1.0f);
            laser.Visual.Position = localHitPoint * 0.5f; // Center of beam
            laser.Visual.LookAt(laser.RayCast.Position + localHitPoint, Vector3.Up);
            laser.Visual.Visible = true;
        }
        else
        {
            // No hit - full length beam
            laser.Visual.Scale = new Vector3(1.0f, LaserRange, 1.0f);
            laser.Visual.Position = laser.RayCast.TargetPosition * 0.5f;
            laser.Visual.LookAt(laser.RayCast.Position + laser.RayCast.TargetPosition, Vector3.Up);
            laser.Visual.Visible = true;
        }
    }

    private Vector3 GetRandomPositionInRadius()
    {
        // Random XYZ within 50^3 unit cube
        float x = ((float)_random.NextDouble() - 0.5f) * 59.0f; // -50 to +50
        float y = ((float)_random.NextDouble() - 0.5f) * 50.0f; // -50 to +50
        float z = ((float)_random.NextDouble() - 0.5f) * 50.0f; // -50 to +50
        
        return new Vector3(x, y, z);
    }

    private Vector3 GetRandomDirection()
    {
        // Generate completely random direction in all directions
        float theta = (float)_random.NextDouble() * Mathf.Pi * 2; // Azimuth (0-360°)
        float phi = (float)_random.NextDouble() * Mathf.Pi; // Polar (0-180°)
        
        return new Vector3(
            Mathf.Sin(phi) * Mathf.Cos(theta),
            Mathf.Cos(phi),
            Mathf.Sin(phi) * Mathf.Sin(theta)
        ).Normalized();
    }

    // Public methods for runtime control
    public void RespawnLasers()
    {
        ClearLasers();
        SpawnLasers();
    }

    public void ClearLasers()
    {
        foreach (var laser in _lasers)
        {
            laser.RayCast?.QueueFree();
        }
        _lasers.Clear();
    }

    public void SetLaserCount(int count)
    {
        LaserCount = count;
        RespawnLasers();
    }

    public void ToggleAnimation()
    {
        AnimateLasers = !AnimateLasers;
    }
}