using Godot;
using System;

public partial class hijackIntroText : RichTextLabel
{
    private string[] _textLines = {
    "You approach the prosthetic arm.\n",
    "It erupts in light, the brightest, darkest hellfire.\n",
    "With a fist, it crushes the protective glass.\n",
    "A barbed needle springs out like a snake, and fiercely punctures your artery.\n",
    "The arm radiates such an intense heat, and tightly grips your own arm.\n",
    "You feel the burning sensation of your flesh melting away.\n",
    };

    private int _currentLineIndex = 0;
    private int _currentCharIndex = 0;
    private float _letterDelay = 0.05f; // Time between each letter
    private float _lineDelay = 1.0f; // Time before starting next line
    private float _sceneDelay = 0.1f; // Time before loading next scene
    private float _fadeDuration = 2.0f; // Duration of fade to black animation
    private Timer _letterTimer;
    private Timer _lineTimer;
    private Timer _sceneTimer;
    private bool _isTyping = false;
    private PackedScene _preloadedScene; // NEW: Store preloaded scene
    private Tween _fadeTween; // Tween for fade animation

    public override void _Ready()
    {
        // Clear any existing text
        Text = "";

        // Start loading the scene in the background
        LoadSceneInBackground();

        // Create and configure letter timer
        _letterTimer = new Timer();
        _letterTimer.WaitTime = _letterDelay;
        _letterTimer.OneShot = true;
        _letterTimer.Timeout += OnLetterTimerTimeout;
        AddChild(_letterTimer);

        // Create and configure line timer
        _lineTimer = new Timer();
        _lineTimer.WaitTime = _lineDelay;
        _lineTimer.OneShot = true;
        _lineTimer.Timeout += OnLineTimerTimeout;
        AddChild(_lineTimer);

        // Create and configure scene timer
        _sceneTimer = new Timer();
        _sceneTimer.WaitTime = _sceneDelay;
        _sceneTimer.OneShot = true;
        _sceneTimer.Timeout += OnSceneTimerTimeout;
        AddChild(_sceneTimer);

        // Start typing the first line
        StartTypingCurrentLine();
    }

    private void StartTypingCurrentLine()
    {
        if (_currentLineIndex >= _textLines.Length)
        {
            // All lines complete, start fade to black
            StartFadeToBlack();
            return;
        }

        _currentCharIndex = 0;
        _isTyping = true;
        TypeNextCharacter();
    }

    private void TypeNextCharacter()
    {
        if (!_isTyping || _currentLineIndex >= _textLines.Length)
            return;

        string currentLine = _textLines[_currentLineIndex];

        if (_currentCharIndex < currentLine.Length)
        {
            // Add the next character to the display
            string displayText = Text;
            if (_currentCharIndex == 0 && _currentLineIndex > 0)
            {
                // Add newline before starting new line (except for first line)
                displayText += "\n";
            }

            displayText += currentLine[_currentCharIndex];
            Text = displayText;

            _currentCharIndex++;
            _letterTimer.Start();
        }
        else
        {
            // Current line is complete
            _isTyping = false;
            _currentLineIndex++;

            // Start timer for next line
            _lineTimer.Start();
        }
    }

    private void OnLetterTimerTimeout()
    {
        TypeNextCharacter();
    }

    private void OnLineTimerTimeout()
    {
        StartTypingCurrentLine();
    }

    private void OnSceneTimerTimeout()
    {
        // Load the hijack scene
        LoadHijackScene();
    }

    // NEW: Load scene in background
    private void LoadSceneInBackground()
    {
        try
        {
            GD.Print("hijackIntroText: Starting background scene load");
            _preloadedScene = GD.Load<PackedScene>("res://scenes/hijack/hijack.tscn");
            if (_preloadedScene != null)
            {
                GD.Print("hijackIntroText: Scene loaded successfully in background");
            }
            else
            {
                GD.PrintErr("hijackIntroText: Failed to load hijack.tscn - scene file not found");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"hijackIntroText: Error loading hijack scene in background: {ex.Message}");
        }
    }

    private void LoadHijackScene()
    {
        try
        {
            if (_preloadedScene != null)
            {
                var hijackInstance = _preloadedScene.Instantiate();

                // Add to the scene tree (replace current scene or add to parent)
                var parent = GetParent();
                if (parent != null)
                {
                    parent.AddChild(hijackInstance);

                    // Remove the intro text (this node)
                    CallDeferred("queue_free");
                }
                else
                {
                    // Fallback: change scene entirely
                    GetTree().ChangeSceneToFile("res://scenes/hijack/hijack.tscn");
                }

                GD.Print("hijackIntroText: Successfully instantiated preloaded hijack scene");
            }
            else
            {
                // Fallback: try to load directly if preload failed
                GD.PrintErr("hijackIntroText: Preloaded scene is null, trying direct load");
                GetTree().ChangeSceneToFile("res://scenes/hijack/hijack.tscn");
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"hijackIntroText: Error instantiating hijack scene: {ex.Message}");
        }
    }

    // NEW: Start fade to black animation
    private void StartFadeToBlack()
    {
        var colorRect = GetParent()?.GetNode<ColorRect>("ColorRect");
        if (colorRect != null)
        {
            GD.Print("hijackIntroText: Starting fade to black");
            _fadeTween = CreateTween();
            _fadeTween.TweenProperty(colorRect, "modulate:a", 1.0f, _fadeDuration);
            _fadeTween.TweenCallback(Callable.From(() =>
            {
                GD.Print("hijackIntroText: Fade to black completed, starting scene timer");
                _sceneTimer.Start();
            }));
        }
        else
        {
            GD.PrintErr("hijackIntroText: ColorRect not found in parent, starting scene timer directly");
            _sceneTimer.Start();
        }
    }

    // Allow skipping the intro by pressing any key
    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // Skip to scene loading
            _letterTimer.Stop();
            _lineTimer.Stop();

            // Stop fade tween if running
            if (_fadeTween != null)
            {
                _fadeTween.Kill();
            }

            // Show all remaining text instantly
            string fullText = "";
            for (int i = 0; i < _textLines.Length; i++)
            {
                if (i > 0) fullText += "\n";
                fullText += _textLines[i];
            }
            Text = fullText;

            // Set ColorRect to fully black immediately
            var colorRect = GetParent()?.GetNode<ColorRect>("ColorRect");
            if (colorRect != null)
            {
                colorRect.Modulate = new Color(1, 1, 1, 1);
            }

            // Load scene immediately
            LoadHijackScene();
        }
    }
}
