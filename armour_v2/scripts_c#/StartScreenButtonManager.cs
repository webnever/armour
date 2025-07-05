using Godot;
using System;
using System.Collections.Generic;

public partial class StartScreenButtonManager : Node
{
    [Export] public bool CanScroll { get; set; } = false;
    private bool _canInteract = false;
    
    // Core references
    private GameSceneManager _gameSceneManager;
    private Viewport _viewport;
    private Control _leftContainer;
    private Resource _cursorArrow;
    private Resource _cursorLink;
    private CustomMenuButton[] _menuButtons;
    
    private CustomMenuButton _hoveredButton;
    private readonly Dictionary<string, Action> _buttonActions = new();

    private struct MenuDefinition
    {
        public string Name { get; set; }
        public string Text { get; set; }
        public Action Action { get; set; }
    }

    private readonly MenuDefinition[] _menuDefinitions;

    public StartScreenButtonManager()
    {
        _menuDefinitions = new MenuDefinition[]
        {
            new() { Name = "CustomMenuButton", Text = "NEW GAME", Action = HandleNewGamePressed },
            new() { Name = "CustomMenuButton2", Text = "CONTINUE", Action = HandleContinuePressed },
            new() { Name = "CustomMenuButton3", Text = "LOAD GAME", Action = HandleLoadGamePressed },
            new() { Name = "CustomMenuButton4", Text = "OPTIONS", Action = HandleOptionsPressed },
            new() { Name = "CustomMenuButton5", Text = "QUIT", Action = HandleQuitPressed }
        };
    }

    public override void _Ready()
    {
        InitializeReferences();
        LoadResources();
        InitializeButtons();
        CanScroll = false;
        Input.SetCustomMouseCursor(_cursorArrow);
    }

    private void InitializeReferences()
    {
        _gameSceneManager = GetNode<GameSceneManager>("/root/mainScene/GameSceneManager");
        _viewport = GetViewport();
        _leftContainer = GetNode<Control>("LeftContainer");
        _leftContainer.MouseFilter = Control.MouseFilterEnum.Pass;
    }

    private void LoadResources()
    {
        _cursorArrow = GD.Load<Resource>("res://cursor/tex_cursor_pointer.png");
        _cursorLink = GD.Load<Resource>("res://cursor/tex_cursor_link.png");
    }

    private void InitializeButtons()
    {
        _menuButtons = new CustomMenuButton[_menuDefinitions.Length];
        
        for (int i = 0; i < _menuDefinitions.Length; i++)
        {
            var buttonDef = _menuDefinitions[i];
            var button = GetNode<CustomMenuButton>($"LeftContainer/{buttonDef.Name}");
            
            button.Text = buttonDef.Text;
            
            // Connect signals and explicitly pass the button reference
            button.ButtonPressed += () => OnButtonPressed(button);
            button.MouseEntered += () => OnMouseEntered(button);
            button.MouseExited += () => OnMouseExited(button);
            
            _menuButtons[i] = button;
            _buttonActions[button.Name] = buttonDef.Action;
        }
    }

    private void OnMouseEntered(CustomMenuButton button)
    {
        HandleButtonHover(button, true);
    }

    private void OnMouseExited(CustomMenuButton button)
    {
        HandleButtonHover(button, false);
    }

    private void OnButtonPressed(CustomMenuButton button)
    {
        GD.Print($"OnButtonPressed called for button: {button.Name}");
        HandleButtonPress(button);
    }

    private async void HandleButtonPress(CustomMenuButton button)
    {
        GD.Print($"HandleButtonPress - CanScroll: {CanScroll}, _canInteract: {_canInteract}");
        if (!CanScroll || !_canInteract)
        {
            GD.Print("Button press ignored due to interaction controls");
            return;
        }

        await ToSignal(GetTree().CreateTimer(0.1), "timeout");

        if (_buttonActions.TryGetValue(button.Name, out Action action))
        {
            GD.Print($"Executing action for button: {button.Name}");
            action?.Invoke();
        }
    }

    public void EnableButtonInteraction()
    {
        _canInteract = true;
        foreach (var button in _menuButtons)
        {
            button.IsEnabled = true;
        }
    }

    public void SetButtonsEnabled(bool enabled)
    {
        foreach (var button in _menuButtons)
        {
            button.IsEnabled = enabled;
        }
    }

    private void HandleButtonHover(CustomMenuButton button, bool isHovered)
    {
        GD.Print("HandleButtonHover: " + button.Name + " " + isHovered);
        if (!CanScroll || !_canInteract) return;

        _hoveredButton = isHovered ? button : null;
        Input.SetCustomMouseCursor(isHovered ? _cursorLink : _cursorArrow);
    }

    // Button handlers
    private void HandleNewGamePressed()
    {
        CanScroll = false;
        _gameSceneManager.StartNewGame();
    }

    private void HandleContinuePressed()
    {
        GD.Print("Continue pressed.");
    }

    private void HandleLoadGamePressed()
    {
        GD.Print("Load Game pressed.");
    }

    private void HandleOptionsPressed()
    {
        GD.Print("Options pressed.");
    }

    private void HandleQuitPressed()
    {
        GetTree().Quit();
    }
}