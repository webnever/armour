using Godot;
using System;

public partial class CustomMenuButton : Control
{
    [Export]
    public string Text
    {
        get => _text;
        set
        {
            GD.Print($"[{Name}] Setting Text: {value}");
            _text = value;
            if (_buttonLabel != null)
                _buttonLabel.Text = value;
        }
    }

    [Signal]
    public delegate void ButtonPressedEventHandler();
    [Signal]
    public delegate void MouseEnteredEventHandler();
    [Signal]
    public delegate void MouseExitedEventHandler();

    private Label _buttonLabel;
    private TextureRect _selectionTexture;
    private bool _isHovered;
    private bool _isPressed;
    private bool _isEnabled = true;
    private string _text = "";

    public bool IsEnabled
    {
        get => _isEnabled;
        set
        {
            GD.Print($"[{Name}] Setting IsEnabled: {value}");
            _isEnabled = value;
            Modulate = _isEnabled ? Colors.White : new Color(1, 1, 1, 0.5f);
        }
    }

    public override void _Ready()
    {
        GD.Print($"[{Name}] _Ready called");
        _buttonLabel = GetNode<Label>("CenterContainer/Label");
        _selectionTexture = GetNode<TextureRect>("CenterContainer/TextureRect");
        if (!string.IsNullOrEmpty(_text))
        {
            GD.Print($"[{Name}] Setting initial label text: {_text}");
            _buttonLabel.Text = _text;
        }
        _selectionTexture.Modulate = new Color(1, 1, 1, 0);
        GD.Print($"[{Name}] Initial setup complete");
    }

    public override void _Process(double delta)
    {
        if (!_isEnabled)
        {
            return;
        }

        Vector2 mousePos = GetViewport().GetMousePosition();
        bool isMouseOver = GetGlobalRect().HasPoint(mousePos);

        if (isMouseOver != _isHovered)
        {
            GD.Print($"[{Name}] Hover state changed - Previous: {_isHovered}, New: {isMouseOver}");
            _isHovered = isMouseOver;
            UpdateVisualState();

            if (_isHovered)
            {
                GD.Print($"[{Name}] Emitting MouseEntered");
                EmitSignal(SignalName.MouseEntered);
            }
            else
            {
                GD.Print($"[{Name}] Emitting MouseExited");
                EmitSignal(SignalName.MouseExited);
            }
        }
    }

    private void UpdateVisualState()
    {
        GD.Print($"[{Name}] UpdateVisualState - Enabled: {_isEnabled}, Pressed: {_isPressed}, Hovered: {_isHovered}");
        Color newColor;
        
        if (!_isEnabled)
        {
            newColor = new Color(1, 1, 1, 0);
        }
        else if (_isPressed)
        {
            newColor = new Color(0, 1, 0, 1);
        }
        else if (_isHovered)
        {
            newColor = new Color(1, 1, 1, 1);
        }
        else
        {
            newColor = new Color(1, 1, 1, 0);
        }
        
        GD.Print($"[{Name}] Setting modulation to: R:{newColor.R} G:{newColor.G} B:{newColor.B} A:{newColor.A}");
        _selectionTexture.Modulate = newColor;
        QueueRedraw();
    }

    private void OnTimerTimeout()
    {
        GD.Print($"[{Name}] Timer timeout - Resetting pressed state");
        UpdateVisualState();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!_isEnabled)
        {
            GD.Print($"[{Name}] Ignoring input - button disabled");
            return;
        }

        if (@event is InputEventMouseButton mouseButton)
        {
            if (mouseButton.ButtonIndex == MouseButton.Left)
            {
                if (mouseButton.Pressed)
                {
                    GD.Print($"[{Name}] Left mouse button pressed");
                    _isPressed = true;
                    UpdateVisualState();
                    var timer = GetTree().CreateTimer(0.1);
                    timer.Timeout += OnTimerTimeout;
                }
                else if (_isPressed) // This will now work since timer isn't resetting _isPressed
                {
                    GD.Print($"[{Name}] Left mouse button released");
                    _isPressed = false;
                    if (_isHovered)
                    {
                        GD.Print($"[{Name}] Emitting ButtonPressed");
                        EmitSignal(SignalName.ButtonPressed);
                        UpdateVisualState();
                    }
                }
            }
        }
    }
}