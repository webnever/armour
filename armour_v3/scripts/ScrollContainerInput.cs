using Godot;
using System;

// Direct scroll handler script to attach to ScrollContainer
public partial class ScrollContainerInput : ScrollContainer
{
    // How much to scroll per mouse wheel click
    [Export] private float _scrollSpeed = 100.0f;
    [Export] private float _pageScrollAmount = 300.0f;
    [Export] private bool _enableKeyboardScrolling = true;
    [Export] private bool _enableMouseWheelScrolling = true;
    
    // Debug options
    [Export] private bool _debugMode = true;
    
    // Prevent auto-scrolling after manual scroll
    private bool _userIsScrolling = false;
    private float _userScrollTimeout = 5.0f;  // Seconds to prevent auto-scroll after user interaction
    
    // Reference to VScrollBar
    private VScrollBar _vScrollBar;
    
    public override void _Ready()
    {
        // Enable input processing
        SetProcessInput(true);
        
        // Force mouse filter mode to ensure we capture input events
        MouseFilter = MouseFilterEnum.Stop;
        
        // Configure scrollbar to handle input
        _vScrollBar = GetVScrollBar();
        if (_vScrollBar != null)
        {
            _vScrollBar.SetProcessInput(true);
            
            // Set visible to ensure scrollbar is active
            _vScrollBar.Visible = true;
            
            if (_debugMode)
            {
                GD.Print($"ScrollContainerInput: Found VScrollBar, MaxValue = {_vScrollBar.MaxValue}");
            }
            
            // Hook up value changed signal if available
            if (_vScrollBar.HasSignal("value_changed"))
            {
                _vScrollBar.Connect("value_changed", new Callable(this, nameof(OnScrollValueChanged)));
            }
        }
        else
        {
            GD.Print("ScrollContainerInput: WARNING - No VScrollBar found!");
        }
        
        // Ensure we're capturing input properly
        FocusMode = FocusModeEnum.All;
        
        // Force proper sizing of content
        ForceUpdateScrollbars();
        
        // Debugging info
        GD.Print($"ScrollContainerInput: Initialized on {Name}, MouseFilter = {MouseFilter}");
        
        // Wait a moment then display max value (to check if content is sized properly)
        var timer = new Timer();
        AddChild(timer);
        timer.WaitTime = 1.0f;
        timer.OneShot = true;
        timer.Timeout += () => {
            if (_vScrollBar != null)
            {
                GD.Print($"ScrollContainerInput: After 1s, VScrollBar.MaxValue = {_vScrollBar.MaxValue}");
                
                // Force scrollbars to update again after layout completes
                ForceUpdateScrollbars();
                GD.Print($"ScrollContainerInput: After update, VScrollBar.MaxValue = {_vScrollBar.MaxValue}");
            }
            timer.QueueFree();
        };
        timer.Start();
    }
    
    // Force scrollbars to recalculate
    private void ForceUpdateScrollbars()
    {
        // Force a container resize
        var currentSize = Size;
        Size = new Vector2(currentSize.X, currentSize.Y + 0.1f); // Tiny change to force update
        Size = currentSize; // Restore original size
        
        // Get scrollbar values
        _vScrollBar = GetVScrollBar();
    }
    
    // Handle scrollbar value changes
    private void OnScrollValueChanged(float value)
    {
        if (_debugMode)
        {
            // Get call stack to find out who is changing the value
            System.Diagnostics.StackTrace stackTrace = new System.Diagnostics.StackTrace();
            string caller = stackTrace.ToString();
            
            GD.Print($"ScrollContainerInput: ScrollValue changed to {value}, userIsScrolling={_userIsScrolling}\nCall stack: {caller}");
        }
        
        // If this is not from user scrolling, and value is being set to something other than what we set,
        // we might need to mark this as user scrolling
        if (!_userIsScrolling && _vScrollBar != null && Math.Abs(value - _vScrollBar.MaxValue) > 10)
        {
            _userIsScrolling = true;
            _userScrollTimeout = 5.0f;
            
            if (_debugMode)
            {
                GD.Print($"ScrollContainerInput: External scroll detected, enabling user scrolling protection");
            }
        }
    }
    
    // Override the input handling for the ScrollContainer
    public override void _GuiInput(InputEvent @event)
    {
        if (_enableMouseWheelScrolling && @event is InputEventMouseButton mouseEvent)
        {
            if (mouseEvent.Pressed)
            {
                if (_debugMode)
                {
                    GD.Print($"ScrollContainerInput: Mouse input detected: {mouseEvent.ButtonIndex}");
                }
                
                int currentScroll = ScrollVertical;
                
                if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
                {
                    // Scroll up by scrollSpeed amount
                    int newPosition = Mathf.Max(0, currentScroll - (int)_scrollSpeed);
                    ScrollVertical = newPosition;
                    
                    // Mark that user is manually scrolling
                    _userIsScrolling = true;
                    _userScrollTimeout = 5.0f;
                    
                    if (_debugMode)
                    {
                        GD.Print($"ScrollContainerInput: Scrolled up from {currentScroll} to {newPosition} (user scrolling enabled)");
                    }
                    
                    GetViewport().SetInputAsHandled();
                    AcceptEvent();
                }
                else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
                {
                    // Get the max scroll value
                    float maxScroll = 0;
                    if (_vScrollBar != null)
                    {
                        maxScroll = (float)_vScrollBar.MaxValue;
                    }
                    
                    // Scroll down by scrollSpeed amount, clamped to max
                    int newPosition = Mathf.Min((int)maxScroll, currentScroll + (int)_scrollSpeed);
                    ScrollVertical = newPosition;
                    
                    // Mark that user is manually scrolling
                    _userIsScrolling = true;
                    _userScrollTimeout = 5.0f;
                    
                    if (_debugMode)
                    {
                        GD.Print($"ScrollContainerInput: Scrolled down from {currentScroll} to {newPosition} (max: {maxScroll}, user scrolling enabled)");
                    }
                    
                    GetViewport().SetInputAsHandled();
                    AcceptEvent();
                }
            }
        }
        
        // Let the parent handle other input
        base._GuiInput(@event);
    }
    
    public override void _UnhandledInput(InputEvent @event)
    {
        // Capture keyboard input for scrolling
        if (!_enableKeyboardScrolling)
            return;
        
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            if (_debugMode)
            {
                GD.Print($"ScrollContainerInput: Key input detected: {keyEvent.Keycode}");
            }
            
            bool handled = false;
            
            // PageUp key
            if ((int)keyEvent.Keycode == 16777235) // PageUp
            {
                ScrollVertical -= (int)_pageScrollAmount;
                handled = true;
            }
            // PageDown key
            else if ((int)keyEvent.Keycode == 16777236) // PageDown
            {
                ScrollVertical += (int)_pageScrollAmount;
                handled = true;
            }
            // Home key
            else if ((int)keyEvent.Keycode == 16777229) // Home
            {
                ScrollVertical = 0;
                handled = true;
            }
            // End key
            else if ((int)keyEvent.Keycode == 16777230) // End
            {
                // Maximum scrolling position
                if (_vScrollBar != null)
                {
                    ScrollVertical = (int)_vScrollBar.MaxValue;
                }
                handled = true;
            }
            
            // Set as handled if we processed a key
            if (handled)
            {
                if (_debugMode)
                {
                    GD.Print($"ScrollContainerInput: Keyboard scroll to {ScrollVertical}");
                }
                GetViewport().SetInputAsHandled();
                AcceptEvent();
            }
        }
    }
    
    public override void _Process(double delta)
    {
        // Update user scrolling timeout
        if (_userIsScrolling)
        {
            _userScrollTimeout -= (float)delta;
            if (_userScrollTimeout <= 0)
            {
                _userIsScrolling = false;
                if (_debugMode)
                {
                    GD.Print("ScrollContainerInput: User scrolling timeout ended, auto-scroll enabled again");
                }
            }
        }
        
        // Check if VScrollBar is valid during runtime, in case it gets created after Ready
        if (_vScrollBar == null || _vScrollBar.MaxValue <= 0)
        {
            _vScrollBar = GetVScrollBar();
            if (_vScrollBar != null && _debugMode)
            {
                GD.Print($"ScrollContainerInput: Found VScrollBar in Process, MaxValue = {_vScrollBar.MaxValue}");
                
                // If maxValue is still 0, try forcing container update
                if (_vScrollBar.MaxValue <= 0)
                {
                    ForceUpdateScrollbars();
                }
                
                // Connect to value changed signal if we haven't already
                if (_vScrollBar.HasSignal("value_changed") && 
                    !_vScrollBar.IsConnected("value_changed", new Callable(this, nameof(OnScrollValueChanged))))
                {
                    _vScrollBar.Connect("value_changed", new Callable(this, nameof(OnScrollValueChanged)));
                }
            }
        }
        
        // Debug current scroll position periodically
        if (_debugMode && _vScrollBar != null && (float)delta > 0.1f) // Only print occasionally
        {
            // Show current scroll values if they've changed
            if (_vScrollBar.Value != ScrollVertical)
            {
                GD.Print($"ScrollContainerInput: Current scroll - VScrollBar.Value = {_vScrollBar.Value}, ScrollVertical = {ScrollVertical}, MaxValue = {_vScrollBar.MaxValue}");
            }
        }
    }
    
    // Public method to scroll to bottom
    public void ScrollToBottom()
    {
        // If user is manually scrolling, don't auto-scroll
        if (_userIsScrolling)
        {
            if (_debugMode)
            {
                GD.Print("ScrollContainerInput: ScrollToBottom called but ignored due to user scrolling");
            }
            return;
        }
        
        // Refresh scrollbar reference
        if (_vScrollBar == null)
        {
            _vScrollBar = GetVScrollBar();
        }
        
        if (_vScrollBar != null)
        {
            // Force update to ensure we have the correct max value
            ForceUpdateScrollbars();
            
            float maxValue = (float)_vScrollBar.MaxValue;
            ScrollVertical = (int)maxValue;
            
            if (_debugMode)
            {
                GD.Print($"ScrollContainerInput: Scrolled to bottom: {maxValue}");
            }
        }
        else
        {
            GD.PrintErr("ScrollContainerInput: Cannot scroll to bottom - no scrollbar found");
        }
    }
    
    // Method to ensure ScrollContainer and its content are properly sized
    public void UpdateSize()
    {
        // Find first Control child
        Control content = null;
        foreach (var child in GetChildren())
        {
            if (child is Control control)
            {
                content = control;
                break;
            }
        }
        
        if (content != null)
        {
            var contentSize = content.Size;
            
            // Print debugging information
            if (_debugMode)
            {
                GD.Print($"ScrollContainerInput: Content size = {contentSize}, Container size = {Size}");
            }
            
            // Make sure content is positioned at the top
            content.Position = new Vector2(0, 0);
            
            // Force update the scrollbars
            ForceUpdateScrollbars();
        }
    }
}