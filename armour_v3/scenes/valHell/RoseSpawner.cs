using Godot;

public partial class RoseSpawner : Node3D
{
    [Export]
    public PackedScene RoseScene { get; set; } = GD.Load<PackedScene>("res://scenes/valHell/rose.tscn");

    [Export]
    public int MaxRoseCount { get; set; } = 200;

    [Export]
    public float SpawnHeight { get; set; } = 100f;

    [Export]
    public float RayDistance { get; set; } = 200f;

    [Export]
    public float MinDistanceBetweenRoses { get; set; } = 0.8f;

    [Export]
    public uint CollisionMask { get; set; } = 1;

    [Export]
    public float FadeDuration { get; set; } = 1.0f;

    [Export]
    public float UpdateInterval { get; set; } = 0.3f;

    [Export]
    public float TransitionZPosition { get; set; } = 35f;

    [Export]
    public float ResetZPosition { get; set; } = -35f;

    [Export]
    public float SpawnWindowAhead { get; set; } = 20f;

    [Export]
    public float SpawnWindowBehind { get; set; } = 10f;

    [Export]
    public float SpawnWidth { get; set; } = 20f;

    [Export]
    public int RosesPerUpdate { get; set; } = 20;

    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private Node3D valHellAnim;
    private ColorRect fadeOverlay;
    private bool isTransitioning = false;
    
    private Godot.Collections.Array<Node3D> activeRoses = new();
    private double lastUpdateTime = 0;
    private float lastValHellZ = 0f;

    public override void _Ready()
    {
        rng.Randomize();
        
        valHellAnim = GetNode<Node3D>("valHellAnim");
        
        CallDeferred(MethodName.CreateFadeOverlay);
        CallDeferred(MethodName.InitialSpawn);
    }

    public override void _Process(double delta)
    {
        // Check transition trigger
        if (valHellAnim != null && !isTransitioning && valHellAnim.GlobalPosition.Z >= TransitionZPosition)
        {
            TriggerTransition();
        }

        // Update culling at intervals
        lastUpdateTime += delta;
        if (lastUpdateTime >= UpdateInterval)
        {
            lastUpdateTime = 0;
            UpdateRoseCulling();
        }
    }

    private void CreateFadeOverlay()
    {
        var canvasLayer = new CanvasLayer();
        canvasLayer.Layer = 100;
        GetTree().Root.CallDeferred(MethodName.AddChild, canvasLayer);
        
        fadeOverlay = new ColorRect();
        fadeOverlay.Color = new Color(0, 0, 0, 1);
        fadeOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        canvasLayer.CallDeferred(MethodName.AddChild, fadeOverlay);
        
        GetTree().CreateTimer(0.1f).Timeout += FadeInFromBlack;
    }

    private void FadeInFromBlack()
    {
        if (fadeOverlay == null) return;
        
        var tween = CreateTween();
        tween.TweenProperty(fadeOverlay, "modulate:a", 0.0f, FadeDuration);
    }

    private void TriggerTransition()
    {
        isTransitioning = true;
        
        var tween = CreateTween();
        tween.TweenProperty(fadeOverlay, "modulate:a", 1.0f, FadeDuration);
        tween.TweenCallback(Callable.From(MoveAndFadeIn));
    }

    private void MoveAndFadeIn()
    {
        if (valHellAnim != null)
        {
            var currentPos = valHellAnim.GlobalPosition;
            valHellAnim.GlobalPosition = new Vector3(currentPos.X, currentPos.Y, ResetZPosition);
            
            // Reset the spawning system for the new loop
            lastValHellZ = ResetZPosition;
            ClearAllRoses();
            CallDeferred(MethodName.SpawnInitialStrip);
        }
        
        var tween = CreateTween();
        tween.TweenProperty(fadeOverlay, "modulate:a", 0.0f, FadeDuration);
        tween.TweenCallback(Callable.From(() => isTransitioning = false));
    }

    private void InitialSpawn()
    {
        if (valHellAnim == null) return;
        
        GetTree().CreateTimer(0.1f).Timeout += () => {
            var valHellPos = valHellAnim.GlobalPosition;
            lastValHellZ = valHellPos.Z;
            GD.Print($"Initial spawn at ValHell Z: {lastValHellZ}");
            SpawnInitialStrip();
        };
    }

    private void UpdateRoseCulling()
    {
        if (valHellAnim == null) return;

        var valHellPos = valHellAnim.GlobalPosition;
        float currentZ = valHellPos.Z;

        // Handle the case where valHell has moved significantly (likely due to teleport/reset)
        if (Mathf.Abs(currentZ - lastValHellZ) > 50f)
        {
            lastValHellZ = currentZ;
            return; // Skip this update cycle to avoid spawning issues
        }

        // Remove roses outside the spawn window
        for (int i = activeRoses.Count - 1; i >= 0; i--)
        {
            var rose = activeRoses[i];
            if (rose == null || !IsInstanceValid(rose))
            {
                activeRoses.RemoveAt(i);
                continue;
            }

            float roseZ = rose.GlobalPosition.Z;
            if (roseZ < currentZ - SpawnWindowBehind || roseZ > currentZ + SpawnWindowAhead)
            {
                rose.QueueFree();
                activeRoses.RemoveAt(i);
            }
        }

        // Spawn new roses if needed (when valHell moves forward)
        if (currentZ > lastValHellZ + 0.1f) // Small threshold to avoid micro-movements
        {
            SpawnRosesAhead(currentZ);
            lastValHellZ = currentZ;
        }
    }

    private void SpawnInitialStrip()
    {
        if (valHellAnim == null) return;
        
        var valHellPos = valHellAnim.GlobalPosition;
        float startZ = valHellPos.Z - SpawnWindowBehind;
        float endZ = valHellPos.Z + SpawnWindowAhead;
        
        SpawnRosesInZRange(startZ, endZ, MaxRoseCount);
    }

    private void SpawnRosesAhead(float currentZ)
    {
        // Spawn roses ahead of current position
        float spawnStartZ = currentZ + SpawnWindowAhead - 5f; // Overlap to prevent gaps
        float spawnEndZ = currentZ + SpawnWindowAhead;
        
        SpawnRosesInZRange(spawnStartZ, spawnEndZ, RosesPerUpdate);
    }

    private void SpawnRosesInZRange(float startZ, float endZ, int maxRoses)
    {
        if (valHellAnim == null) return;
        
        var valHellPos = valHellAnim.GlobalPosition;
        PhysicsDirectSpaceState3D spaceState = GetWorld3D().DirectSpaceState;
        int attempts = 0;
        int maxAttempts = maxRoses * 5;
        int spawnedCount = 0;

        while (spawnedCount < maxRoses && attempts < maxAttempts && activeRoses.Count < MaxRoseCount)
        {
            attempts++;

            // Random position within the strip
            float x = valHellPos.X + rng.RandfRange(-SpawnWidth / 2, SpawnWidth / 2);
            float z = rng.RandfRange(startZ, endZ);
            
            Vector3 rayStart = new Vector3(x, valHellPos.Y + SpawnHeight, z);
            Vector3 rayEnd = rayStart + Vector3.Down * RayDistance;

            var query = PhysicsRayQueryParameters3D.Create(rayStart, rayEnd);
            query.CollideWithAreas = false;
            query.CollideWithBodies = true;
            query.CollisionMask = CollisionMask;

            var result = spaceState.IntersectRay(query);

            if (result.Count > 0)
            {
                Vector3 hitPos = (Vector3)result["position"];

                if (hitPos.IsEqualApprox(Vector3.Zero))
                    continue;

                // Check distance from existing roses
                bool tooClose = false;
                foreach (var existingRose in activeRoses)
                {
                    if (existingRose != null && IsInstanceValid(existingRose))
                    {
                        if (hitPos.DistanceTo(existingRose.GlobalPosition) < MinDistanceBetweenRoses)
                        {
                            tooClose = true;
                            break;
                        }
                    }
                }

                if (!tooClose && RoseScene != null)
                {
                    Node3D roseInstance = (Node3D)RoseScene.Instantiate();
                    AddChild(roseInstance);
                    roseInstance.GlobalPosition = hitPos;
                    roseInstance.RotationDegrees = Vector3.Zero;
                    
                    activeRoses.Add(roseInstance);
                    spawnedCount++;
                }
            }
        }
    }

    public void ClearAllRoses()
    {
        foreach (var rose in activeRoses)
        {
            if (rose != null && IsInstanceValid(rose))
            {
                rose.QueueFree();
            }
        }
        activeRoses.Clear();
    }

    public void RespawnRoses()
    {
        ClearAllRoses();
        CallDeferred(MethodName.InitialSpawn);
    }
}