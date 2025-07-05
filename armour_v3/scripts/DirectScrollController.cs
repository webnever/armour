using Godot;
using System;

// Enhanced version of DirectScrollController
public partial class DirectScrollController : ScrollContainer
{
    [Export] private float _scrollStep = 100f;
    [Export] private bool _debugMode = true;
    [Export] private bool _hideScrollbar = true; // Option to hide the scrollbar
    [Export] private bool _alwaysScrollToBottom = true; // Force scrolling to bottom on new content
    [Export] private bool _resetManualScrollOnCommand = true; // New flag to control behavior
    
    // Keep track of manual scrolling state
    private bool _userScrolling = false;
    
    // For tracking forced value changes
    private bool _isSettingValue = false;
    
    // Reference to VScrollBar
    private VScrollBar _vScrollBar;
    
    // The previous child count, used to detect new content
    private int _lastChildCount = 0;
    private float _contentChangeCheckDelay = 0.0f;
    
    // Content container reference
    private Node _contentContainer;
    
    // Signal to communicate with AdvancedCLIEmulator
    [Signal]
    public delegate void ScrollCompletedEventHandler();
    
    public override void _Ready()
    {
        // Get scrollbar reference immediately
        _vScrollBar = GetVScrollBar();
        
        // Find the content container (usually a VBoxContainer)
        if (GetChildCount() > 0)
        {
            _contentContainer = GetChild(0);
            _lastChildCount = _contentContainer.GetChildCount();
            
            if (_debugMode)
            {
                GD.Print($"DirectScrollController: Found content container '{_contentContainer.Name}' with {_lastChildCount} children");
            }
        }
        
        // Hide the scrollbar but keep its functionality
        if (_vScrollBar != null && _hideScrollbar)
        {
            // Make scrollbar invisible but keep it functional
            _vScrollBar.Modulate = new Color(1, 1, 1, 0); // Fully transparent
            
            // Set custom minimum size to 0 to reduce space taken
            _vScrollBar.CustomMinimumSize = new Vector2(1, 0);
            
            if (_debugMode)
            {
                GD.Print($"DirectScrollController: Found VScrollBar, MaxValue = {_vScrollBar.MaxValue}, made invisible");
            }
        }
        
        // Set up mouse filter
        MouseFilter = MouseFilterEnum.Stop;
        
        // Print debug info
        if (_debugMode)
        {
            GD.Print($"DirectScrollController: Initialized on {Name}");
        }
        
        // Initial scroll to bottom
        CallDeferred(nameof(ForceScrollToBottom));
    }
    
    public override void _Process(double delta)
    {
        // Ensure we have the VScrollBar reference
        if (_vScrollBar == null)
        {
            _vScrollBar = GetVScrollBar();
        }
        
        // Check if content size has changed by monitoring child count
        CheckContentChange((float)delta);
    }
    
    // Improved check for content changes with more robust detection
    private void CheckContentChange(float delta)
    {
        // Update delay counter
        _contentChangeCheckDelay -= delta;
        if (_contentChangeCheckDelay > 0)
            return;
            
        _contentChangeCheckDelay = 0.1f; // Check more frequently (was 0.2)
        
        // Ensure we have content container
        if (_contentContainer == null && GetChildCount() > 0)
        {
            _contentContainer = GetChild(0);
        }
        
        if (_contentContainer == null)
            return;
            
        // Get current child count
        int childCount = _contentContainer.GetChildCount();
        
        // If new content was added
        if (childCount > _lastChildCount && _lastChildCount > 0)
        {
            if (_debugMode)
            {
                GD.Print($"DirectScrollController: Content changed from {_lastChildCount} to {childCount} items");
            }
            
            // If always scroll to bottom is enabled or we're not user scrolling
            if (_alwaysScrollToBottom || !_userScrolling)
            {
                // Important: Use CallDeferred twice to ensure it happens after layout
                CallDeferred(nameof(DeferredScrollToBottom));
            }
        }
        
        _lastChildCount = childCount;
    }
    
    // Deferred scroll to bottom - helps with timing issues
    private void DeferredScrollToBottom()
    {
        CallDeferred(nameof(ForceScrollToBottom));
    }
    
    // New method: Force scroll to bottom regardless of user scrolling state
    public void ForceScrollToBottom()
    {
        // Ensure we have the VScrollBar
        if (_vScrollBar == null)
        {
            _vScrollBar = GetVScrollBar();
            if (_vScrollBar == null)
            {
                GD.PrintErr("DirectScrollController: Cannot force scroll - no VScrollBar found");
                return;
            }
        }
        
        float maxValue = (float)_vScrollBar.MaxValue;
        
        // Set the value directly
        _isSettingValue = true;
        ScrollVertical = (int)maxValue;
        _isSettingValue = false;
        
        if (_debugMode)
        {
            GD.Print($"DirectScrollController: Force scrolled to bottom ({maxValue})");
        }
        
        // Emit signal to notify that scrolling is complete
        EmitSignal(SignalName.ScrollCompleted);
    }
    
    // Handle GUI input events
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && mouseEvent.Pressed)
        {
            // Process mouse wheel scrolling
            if (mouseEvent.ButtonIndex == MouseButton.WheelUp)
            {
                HandleMouseScroll(-_scrollStep);
                AcceptEvent();
            }
            else if (mouseEvent.ButtonIndex == MouseButton.WheelDown)
            {
                HandleMouseScroll(_scrollStep);
                AcceptEvent();
            }
        }
        
        // Let parent handle other events
        base._GuiInput(@event);
    }
    
    // Handle mouse wheel scrolling
    private void HandleMouseScroll(float delta)
    {
        // Enable user scrolling mode
        _userScrolling = true;
        
        // Calculate new position
        int oldPosition = ScrollVertical;
        int newPosition;
        
        if (delta < 0)
        {
            // Scrolling up
            newPosition = Mathf.Max(0, oldPosition + (int)delta);
        }
        else
        {
            // Scrolling down
            float maxScroll = _vScrollBar != null ? (float)_vScrollBar.MaxValue : 1000f;
            newPosition = Mathf.Min((int)maxScroll, oldPosition + (int)delta);
            
            // If scrolled to bottom, disable user scrolling
            if (newPosition >= maxScroll - 10)
            {
                _userScrolling = false;
            }
        }
        
        // Set new position
        _isSettingValue = true;
        ScrollVertical = newPosition;
        _isSettingValue = false;
        
        if (_debugMode)
        {
            GD.Print($"DirectScrollController: User scrolled from {oldPosition} to {newPosition}");
        }
    }
    
    // Public method to notify that content was added
    public void NotifyContentAdded()
    {
        if (_alwaysScrollToBottom || !_userScrolling)
        {
            CallDeferred(nameof(ForceScrollToBottom));
            
            if (_debugMode)
            {
                GD.Print("DirectScrollController: Content added notification received, scrolling to bottom");
            }
        }
    }
    
    // Add a method to reset manual scrolling state
    public void ResetManualScrolling()
    {
        if (_resetManualScrollOnCommand)
        {
            _userScrolling = false;
            ForceScrollToBottom();
        }
    }
}