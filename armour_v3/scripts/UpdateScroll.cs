using Godot;
using System;

public partial class UpdateScroll : VBoxContainer
{
    // Configuration
    [Export] private float _scrollSpeed = 15.0f;
    [Export] private bool _autoScrollToBottom = true;
    [Export] private bool _enableManualScrolling = true;
    [Export] private NodePath _scrollContainerPath = "../"; // Default to parent
    [Export] private bool _resetManualScrollOnCommand = true;
    
    // Scrolling state
    private float _targetScrollPosition = 0f;
    private float _currentScrollPosition = 0f;
    private bool _isManuallyScrolling = false;
    
    // Parent ScrollContainer reference (if available)
    private ScrollContainer _scrollContainer;
    
    // Screen size reference
    private float _visibleHeight = 600f;
    
    public override void _Ready()
    {
        // In your specific structure, the path is "../../ScrollContainer"
        _scrollContainer = GetNodeOrNull<ScrollContainer>("../../ScrollContainer");
        
        // If not found, try the root CLIEmulator node for the path "Render/ScrollContainer"
        if (_scrollContainer == null)
        {
            var root = GetTree().Root.GetNode<Node>("CLIEmulator");
            if (root != null)
            {
                _scrollContainer = root.GetNodeOrNull<ScrollContainer>("Render/ScrollContainer");
            }
        }
        
        // If still not found, try direct path based on your structure
        if (_scrollContainer == null)
        {
            var render = GetNodeOrNull<Node>("../../../");
            if (render != null && render.Name == "Render")
            {
                _scrollContainer = render.GetNodeOrNull<ScrollContainer>("ScrollContainer");
            }
        }
        
        // If no ScrollContainer is found, we'll use the legacy position-based approach
        if (_scrollContainer == null)
        {
            GD.Print("UpdateScroll: No ScrollContainer found, using position-based scrolling. Current path: " + GetPath());
            
            // One more attempt - print all parent node names for debugging
            var debugParent = GetParent();
            string parentChain = "Parent chain: ";
            while (debugParent != null)
            {
                parentChain += debugParent.Name + " (" + debugParent.GetType().Name + ") -> ";
                debugParent = debugParent.GetParent();
            }
            GD.Print(parentChain);
        }
        else
        {
            GD.Print("UpdateScroll: Found ScrollContainer: " + _scrollContainer.Name);
        }
        
        // Update visible height based on viewport size
        Viewport viewport = GetViewport();
        if (viewport != null)
        {
            _visibleHeight = viewport.GetVisibleRect().Size.Y;
        }
    }
    
    public override void _Process(double delta)
    {
        float deltaf = (float)delta;
        
        if (_scrollContainer != null)
        {
            // Use ScrollContainer for proper scrolling
            HandleScrollContainerScrolling(deltaf);
        }
        else
        {
            // Legacy position-based scrolling
            HandlePositionBasedScrolling();
        }
    }
    
    // Enable direct input handling for ScrollContainer scrolling
    public override void _GuiInput(InputEvent @event)
    {
        if (!_enableManualScrolling || _scrollContainer == null)
            return;
            
        // Handle mouse wheel scrolling manually
        if (@event is InputEventMouseButton mouseEvent)
        {
            float scrollStep = 50.0f; // Pixels to scroll per wheel click
            
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                _scrollContainer.ScrollVertical -= (int)scrollStep;
                _isManuallyScrolling = true;
                GetViewport().SetInputAsHandled();
                AcceptEvent(); // Important: Accept the event to prevent further propagation
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                _scrollContainer.ScrollVertical += (int)scrollStep;
                _isManuallyScrolling = true;
                GetViewport().SetInputAsHandled();
                AcceptEvent(); // Important: Accept the event to prevent further propagation
            }
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        if (!_enableManualScrolling || _scrollContainer == null)
            return;
            
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            float pageScrollAmount = 300.0f; // Amount to scroll for page up/down
            float lineScrollAmount = 50.0f;  // Amount to scroll for arrow keys
            bool handled = false;
            
            // Get max scroll range
            float maxScroll = (float)_scrollContainer.GetVScrollBar().MaxValue;
            
            // Handle PAGE_UP key
            if ((int)keyEvent.Keycode == 16777235) // PageUp
            {
                _scrollContainer.ScrollVertical -= (int)pageScrollAmount;
                handled = true;
            }
            // Handle PAGE_DOWN key
            else if ((int)keyEvent.Keycode == 16777236) // PageDown
            {
                _scrollContainer.ScrollVertical += (int)pageScrollAmount;
                handled = true;
            }
            // Handle Home key
            else if ((int)keyEvent.Keycode == 16777229) // Home
            {
                _scrollContainer.ScrollVertical = 0;
                handled = true;
            }
            // Handle End key
            else if ((int)keyEvent.Keycode == 16777230) // End
            {
                _scrollContainer.ScrollVertical = (int)maxScroll;
                handled = true;
            }
            // Handle arrow up (for scrolling)
            else if ((int)keyEvent.Keycode == 16777232 && keyEvent.ShiftPressed) // Up + Shift
            {
                _scrollContainer.ScrollVertical -= (int)lineScrollAmount;
                handled = true;
            }
            // Handle arrow down (for scrolling)
            else if ((int)keyEvent.Keycode == 16777234 && keyEvent.ShiftPressed) // Down + Shift
            {
                _scrollContainer.ScrollVertical += (int)lineScrollAmount;
                handled = true;
            }
            
            // Mark as manually scrolling and handle the event if needed
            if (handled)
            {
                _isManuallyScrolling = true;
                GetViewport().SetInputAsHandled();
            }
        }
    }
    
    private void HandleScrollContainerScrolling(float delta)
    {
        // If we should auto-scroll to bottom and not manually scrolling
        if (_autoScrollToBottom && !_isManuallyScrolling)
        {
            // Safety check to ensure we have a valid scrollbar
            var vScrollBar = _scrollContainer.GetVScrollBar();
            if (vScrollBar != null)
            {
                // Get the scroll range and calculate the bottom position
                float maxScroll = (float)vScrollBar.MaxValue;
                _targetScrollPosition = maxScroll;
                
                // Smoothly scroll to the target (explicit cast for the multiplication)
                float lerpFactor = delta * _scrollSpeed;
                _currentScrollPosition = Mathf.Lerp(_currentScrollPosition, _targetScrollPosition, lerpFactor);
                _scrollContainer.ScrollVertical = (int)_currentScrollPosition;
            }
        }
    }
    
    private void HandlePositionBasedScrolling()
    {
        // Legacy approach - adjust position based on content size
        float contentHeight = Size.Y;
        float overflow = Mathf.Max(0f, contentHeight - _visibleHeight);
        
        if (_autoScrollToBottom && !_isManuallyScrolling)
        {
            // Set position to show the bottom of the content
            Position = new Vector2(Position.X, -overflow);
        }
        // If manually scrolling, we'll let the user control it via mouse/keyboard
    }
    
    // Public method to scroll to bottom immediately
    public void ScrollToBottom()
    {
        if (_scrollContainer != null)
        {
            var vScrollBar = _scrollContainer.GetVScrollBar();
            if (vScrollBar != null)
            {
                float maxScroll = (float)vScrollBar.MaxValue;
                _scrollContainer.ScrollVertical = (int)maxScroll;
                _currentScrollPosition = maxScroll;
                _targetScrollPosition = maxScroll;
            }
        }
        else
        {
            float contentHeight = Size.Y;
            float overflow = Mathf.Max(0f, contentHeight - _visibleHeight);
            Position = new Vector2(Position.X, -overflow);
        }
    }
    
    // Public method to explicitly set the ScrollContainer reference
    public void SetScrollContainer(ScrollContainer scrollContainer)
    {
        _scrollContainer = scrollContainer;
        if (_scrollContainer != null)
        {
            GD.Print("UpdateScroll: ScrollContainer manually set to " + _scrollContainer.Name);
        }
    }
    
    // Add a method to reset manual scrolling
    public void ResetManualScrollOnCommand()
    {
        if (_resetManualScrollOnCommand)
        {
            _isManuallyScrolling = false;
            ScrollToBottom();
        }
    }
}