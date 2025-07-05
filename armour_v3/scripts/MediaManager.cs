using Godot;
using System;
using System.Collections.Generic;

// Central manager for all visual media display over CLI
public partial class MediaManager : Node
{
    private AsciiImageViewer _imageViewer;
    private SceneRenderer _sceneRenderer;
    private AdvancedCLIEmulator _cliEmulator;
    private GameState _gameState;
    private Node _sceneTargetNode; // NEW: Target node for scene instantiation
    private bool _handleLocationTransition = false; // NEW: Flag for handling location transitions
    
    // Media display modes
    public enum DisplayMode
    {
        ASCII,
        Regular,
        Scene3D
    }
    
    // Configuration
    [Export] public DisplayMode DefaultImageMode = DisplayMode.ASCII;
    [Export] public bool EnableSceneRendering = true;
    [Export] public bool EnableImageViewing = true;
    [Export] public bool AllowDisplayModeToggle = true; // New: Allow users to toggle modes
    
    // Signal to notify when any media viewer is closed
    [Signal]
    public delegate void MediaViewerClosedEventHandler();
    
    public override void _Ready()
    {
        // Initialization will be done in Initialize method
    }
    
    public void Initialize(AdvancedCLIEmulator cliEmulator, GameState gameState)
    {
        _cliEmulator = cliEmulator;
        _gameState = gameState;
        
        if (EnableImageViewing)
        {
            SetupImageViewer();
        }
        
        if (EnableSceneRendering)
        {
            SetupSceneRenderer();
        }
        
        GD.Print("MediaManager: Initialized successfully");
    }
    
    private void SetupImageViewer()
    {
        _imageViewer = new AsciiImageViewer();
        var imageCanvasLayer = new CanvasLayer();
        imageCanvasLayer.Layer = 10; // Changed from 101 to 10
        imageCanvasLayer.AddChild(_imageViewer);
        AddChild(imageCanvasLayer);
        _imageViewer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _imageViewer.ImageViewerClosed += OnMediaViewerClosed;
    }
    
    private void SetupSceneRenderer()
    {
        _sceneRenderer = new SceneRenderer();
        var sceneCanvasLayer = new CanvasLayer();
        sceneCanvasLayer.Layer = 5; // Changed from 100 to 5
        sceneCanvasLayer.AddChild(_sceneRenderer);
        AddChild(sceneCanvasLayer);
        _sceneRenderer.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _sceneRenderer.SceneViewerClosed += OnSceneViewerClosed;
    }

    // Main method to display media based on location data
    public void DisplayLocationMedia(string locationId, DisplayMode? mode = null)
    {
        if (string.IsNullOrEmpty(locationId) || _gameState == null)
        {
            GD.PrintErr("MediaManager: No location ID provided or game state not available");
            return;
        }
        
        // Get location data from game state
        var location = _gameState.GetLocationById(locationId);
        if (location == null)
        {
            GD.PrintErr($"MediaManager: Location '{locationId}' not found");
            return;
        }
        
        // Close any currently open media
        CloseAllMedia();
        
        // Check for special location handling
        _handleLocationTransition = (locationId == "hijack_temple_arm");
        
        // Check if location has a scene path and mode is Scene3D
        if (!string.IsNullOrEmpty(location.ScenePath) && 
            (mode == DisplayMode.Scene3D || string.IsNullOrEmpty(location.ImagePath)))
        {
            GD.Print($"MediaManager: Displaying 3D scene '{location.ScenePath}' for location '{locationId}'");
            DisplayScene(location.ScenePath);
        }
        // Check if location has an image path
        else if (!string.IsNullOrEmpty(location.ImagePath))
        {
            // Determine display mode: parameter override > location preference > global default
            DisplayMode displayMode;
            if (mode.HasValue)
            {
                displayMode = mode.Value;
            }
            else if (!string.IsNullOrEmpty(location.ImageDisplayMode) && location.ImageDisplayMode != "default")
            {
                // Parse location-specific display mode
                switch (location.ImageDisplayMode.ToLower())
                {
                    case "ascii":
                        displayMode = DisplayMode.ASCII;
                        break;
                    case "regular":
                    case "normal":
                    case "image":
                        displayMode = DisplayMode.Regular;
                        break;
                    default:
                        displayMode = DefaultImageMode;
                        break;
                }
            }
            else
            {
                displayMode = DefaultImageMode;
            }
            
            GD.Print($"MediaManager: Displaying image '{location.ImagePath}' for location '{locationId}' in {displayMode} mode");
            DisplayImage(location.ImagePath, displayMode);
        }
        else
        {
            GD.Print($"MediaManager: No media found for location '{locationId}'");
        }
    }
    
    private void DisplayImage(string imagePath, DisplayMode mode)
    {
        if (_imageViewer == null)
        {
            GD.PrintErr("MediaManager: Image viewer not initialized");
            return;
        }
        
        bool asciiMode = mode == DisplayMode.ASCII;
        _imageViewer.ShowImage(imagePath, asciiMode);
    }
    
    private void DisplayScene(string scenePath)
    {
        if (_sceneRenderer == null)
        {
            GD.PrintErr("MediaManager: Scene renderer not initialized");
            return;
        }

        // Temporarily disable CLI input processing while scene is active
        if (_cliEmulator != null)
        {
            _cliEmulator.SetProcessInput(false);
        }

        // Use SceneLoadingManager for async loading with CLI loading bar
        if (SceneLoadingManager.Instance != null)
        {
            GD.Print("MediaManager: Starting async scene loading");
            
            // Don't display initial bar - let the progress callback handle it
            bool isFirstProgress = true;
            
            SceneLoadingManager.Instance.LoadSceneAsync(
                scenePath,
                progress =>
                {
                    // Update ASCII loading bar in CLI
                    int percent = Mathf.Clamp((int)(progress * 100), 0, 100);
                    int bars = percent / 10;
                    string bar = "[" + new string('#', bars) + new string('-', 10 - bars) + $"] {percent}%";
                    
                    if (isFirstProgress)
                    {
                        // First progress update - display new line using default text color
                        _cliEmulator?.DisplayOutput($"Loading Scene: {bar}");
                        isFirstProgress = false;
                    }
                    else
                    {
                        // Subsequent updates - update the same line
                        _cliEmulator?.UpdateLastLine($"Loading Scene: {bar}");
                    }
                    GD.Print($"Loading progress: {percent}%");
                },
                packedScene =>
                {
                    GD.Print("MediaManager: Scene loading completed");
                    if (packedScene != null)
                    {
                        _cliEmulator?.UpdateLastLine("Loading Scene: [##########] 100% - Complete!");
                        _sceneRenderer.ShowScene(scenePath);
                    }
                    else
                    {
                        _cliEmulator?.UpdateLastLine("Loading Scene: Failed to load scene.");
                        // Re-enable CLI input if scene failed to load
                        if (_cliEmulator != null)
                        {
                            _cliEmulator.SetProcessInput(true);
                        }
                    }
                }
            );
        }
        else
        {
            GD.PrintErr("MediaManager: SceneLoadingManager instance not found");
            // Fallback: load instantly
            _sceneRenderer.ShowScene(scenePath);
        }
    }
    
    // NEW: Check if any media viewer is currently visible
    public bool IsMediaVisible()
    {
        bool imageVisible = _imageViewer != null && _imageViewer.Visible;
        bool sceneVisible = _sceneRenderer != null && _sceneRenderer.IsSceneVisible();
        return imageVisible || sceneVisible;
    }
    
    // NEW: Method to display media for current location
    public void DisplayCurrentLocationMedia(DisplayMode? mode = null)
    {
        if (_gameState == null)
        {
            GD.PrintErr("MediaManager: Game state not available");
            return;
        }
        
        var currentLocation = _gameState.GetCurrentLocation();
        if (currentLocation == null)
        {
            GD.PrintErr("MediaManager: No current location");
            return;
        }
        
        DisplayLocationMedia(currentLocation.Id, mode);
    }
    
    // NEW: Check if current location has any media
    public bool CurrentLocationHasMedia()
    {
        if (_gameState == null) return false;
        
        var currentLocation = _gameState.GetCurrentLocation();
        return currentLocation != null && (currentLocation.HasImage || currentLocation.HasScene);
    }
    
    // Close any currently open media
    public void CloseAllMedia()
    {
        if (_imageViewer != null && _imageViewer.Visible)
        {
            _imageViewer.HideImage();
        }
        
        if (_sceneRenderer != null && _sceneRenderer.IsSceneVisible())
        {
            _sceneRenderer.HideScene();
        }
    }
    
    private void OnMediaViewerClosed()
    {
        // Re-enable CLI input processing when media closes
        if (_cliEmulator != null)
        {
            _cliEmulator.SetProcessInput(true);
        }
        
        // Handle location transition if needed
        if (_handleLocationTransition)
        {
            _handleLocationTransition = false;
            // Don't add new command prompt here - let GameState handle the transition
            // The GameState will add a message which will trigger a new prompt in AdvancedCLIEmulator
        }
        
        // Notify CLI emulator that media viewer closed
        EmitSignal(SignalName.MediaViewerClosed);
        
        // Return focus to CLI and add command prompt if not handling location transition
        if (_cliEmulator != null)
        {
            _cliEmulator.CallDeferred("GrabLineEditFocus");
            
            // Always add a new command prompt when media closes
            _cliEmulator.CallDeferred("AddNewCommandPrompt");
        }
    }

    // NEW: Special handling for scene viewer closed from SceneRenderer
    private void OnSceneViewerClosed()
    {
        OnMediaViewerClosed();
    }
    
    // Set display mode preference
    public void SetDefaultImageMode(DisplayMode mode)
    {
        DefaultImageMode = mode;
        GD.Print($"MediaManager: Default image mode set to {mode}");
    }
    
    // Get list of locations with media
    public string[] GetLocationsWithMedia()
    {
        var locationsWithMedia = new List<string>();
        
        if (_gameState != null)
        {
            var locations = _gameState.GetLocations();
            foreach (var location in locations.Values)
            {
                if (!string.IsNullOrEmpty(location.ImagePath))
                {
                    locationsWithMedia.Add($"{location.Id} (Image)");
                }
                else if (!string.IsNullOrEmpty(location.ScenePath))
                {
                    locationsWithMedia.Add($"{location.Id} (Scene)");
                }
            }
        }
        
        return locationsWithMedia.ToArray();
    }
    
    // NEW: Direct scene display method (bypasses location data)
    public void DisplaySceneDirect(string scenePath)
    {
        if (string.IsNullOrEmpty(scenePath))
        {
            GD.PrintErr("MediaManager: No scene path provided for direct display");
            return;
        }

        // Close any currently open media
        CloseAllMedia();
        
        // Set flag for location transition handling
        _handleLocationTransition = true;
        
        GD.Print($"MediaManager: Displaying 3D scene directly: {scenePath}");
        DisplayScene(scenePath);
    }
}
