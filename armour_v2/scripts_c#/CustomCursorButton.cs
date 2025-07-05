using Godot;
using System;

public partial class CustomCursorButton : Button
{
    private Resource normalCursor;
    private Resource hoverCursor;
    private Vector2 cursorHotspot = Vector2.Zero;
    private bool isHovered = false;

    [Export]
    public string NormalCursorPath { get; set; } = "";
    
    [Export]
    public string HoverCursorPath { get; set; } = "";
    
    [Export]
    public Vector2 CursorHotspot
    {
        get => cursorHotspot;
        set
        {
            cursorHotspot = value;
            if (isHovered)
            {
                UpdateCursor();
            }
        }
    }

    public override void _Ready()
    {
        // Debug information
        GD.Print($"Button {Name} _Ready called");
        GD.Print($"Initial size: {Size}");
        GD.Print($"Mouse filter: {MouseFilter}");
        GD.Print($"Visible: {Visible}");
        GD.Print($"Global position: {GlobalPosition}");

        // Load cursor textures if paths are provided
        if (!string.IsNullOrEmpty(NormalCursorPath))
        {
            normalCursor = ResourceLoader.Load(NormalCursorPath);
        }
        if (!string.IsNullOrEmpty(HoverCursorPath))
        {
            hoverCursor = ResourceLoader.Load(HoverCursorPath);
        }

        // Set up default button properties
        Flat = true;
        FocusMode = FocusModeEnum.None;
        MouseDefaultCursorShape = CursorShape.Arrow;
        MouseFilter = MouseFilterEnum.Stop;

        // Ensure the button has a minimum size
        CustomMinimumSize = new Vector2(150, 30);

        var parent = GetParent();
        while (parent != null)
        {
            if (parent is Control control)
            {
                control.Visible = true;
                control.MouseFilter = control is VBoxContainer ? MouseFilterEnum.Ignore : MouseFilterEnum.Stop;
            }
            parent = parent.GetParent();
        }
        
        // Connect signals
        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;

        // Force layout update
        NotifyPropertyListChanged();
        QueueRedraw();

        // Debug print parent hierarchy
        PrintParentHierarchy();
    }

    private void PrintParentHierarchy()
    {
        Node current = this;
        GD.Print("Parent hierarchy:");
        while (current != null)
        {
            if (current is Control control)
            {
                GD.Print($"- {current.Name}: Visible={control.Visible}, MouseFilter={control.MouseFilter}, Size={control.Size}");
            }
            else
            {
                GD.Print($"- {current.Name}");
            }
            current = current.GetParent();
        }
    }

    public override void _Process(double delta)
    {
        // Debug mouse position relative to button
        var mousePos = GetLocalMousePosition();
        if (new Rect2(Vector2.Zero, Size).HasPoint(mousePos))
        {
            GD.Print($"Mouse is over button at local position: {mousePos}");
        }
    }

    public override void _GuiInput(InputEvent @event)
    {
        base._GuiInput(@event);  // Make sure to call base
        
        GD.Print($"Button GuiInput: {@event.GetType()}");
        GetViewport().SetInputAsHandled();  // Add this to ensure input isn't being eaten by other controls
    }

    private void OnMouseEntered()
    {
        GD.Print("MouseEntered");
        isHovered = true;
        UpdateCursor();
    }

    private void OnMouseExited()
    {
        GD.Print("MouseEntered");
        isHovered = false;
        UpdateCursor();
    }

    private void UpdateCursor()
    {
        if (isHovered && !Disabled)
        {
            // Use hover cursor if available, otherwise use normal cursor
            var cursorTexture = hoverCursor ?? normalCursor;
            if (cursorTexture != null)
            {
                Input.SetCustomMouseCursor(cursorTexture, Input.CursorShape.Arrow, cursorHotspot);
            }
        }
        else
        {
            // Use normal cursor when not hovered or disabled
            if (normalCursor != null)
            {
                Input.SetCustomMouseCursor(normalCursor, Input.CursorShape.Arrow, cursorHotspot);
            }
            else
            {
                Input.SetCustomMouseCursor(null); // Reset to default system cursor
            }
        }
    }

    public override void _ExitTree()
    {
        // Reset cursor when button is removed from scene
        Input.SetCustomMouseCursor(null);
        base._ExitTree();
    }

    public void SetCursorTextures(string normalPath, string hoverPath = "")
    {
        if (!string.IsNullOrEmpty(normalPath))
        {
            NormalCursorPath = normalPath;
            normalCursor = ResourceLoader.Load(normalPath);
        }
        
        if (!string.IsNullOrEmpty(hoverPath))
        {
            HoverCursorPath = hoverPath;
            hoverCursor = ResourceLoader.Load(hoverPath);
        }
        
        if (isHovered)
        {
            UpdateCursor();
        }
    }
}