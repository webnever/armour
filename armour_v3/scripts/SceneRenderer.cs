using Godot;
using System;

// Component for rendering 3D scenes over the CLI emulator
public partial class SceneRenderer : Control
{
    private SubViewport _viewport;
    private Node3D _sceneRoot;
    private ColorRect _backgroundOverlay;
    private bool _isVisible = false;
    private PackedScene _currentScene;
    private Node _currentSceneInstance;
    
    // Configuration
    [Export] public Color BackgroundColor = Colors.Black;
    
    // Signal to notify when the scene viewer is closed
    [Signal]
    public delegate void SceneViewerClosedEventHandler();
    
    public override void _Ready()
    {
        // Create background overlay
        _backgroundOverlay = new ColorRect();
        _backgroundOverlay.Color = BackgroundColor;
        _backgroundOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_backgroundOverlay);
        
        // Create viewport for 3D rendering
        _viewport = new SubViewport();
        _viewport.Size = new Vector2I(1280, 720);
        _viewport.RenderTargetUpdateMode = SubViewport.UpdateMode.Always;
        AddChild(_viewport);
        
        // Create scene root
        _sceneRoot = new Node3D();
        _sceneRoot.Name = "SceneRoot";
        _viewport.AddChild(_sceneRoot);
        
        // Create viewport display
        var textureRect = new TextureRect();
        textureRect.Texture = _viewport.GetTexture();
        textureRect.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        textureRect.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
        textureRect.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        AddChild(textureRect);
        
        // Initially hidden
        Visible = false;
        
        // Set up input handling
        SetProcessUnhandledInput(true);
    }
    
    public void ShowScene(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            GD.PrintErr("SceneRenderer: No scene path provided");
            return;
        }
        
        GD.Print($"SceneRenderer: Attempting to load scene: {scenePath}");
        
        try
        {
            // Try to load scene
            PackedScene scene = null;
            if (FileAccess.FileExists(scenePath))
            {
                scene = GD.Load<PackedScene>(scenePath);
            }
            else
            {
                // Try with full path
                string fullPath = scenePath.StartsWith("res://") ? scenePath : $"res://scenes/3d/{scenePath}";
                if (FileAccess.FileExists(fullPath))
                {
                    scene = GD.Load<PackedScene>(fullPath);
                    scenePath = fullPath;
                }
            }
            
            if (scene == null)
            {
                GD.PrintErr($"SceneRenderer: Failed to load scene from {scenePath}");
                ShowFallbackScene(scenePath);
                return;
            }

            // Clear any existing scene
            ClearScene();
            
            // Instantiate the scene
            _currentSceneInstance = scene.Instantiate();
            if (_currentSceneInstance == null)
            {
                GD.PrintErr("SceneRenderer: Failed to instantiate scene");
                return;
            }
            
            // Add to scene tree and make visible
            _sceneRoot.AddChild(_currentSceneInstance);
            
            // Enable input processing for the scene
            if (_currentSceneInstance is Node sceneNode)
            {
                // Set process mode to always process input
                sceneNode.ProcessMode = Node.ProcessModeEnum.Always;
                
                // If it's a Control node, grab focus
                if (_currentSceneInstance is Control control)
                {
                    control.GrabFocus();
                }
                
                // If it has a specific input handler, enable it
                EnableSceneInputProcessing(_currentSceneInstance);
            }
            
            // Show the viewer
            _isVisible = true;
            Visible = true;
            
            GD.Print($"SceneRenderer: Successfully displaying scene {scenePath}");
        }
        catch (Exception e)
        {
            GD.PrintErr($"SceneRenderer: Error loading scene {scenePath}: {e.Message}");
            ShowFallbackScene(scenePath);
        }
    }
    
    private void EnableSceneInputProcessing(Node sceneNode)
    {
        // Enable input processing for the scene and its children
        sceneNode.SetProcessInput(true);
        sceneNode.SetProcessUnhandledInput(true);
        sceneNode.SetProcessUnhandledKeyInput(true);
        
        // Recursively enable input for child nodes
        foreach (Node child in sceneNode.GetChildren())
        {
            EnableSceneInputProcessing(child);
        }
        
        // If the scene has a camera, make it current
        if (sceneNode.FindChild("Camera3D") is Camera3D camera)
        {
            camera.MakeCurrent();
        }
        
        // If the scene is a 3D scene, ensure the viewport can receive input
        if (sceneNode is Node3D)
        {
            GetViewport().GuiDisableInput = false;
        }
    }
    
    private void ShowFallbackScene(string requestedScene)
    {
        // Create a simple fallback scene with text
        ClearScene();
        
        var label3D = new Label3D();
        label3D.Text = $"Scene Not Found:\n{requestedScene}\n\nPress ESCAPE to close";
        label3D.Position = Vector3.Zero;
        label3D.Scale = Vector3.One * 0.1f;
        _sceneRoot.AddChild(label3D);
        
        _isVisible = true;
        Visible = true;
    }
    
    private void ClearScene()
    {
        foreach (Node child in _sceneRoot.GetChildren())
        {
            child.QueueFree();
        }
    }
    
    public void HideScene()
    {
        _isVisible = false;
        Visible = false;
        
        // Clear the scene
        ClearScene();
        _currentScene = null;
        
        // Emit signal to notify that viewer is closed
        EmitSignal(SignalName.SceneViewerClosed);
        
        GD.Print("SceneRenderer: Scene viewer closed");
    }
    
    public override void _Input(InputEvent @event)
    {
        // Only process input when scene is visible
        if (!IsSceneVisible())
            return;
            
        // Allow ESC to close the scene
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                HideScene();
                GetViewport().SetInputAsHandled();
                return;
            }
        }
        
        // Forward input to the current scene instance
        if (_currentSceneInstance != null)
        {
            // Let the scene handle the input first
            _currentSceneInstance._Input(@event);
        }
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        // Only process unhandled input when scene is visible
        if (!IsSceneVisible())
            return;
            
        // Forward unhandled input to the scene
        if (_currentSceneInstance != null)
        {
            _currentSceneInstance._UnhandledInput(@event);
        }
    }
    
    // Get the CanvasLayer this renderer is on
    public CanvasLayer GetCanvasLayer()
    {
        return GetParent() as CanvasLayer;
    }
    
    // Check if scene viewer is currently visible
    public bool IsSceneVisible()
    {
        return _isVisible;
    }
}
