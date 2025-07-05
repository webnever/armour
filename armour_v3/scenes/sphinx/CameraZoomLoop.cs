using Godot;

public partial class CameraZoomLoop : Camera3D
{
    [Export] public float ZoomSpeed = 2.0f;
    [Export] public float DistanceBetweenScenes = 30.0f;
    [Export] public int NumberOfInstances = 5;
    
    [Export] public PackedScene SphinxGateScene;
    [Export] public PackedScene CrossedGunsScene;
    [Export] public PackedScene AngelWingsScene;
    
    private Tween _tween;
    private float _sceneSpacing;
    private float _furthestSceneZ = 0; // Track the furthest scene position
    private int _scenePattern = 0; // 0=Sphinx, 1=Guns, 2=Wings
    
    public override void _Ready()
    {
        _sceneSpacing = DistanceBetweenScenes;
        
        // Debug: Check if scenes are assigned
        GD.Print($"SphinxGateScene: {SphinxGateScene != null}");
        GD.Print($"CrossedGunsScene: {CrossedGunsScene != null}");
        GD.Print($"AngelWingsScene: {AngelWingsScene != null}");
        
        // Defer the setup to avoid the "busy parent" error
        CallDeferred(MethodName.SetupScene);
    }
    
    private void SetupScene()
    {
        // Create multiple instances of each scene in sequence
        CreateSceneInstances();
        
        // Start camera movement
        StartInfiniteZoom();
    }
    
    private void CreateSceneInstances()
    {
        float zPosition = 0;
        int instanceCount = 0;
        
        GD.Print($"Creating initial {NumberOfInstances} sets of scenes...");
        
        // Create initial instances
        int totalSets = NumberOfInstances;
        
        for (int set = 0; set < totalSets; set++)
        {
            CreateSceneSet(ref zPosition, ref instanceCount);
        }
        
        _furthestSceneZ = -zPosition; // Track the furthest scene
        GD.Print($"Initial scenes created. Furthest scene at Z: {_furthestSceneZ}");
        GD.Print($"Total instances created: {instanceCount}");
    }
    
    private void CreateSceneSet(ref float zPosition, ref int instanceCount)
    {
        // Sphinx Gate
        if (SphinxGateScene != null)
        {
            CreateSingleScene(SphinxGateScene, "Sphinx Gate", ref zPosition, ref instanceCount);
        }
        
        // Crossed Guns
        if (CrossedGunsScene != null)
        {
            CreateSingleScene(CrossedGunsScene, "Crossed Guns", ref zPosition, ref instanceCount);
        }
        
        // Angel Wings
        if (AngelWingsScene != null)
        {
            CreateSingleScene(AngelWingsScene, "Angel Wings", ref zPosition, ref instanceCount);
        }
    }
    
    private void CreateSingleScene(PackedScene scene, string name, ref float zPosition, ref int instanceCount)
    {
        var instance = scene.Instantiate();
        GetParent().CallDeferred(Node.MethodName.AddChild, instance);
        
        if (instance is Node3D node3D)
        {
            node3D.Position = new Vector3(0, 0, -zPosition);
            // Add a name or group to identify dynamically created scenes
            instance.SetMeta("scene_type", name);
            GD.Print($"Created {name} at Z: {-zPosition}");
        }
        
        instanceCount++;
        zPosition += _sceneSpacing;
        _furthestSceneZ = -zPosition; // Update furthest position
    }
    
    private void StartInfiniteZoom()
    {
        // Set starting position
        Position = new Vector3(0, 0, 10);
        GD.Print($"Camera starting at: {Position}");
        
        // For infinite movement, we don't need an end point
        // The camera will move continuously and we'll spawn scenes as needed
        _tween = CreateTween();
        _tween.SetLoops();
        
        // Move camera continuously forward at constant speed
        float moveDistance = 1000000; // Very large distance for "infinite" movement
        float duration = moveDistance / (ZoomSpeed * 10); // Adjust speed as needed
        
        GD.Print($"Starting infinite movement with speed factor: {ZoomSpeed}");
        
        _tween.TweenProperty(this, "position", new Vector3(0, 0, -moveDistance), duration);
    }
    
    private void ResetCameraPosition()
    {
        GD.Print("Resetting camera position and regenerating scenes");
        Position = new Vector3(0, 0, 10);
        
        // Also regenerate scenes to maintain the effect
        ClearScenes();
        CreateSceneInstances();
    }
    
    private void ClearScenes()
    {
        var parent = GetParent();
        int removedCount = 0;
        
        for (int i = parent.GetChildCount() - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            if (child != this) // Don't remove the camera itself
            {
                child.QueueFree();
                removedCount++;
            }
        }
        
        GD.Print($"Cleared {removedCount} scene instances");
    }
    
    // Continuous scene management for truly infinite effect
    public override void _Process(double delta)
    {
        ManageInfiniteScenes();
        
        // Optional: Add debug info to see camera position
        if (Engine.GetProcessFrames() % 120 == 0) // Print every 2 seconds
        {
            GD.Print($"Camera Z: {Position.Z:F1}, Furthest scene: {_furthestSceneZ:F1}");
        }
    }
    
    private void ManageInfiniteScenes()
    {
        float cameraZ = Position.Z;
        float spawnDistance = 200; // How far ahead to maintain scenes
        float cleanupDistance = 100; // How far behind to clean up scenes
        
        // Spawn new scenes ahead if needed
        while (_furthestSceneZ > cameraZ - spawnDistance)
        {
            SpawnNextScene();
        }
        
        // Clean up scenes far behind the camera
        CleanupOldScenes(cameraZ + cleanupDistance);
    }
    
    private void SpawnNextScene()
    {
        PackedScene sceneToSpawn = null;
        string sceneName = "";
        
        // Follow the pattern: Sphinx (0) -> Guns (1) -> Wings (2) -> repeat
        switch (_scenePattern)
        {
            case 0:
                sceneToSpawn = SphinxGateScene;
                sceneName = "Sphinx Gate";
                break;
            case 1:
                sceneToSpawn = CrossedGunsScene;
                sceneName = "Crossed Guns";
                break;
            case 2:
                sceneToSpawn = AngelWingsScene;
                sceneName = "Angel Wings";
                break;
        }
        
        if (sceneToSpawn != null)
        {
            var instance = sceneToSpawn.Instantiate();
            GetParent().CallDeferred(Node.MethodName.AddChild, instance);
            
            if (instance is Node3D node3D)
            {
                node3D.Position = new Vector3(0, 0, _furthestSceneZ - _sceneSpacing);
                instance.SetMeta("scene_type", sceneName);
                instance.SetMeta("dynamic", true); // Mark as dynamically created
                
                _furthestSceneZ -= _sceneSpacing;
                GD.Print($"Spawned {sceneName} at Z: {node3D.Position.Z:F1}");
            }
        }
        
        // Advance pattern
        _scenePattern = (_scenePattern + 1) % 3;
    }
    
    private void CleanupOldScenes(float cleanupThreshold)
    {
        var parent = GetParent();
        
        for (int i = parent.GetChildCount() - 1; i >= 0; i--)
        {
            var child = parent.GetChild(i);
            
            if (child != this && child is Node3D node3D && child.HasMeta("dynamic"))
            {
                if (node3D.Position.Z > cleanupThreshold)
                {
                    string sceneType = child.GetMeta("scene_type").AsString();
                    GD.Print($"Cleaning up {sceneType} at Z: {node3D.Position.Z:F1}");
                    child.QueueFree();
                }
            }
        }
    }
}