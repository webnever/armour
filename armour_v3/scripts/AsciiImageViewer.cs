using Godot;
using System;

// Component for displaying ASCII art images with overlay functionality
public partial class AsciiImageViewer : Control
{
    private AsciiRenderer _asciiRenderer;
    private TextureRect _imageDisplay;
    private ColorRect _backgroundOverlay;
    private bool _isVisible = false;
    private bool _isAsciiMode = true;
    
    // Signal to notify when the viewer is closed
    [Signal]
    public delegate void ImageViewerClosedEventHandler();
    
    public override void _Ready()
    {        
        // Create background overlay
        _backgroundOverlay = new ColorRect();
        _backgroundOverlay.Color = new Color(0, 0, 0, 1.0f);
        _backgroundOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(_backgroundOverlay);
        
        // Create ASCII renderer
        _asciiRenderer = new AsciiRenderer();
        _asciiRenderer.BackgroundColor = Colors.Black;
        _asciiRenderer.CharacterGridSize = new Vector2I(120, 60);
        _asciiRenderer.Resolution = new Vector2I(1280, 720);
        _asciiRenderer.Position = new Vector2(0, 0);
        AddChild(_asciiRenderer);
        
        // Create regular image display
        _imageDisplay = new TextureRect();
        _imageDisplay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _imageDisplay.ExpandMode = TextureRect.ExpandModeEnum.FitWidthProportional;
        _imageDisplay.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        AddChild(_imageDisplay);
        
        // Initially hidden
        Visible = false;
        
        // Set up input handling
        SetProcessUnhandledInput(true);
    }
    
    public void ShowImage(string imagePath, bool asciiMode = true)
    {
        if (string.IsNullOrEmpty(imagePath))
        {
            GD.PrintErr("AsciiImageViewer: No image path provided");
            return;
        }
        
        _isAsciiMode = asciiMode;
        GD.Print($"AsciiImageViewer: Attempting to load image: {imagePath} (ASCII: {asciiMode})");
        
        // Try to load and display the image
        try
        {
            // Automatically prepend res://images/ if not already a full path
            string fullPath;
            if (imagePath.StartsWith("res://") || imagePath.StartsWith("user://"))
            {
                fullPath = imagePath;
            }
            else
            {
                fullPath = $"res://images/{imagePath}";
            }
            
            GD.Print($"AsciiImageViewer: Full path resolved to: {fullPath}");
            
            var image = Image.LoadFromFile(fullPath);
            if (image == null)
            {
                GD.PrintErr($"AsciiImageViewer: Failed to load image from {fullPath}");
                // Check if file exists
                using var file = FileAccess.Open(fullPath, FileAccess.ModeFlags.Read);
                if (file == null)
                {
                    GD.PrintErr($"AsciiImageViewer: File does not exist at {fullPath}");
                }
                else
                {
                    GD.PrintErr($"AsciiImageViewer: File exists but Image.LoadFromFile failed");
                    file.Close();
                }
                return;
            }
            
            if (asciiMode)
            {
                // ASCII rendering mode
                _asciiRenderer.Visible = true;
                _imageDisplay.Visible = false;
                
                // Clear the renderer and generate ASCII art
                _asciiRenderer.Clear();
                _asciiRenderer.GenerateAsciiFromImage(image);
            }
            else
            {
                // Regular image display mode
                _asciiRenderer.Visible = false;
                _imageDisplay.Visible = true;
                
                // Create texture from image
                var texture = ImageTexture.CreateFromImage(image);
                _imageDisplay.Texture = texture;
            }
            
            // Show the viewer
            _isVisible = true;
            Visible = true;
            
            GD.Print($"AsciiImageViewer: Successfully displaying image for {fullPath} (ASCII: {asciiMode})");
        }
        catch (Exception e)
        {
            GD.PrintErr($"AsciiImageViewer: Error loading image {imagePath}: {e.Message}");
        }
    }
    
    public void HideImage()
    {
        _isVisible = false;
        Visible = false;
        
        // Clear both renderers
        _asciiRenderer?.Clear();
        if (_imageDisplay != null)
        {
            _imageDisplay.Texture = null;
        }
        
        // Emit signal to notify that viewer is closed
        EmitSignal(SignalName.ImageViewerClosed);
        
        GD.Print("AsciiImageViewer: Image viewer closed");
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        if (!_isVisible) return;
        
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (keyEvent.Keycode == Key.Escape)
            {
                HideImage();
                GetViewport().SetInputAsHandled();
            }
        }
    }
    
    // Get the CanvasLayer this viewer is on
    public CanvasLayer GetCanvasLayer()
    {
        return GetParent() as CanvasLayer;
    }
    
    // Toggle between ASCII and regular display modes
    public void ToggleDisplayMode()
    {
        if (!_isVisible) return;
        
        _isAsciiMode = !_isAsciiMode;
        
        if (_isAsciiMode)
        {
            _asciiRenderer.Visible = true;
            _imageDisplay.Visible = false;
        }
        else
        {
            _asciiRenderer.Visible = false;
            _imageDisplay.Visible = true;
        }
        
        GD.Print($"AsciiImageViewer: Toggled to {(_isAsciiMode ? "ASCII" : "Regular")} mode");
    }
}