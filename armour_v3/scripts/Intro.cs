using Godot;

public partial class Intro : Control
{
    [Export] public Texture2D[] Images = new Texture2D[3];
    [Export] public float[] ImageScales = new float[3] { 1.0f, 1.0f, 1.0f };
    [Export] public string NextScenePath = "res://NextScene.tscn";
    [Export] public float ImageDisplayTime = 2.0f;
    [Export] public float FadeDuration = 1.0f;
    
    private TextureRect imageDisplay;
    private ColorRect fadeOverlay;
    private int currentImageIndex = 0;
    public override void _Ready()
    {
        SetupUI();
        StartSlideshow();
    }
    
    private void SetupUI()
    {
        // Create the image display
        imageDisplay = new TextureRect();
        imageDisplay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        imageDisplay.ExpandMode = TextureRect.ExpandModeEnum.FitWidth;
        imageDisplay.StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered;
        imageDisplay.PivotOffset = imageDisplay.Size / 2; // Center the pivot point
        AddChild(imageDisplay);
        
        // Create fade overlay (black rectangle that covers the screen)
        fadeOverlay = new ColorRect();
        fadeOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        fadeOverlay.Color = new Color(0, 0, 0, 1); // Start fully black
        AddChild(fadeOverlay);
    }
    
    private void StartSlideshow()
    {
        if (Images.Length == 0)
        {
            GD.PrintErr("No images assigned to the slideshow!");
            return;
        }
        
        ShowNextImage();
    }
    
    private void ShowNextImage()
    {
        if (currentImageIndex < Images.Length)
        {
            // Set the current image
            imageDisplay.Texture = Images[currentImageIndex];
            
            // Update pivot offset to ensure scaling happens from center
            imageDisplay.PivotOffset = imageDisplay.Size / 2;
            
            // Set the scale for the current image
            float scale = currentImageIndex < ImageScales.Length ? ImageScales[currentImageIndex] : 1.0f;
            imageDisplay.Scale = new Vector2(scale, scale);
            
            // Fade in from black to reveal the image
            FadeIn(() => {
                // Wait for the display time, then fade back to black
                GetTree().CreateTimer(ImageDisplayTime).Timeout += () => {
                    FadeOut(() => {
                        currentImageIndex++;
                        ShowNextImage();
                    });
                };
            });
        }
        else
        {
            // All images shown, switch to next scene
            GetTree().ChangeSceneToFile(NextScenePath);
        }
    }
    
    private void FadeIn(System.Action onComplete = null)
    {
        var tween = CreateTween();
        tween.TweenProperty(fadeOverlay, "color:a", 0.0f, FadeDuration);
        if (onComplete != null)
        {
            tween.TweenCallback(Callable.From(onComplete));
        }
    }
    
    private void FadeOut(System.Action onComplete = null)
    {
        var tween = CreateTween();
        tween.TweenProperty(fadeOverlay, "color:a", 1.0f, FadeDuration);
        if (onComplete != null)
        {
            tween.TweenCallback(Callable.From(onComplete));
        }
    }
}