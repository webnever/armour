using Godot;
using System;
using System.Collections.Generic;

public partial class ContextMenu : Control
{
    private VBoxContainer menuItems;
    private NinePatchRect background;
    private List<Button> actionButtons = new();
    private Dictionary<Button, ContextMenuAction> buttonActions = new();

    public override void _Ready()
    {
        background = GetNode<NinePatchRect>("NinePatchRect");
        menuItems = GetNode<VBoxContainer>("NinePatchRect/MarginContainer/MenuItems");

        // Remove fixed minimum size
        // CustomMinimumSize = new Vector2(150, 0);
        
        ProcessMode = ProcessModeEnum.Always;

        // Set proper focus modes
        FocusMode = FocusModeEnum.None;
        background.FocusMode = FocusModeEnum.None;
        menuItems.FocusMode = FocusModeEnum.None;

        // Allow events to pass through to children
        MouseFilter = MouseFilterEnum.Pass;
        background.MouseFilter = MouseFilterEnum.Pass;
        menuItems.MouseFilter = MouseFilterEnum.Pass;

        // Set size flags to expand
        SizeFlagsHorizontal = Control.SizeFlags.Fill;
        SizeFlagsVertical = Control.SizeFlags.Fill;
        
        Hide();
    }

    public void ShowMenu(Vector2 position, List<ContextMenuAction> actions)
    {
        // Clear existing buttons
        foreach (var button in actionButtons)
        {
            button.QueueFree();
        }
        actionButtons.Clear();
        buttonActions.Clear();

        // Create new buttons
        foreach (var action in actions)
        {
            CreateButton(action);
        }

        // Force minimum size update
        CustomMinimumSize = menuItems.GetCombinedMinimumSize() + new Vector2(40, 40); // Add padding
        Size = CustomMinimumSize;

        Position = position;
        AdjustPositionToViewport();
        Show();
        
        // Debug the sizes after showing
        GD.Print($"Menu size after update: {Size}");
        GD.Print($"MenuItems size: {menuItems.Size}");
        GD.Print($"Background size: {background.Size}");
    }

    private void CreateButton(ContextMenuAction action)
    {
        var button = new Button();
        button.Text = action.Label;
        button.Disabled = !action.IsEnabled;

        // Set proper size flags for the button
        button.SizeFlagsHorizontal = Control.SizeFlags.Fill;
        button.CustomMinimumSize = new Vector2(150, 30); // Ensure minimum height

        SetupButtonAppearance(button);
        
        GD.Print($"Creating button: {action.Label}");
        GD.Print($"  - Action enabled: {action.IsEnabled}");
        GD.Print($"  - Has callback: {action.Callback != null}");
        
        // Store the action
        buttonActions[button] = action;
        
        // Connect both pressed and gui_input signals
        button.Pressed += OnButtonPressed;
        button.GuiInput += OnButtonGuiInput;

        // Change mouse filter to allow events
        button.MouseFilter = MouseFilterEnum.Pass;
        button.ProcessMode = ProcessModeEnum.Always;

        menuItems.AddChild(button);
        actionButtons.Add(button);
        
        GD.Print($"Button {action.Label} setup complete");
        GD.Print($"  - Size: {button.Size}");
        GD.Print($"  - MinSize: {button.CustomMinimumSize}");
        GD.Print($"  - MouseFilter: {button.MouseFilter}");
        GD.Print($"  - ProcessMode: {button.ProcessMode}");
        GD.Print($"  - Parent: {button.GetParent()?.Name}");
    }

    private void OnButtonGuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            var button = GetNodeOrNull<Button>("."); // Gets the current button node
            if (button != null)
            {
                GD.Print($"Button GuiInput received for: {button.Text}");
                GD.Print($"Mouse Position: {mouseEvent.Position}");
                GD.Print($"Button Rect: {button.GetGlobalRect()}");
                // Don't stop the event propagation here
            }
        }
    }

    private void OnButtonPressed()
    {
        // Get the button that was pressed
        if (GetViewport().GuiGetFocusOwner() is Button pressedButton)
        {
            GD.Print($"Button pressed: {pressedButton.Text}");
            ExecuteButtonAction(pressedButton);
        }
    }

    private void ExecuteButtonAction(Button button)
    {
        GD.Print($"Attempting to execute action for button: {button.Text}");
        
        if (buttonActions.TryGetValue(button, out var action))
        {
            GD.Print($"Found action for button {button.Text}");
            
            if (action.Callback == null)
            {
                GD.PrintErr($"Warning: Callback is null for button {button.Text}");
                return;
            }

            GD.Print($"Invoking callback for button {button.Text}");
            try
            {
                action.Callback.Invoke();
                GD.Print($"Successfully invoked callback for {button.Text}");
            }
            catch (Exception e)
            {
                GD.PrintErr($"Error executing callback for button {button.Text}: {e.Message}");
                GD.PrintErr(e.StackTrace);
            }

            Hide();
            MouseFilter = MouseFilterEnum.Ignore;
        }
        else
        {
            GD.PrintErr($"No action found for button {button.Text}");
            // Debug dump of all registered actions
            GD.Print("Registered button actions:");
            foreach (var kvp in buttonActions)
            {
                GD.Print($"  Button: {kvp.Key.Text}, Has Callback: {kvp.Value.Callback != null}");
            }
        }
    }

    private void SetupButtonAppearance(Button button)
    {
        GD.Print($"Setting up appearance for button: {button.Text}");
        
        button.Flat = true;
        button.Alignment = HorizontalAlignment.Left;
        
        // Make sure button can receive focus and input
        button.FocusMode = FocusModeEnum.All;
        button.MouseFilter = MouseFilterEnum.Stop;
        button.MouseDefaultCursorShape = CursorShape.PointingHand;
        
        GD.Print("Loading theme resources...");
        var theme = new Theme();
        var font = ResourceLoader.Load<FontFile>("res://fonts/MSMINCHO.TTF");
        theme.DefaultFont = font;
        theme.DefaultFontSize = 24;
        button.Theme = theme;

        // Set up button styles
        button.AddThemeConstantOverride("h_separation", 24);
        button.AddThemeColorOverride("font_color", new Color(1.0f, 1.0f, 1.0f));
        button.AddThemeColorOverride("font_hover_color", new Color(0.8f, 0.8f, 1.0f));
        button.AddThemeColorOverride("font_disabled_color", new Color(0.5f, 0.5f, 0.5f));

        var normalStyle = new StyleBoxFlat();
        normalStyle.BgColor = Colors.Transparent;
        normalStyle.ContentMarginLeft = 24;
        button.AddThemeStyleboxOverride("normal", normalStyle);

        var hoverStyle = new StyleBoxFlat();
        hoverStyle.BgColor = new Color(1, 1, 1, 0.1f);
        hoverStyle.ContentMarginLeft = 24;
        button.AddThemeStyleboxOverride("hover", hoverStyle);

        var pressedStyle = new StyleBoxFlat();
        pressedStyle.BgColor = new Color(1, 1, 1, 0.2f);
        pressedStyle.ContentMarginLeft = 24;
        button.AddThemeStyleboxOverride("pressed", pressedStyle);
        
        GD.Print("Button appearance setup complete");
    }

    private void AdjustPositionToViewport()
    {
        var viewport = GetViewportRect();
        if (Position.X + Size.X > viewport.Size.X)
        {
            Position = new Vector2(viewport.Size.X - Size.X, Position.Y);
        }
        if (Position.Y + Size.Y > viewport.Size.Y)
        {
            Position = new Vector2(Position.X, viewport.Size.Y - Size.Y);
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            var globalPos = GetGlobalMousePosition();
            var menuRect = GetGlobalRect();
            
            GD.Print($"Menu Rect: pos({menuRect.Position}), size({menuRect.Size})");
            GD.Print($"Mouse Position: {globalPos}");
            GD.Print($"Is point in rect? {menuRect.HasPoint(globalPos)}");

            // Debug each button's rect
            foreach (var button in actionButtons)
            {
                var buttonRect = button.GetGlobalRect();
                GD.Print($"Button '{button.Text}' Rect: pos({buttonRect.Position}), size({buttonRect.Size})");
                GD.Print($"Button in mouse? {buttonRect.HasPoint(globalPos)}");
            }
            
            // Check if we're really outside
            bool isOutside = !menuRect.HasPoint(globalPos);
            if (isOutside)
            {
                // Double check with the background rect
                var bgRect = background.GetGlobalRect();
                isOutside = !bgRect.HasPoint(globalPos);
                
                if (isOutside)
                {
                    GD.Print("Click outside menu confirmed");
                    Hide();
                    MouseFilter = MouseFilterEnum.Ignore;
                }
            }
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (!Visible) return;

        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            var globalPos = GetGlobalMousePosition();
            var menuRect = GetGlobalRect();
            var bgRect = background.GetGlobalRect();

            // Only process if truly outside both the menu and its background
            if (!menuRect.HasPoint(globalPos) && !bgRect.HasPoint(globalPos))
            {
                GD.Print("Click outside menu detected (global)");
                Hide();
                MouseFilter = MouseFilterEnum.Ignore;
                GetViewport().SetInputAsHandled();
            }
        }
    }



    public override void _ExitTree()
    {
        // Clean up button signals when the menu is removed
        foreach (var button in actionButtons)
        {
            button.Pressed -= OnButtonPressed;
            button.GuiInput -= OnButtonGuiInput;
        }
        base._ExitTree();
    }
}