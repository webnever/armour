using Godot;
using System.Collections.Generic;

public partial class TerminalConsole : Control
{
    // Configuration
    private const int MaxLines = 100;
    private const int FontSize = 16;
    private const int LineSpacing = 2;
    private const int MarginFromEdge = 20;
    
    // Exports
    [Export]
    public double FadeDelay = 20.0;
    
    [Export]
    public double FadeDuration = 0.25;
    
    [Export]
    public Font CustomFont { get; set; }
    
    // Node references
    [Export]
    private MarginContainer _marginContainer;
    
    [Export]
    private ScrollContainer _scrollContainer;
    
    [Export]
    private VBoxContainer _textContainer;
    
    [Export]
    private NinePatchRect _background;
    
    // Fade handling
    private double _lastMessageTime;
    private bool _isFaded = false;
    private Tween _currentTween;
    
    // Theme variables
    private readonly Color _textColor = new(0, 1, 0, 1);
    private Font _font;
    
    // Message history
    private readonly List<Label> _messages = new();

    public bool _isVisible = true;
    public bool _timerFinished = true;
    private const double TOGGLE_COOLDOWN = 0.2;

    public override void _Ready()
    {
        // Register this instance with the global singleton
        GlobalTerminal.Instance = this;
        // Get the ScrollContainer from your path
        var scrollContainer = GetNode<ScrollContainer>("MarginContainer/NinePatchRect/MarginContainer/ScrollContainer");
        
        var vScrollBar = scrollContainer.GetVScrollBar();
        vScrollBar.CustomMinimumSize = new Vector2(2, 0);
        var hScrollBar = scrollContainer.GetHScrollBar();
        vScrollBar.CustomMinimumSize = new Vector2(0, 2);

        // Create the style for the scrollbar
        var style = new StyleBoxFlat();
        style.BgColor = new Color("00ff00"); // Green color
        style.CornerRadiusTopLeft = 0;
        style.CornerRadiusTopRight = 0;
        style.CornerRadiusBottomLeft = 0;
        style.CornerRadiusBottomRight = 0;
        
        scrollContainer.GetVScrollBar().AddThemeStyleboxOverride("grabber", style);
        scrollContainer.GetHScrollBar().AddThemeStyleboxOverride("grabber", style);
        scrollContainer.GetVScrollBar().AddThemeStyleboxOverride("grabber_hover", style);
        scrollContainer.GetVScrollBar().AddThemeStyleboxOverride("grabber_pressed", style);
        scrollContainer.GetHScrollBar().AddThemeStyleboxOverride("grabber_hover", style);
        scrollContainer.GetHScrollBar().AddThemeStyleboxOverride("grabber_pressed", style);

        // Initialize font
        _font = CustomFont ?? ThemeDB.FallbackFont;
        if (_font == null)
        {
            _font = new SystemFont();
            GD.PrintErr("No font specified for Terminal Console. Using system font.");
        }
        
        // // Set up positioning
        // SetAnchorsPreset(LayoutPreset.BottomRight);
        // Position = new Vector2(-MarginFromEdge, -MarginFromEdge);
        // CustomMinimumSize = new Vector2(400, 300);
        
        // Setup background
        // _background.Texture = GD.Load<Texture2D>("res://ui/ui9slice120and58bw.png");
        // _background.PatchMarginLeft = 160;
        // _background.PatchMarginTop = 58;
        // _background.PatchMarginRight = 160;
        // _background.PatchMarginBottom = 58;
        
        // Setup text container
        _textContainer.AddThemeConstantOverride("separation", LineSpacing);
        
        // Initialize last message time
        _lastMessageTime = Time.GetUnixTimeFromSystem();
        
        PrintMessage("Terminal Console initialized", "success");
    }

    public override void _Process(double delta)
    {
        // Check if we should start fading
        if (!_isFaded && Time.GetUnixTimeFromSystem() - _lastMessageTime > FadeDelay)
        {
            FadeOut();
        }
    }

    private void FadeOut()
    {
        if (_currentTween != null)
        {
            _currentTween.Kill();
        }

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), FadeDuration);
        _isFaded = true;
        _isVisible = false;  // Update visibility state
    }

    private void FadeIn()
    {
        if (_currentTween != null)
        {
            _currentTween.Kill();
        }

        _currentTween = CreateTween();
        _currentTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), FadeDuration);
        _isFaded = false;
        _isVisible = true;  // Update visibility state
    }

    public void PrintMessage(string text, string type = "normal")
    {
        GD.Print(text);
        // Update last message time and ensure visibility
        _lastMessageTime = Time.GetUnixTimeFromSystem();
        if (_isFaded)
        {
            FadeIn();
        }

        var label = new Label();
        label.Text = text;
        label.AddThemeColorOverride("font_color", GetMessageColor(type));
        label.AddThemeFontOverride("font", _font);
        
        _textContainer.AddChild(label);
        _messages.Add(label);
        
        // Remove old messages if we exceed MaxLines
        if (_messages.Count > MaxLines)
        {
            var oldMessage = _messages[0];
            _messages.RemoveAt(0);
            oldMessage.QueueFree();
        }
        
        // Scroll to bottom
        ScrollToBottom();
    }

    public async void PrintMessageWithTyping(string text, string type = "normal", float typingSpeed = 0.05f)
    {
        // Update last message time and ensure visibility
        _lastMessageTime = Time.GetUnixTimeFromSystem();
        if (_isFaded)
        {
            FadeIn();
        }

        var label = new Label();
        label.AddThemeFontOverride("font", _font);
        _textContainer.AddChild(label);
        _messages.Add(label);
        
        // Type out the message character by character
        var displayedText = "";
        foreach (char character in text)
        {
            displayedText += character;
            label.Text = displayedText;
            await ToSignal(GetTree().CreateTimer(typingSpeed), SceneTreeTimer.SignalName.Timeout);
            // Update last message time for each character to prevent fading during typing
            _lastMessageTime = Time.GetUnixTimeFromSystem();
        }
        
        // Apply final styling
        label.AddThemeColorOverride("font_color", GetMessageColor(type));
        
        // Clean up old messages
        if (_messages.Count > MaxLines)
        {
            var oldMessage = _messages[0];
            _messages.RemoveAt(0);
            oldMessage.QueueFree();
        }
        
        // Scroll to bottom
        ScrollToBottom();
    }

    private Color GetMessageColor(string type) => type switch
    {
        "debug" => new Color(1, 1, 1, 1),
        "info" => new Color(1, 1, 0, 1),
        "error" => new Color(1, 0, 0, 1),
        "warning" => new Color(1, 1, 0, 1),
        "success" => new Color(0, 1, 0, 1),
        _ => _textColor
    };

    private async void ScrollToBottom()
    {
        await ToSignal(GetTree(), "process_frame");
        await ToSignal(GetTree(), "process_frame");

        _scrollContainer.ScrollVertical = (int)_scrollContainer.GetVScrollBar().MaxValue;
    }

    public void ToggleConsole()
    {
        if (!_timerFinished) return;
        
        _timerFinished = false;
        _lastMessageTime = Time.GetUnixTimeFromSystem();
        
        if (_currentTween != null)
        {
            _currentTween.Kill();
        }

        // Simply toggle visibility state
        _isVisible = !_isVisible;
        
        _currentTween = CreateTween();
        if (_isVisible)
        {
            _currentTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 1), FadeDuration);
            _isFaded = false;
        }
        else
        {
            _currentTween.TweenProperty(this, "modulate", new Color(1, 1, 1, 0), FadeDuration);
            _isFaded = true;
        }

        GetTree().CreateTimer(TOGGLE_COOLDOWN).Connect("timeout", Callable.From(() => _timerFinished = true));
    }
}