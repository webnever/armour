using Godot;

public partial class ForgivenFade : Control
{
    [Export] public float FadeDuration = 2.0f;
    [Export] public Color FadeFromColor = Colors.Black;
    [Export] public bool FadeOnReady = true;
    
    private ColorRect _fadeOverlay;
    private Tween _fadeTween;
    
    public override void _Ready()
    {
        // Defer the setup to avoid the "busy setting up children" error
        CallDeferred(MethodName.SetupFade);
    }
    
    private void SetupFade()
    {
        // Create the fade overlay
        _fadeOverlay = new ColorRect();
        _fadeOverlay.Color = FadeFromColor;
        _fadeOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _fadeOverlay.MouseFilter = Control.MouseFilterEnum.Ignore;
        _fadeOverlay.ZIndex = 1000; // High Z index to be on top
        
        // Add it to the scene tree
        GetViewport().AddChild(_fadeOverlay);
        
        // Start fade if enabled
        if (FadeOnReady)
        {
            StartFadeIn();
        }
    }
    
    public void StartFadeIn()
    {
        if (_fadeOverlay == null) return;
        
        // Create tween
        _fadeTween = CreateTween();
        _fadeTween.SetEase(Tween.EaseType.Out);
        _fadeTween.SetTrans(Tween.TransitionType.Cubic);
        
        // Ensure overlay is visible at start
        _fadeOverlay.Modulate = Colors.White;
        
        // Tween the modulate alpha from 1 to 0
        _fadeTween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, FadeDuration);
        
        // Remove overlay when fade is complete
        _fadeTween.TweenCallback(Callable.From(RemoveFadeOverlay));
    }
    
    private void RemoveFadeOverlay()
    {
        if (_fadeOverlay != null)
        {
            _fadeOverlay.QueueFree();
            _fadeOverlay = null;
        }
    }
    
    public override void _ExitTree()
    {
        // Clean up if scene changes before fade completes
        if (_fadeOverlay != null && IsInstanceValid(_fadeOverlay))
        {
            _fadeOverlay.QueueFree();
        }
    }
}