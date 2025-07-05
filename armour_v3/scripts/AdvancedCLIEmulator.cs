using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Updated AdvancedCLIEmulator with comprehensive media system
public partial class AdvancedCLIEmulator : Control
{
    // UI Components
    private VBoxContainer _vContainer;
    private LineEdit _currentLineEdit;
    private ScrollContainer _scrollContainer;
    private DirectScrollController _scrollController;
    private UpdateScroll _updateScroll;
    private MediaManager _mediaManager; // NEW: Media manager
    private ImprovedUI _improvedUI; // NEW: UI system
    private Node _sceneNode; // NEW: Scene node for 3D scene instantiation
    private ColorRect _fadeOverlay; // NEW: Fade overlay for startup animation
    private Node _vhsFilter; // NEW: VHS filter node reference
    
    // Game Systems
    private CommandProcessor _commandProcessor;
    private GameState _gameState;
    private GameResourceLoader _resourceLoader;
    
    // Scene paths
    private Dictionary<string, string> _scenes = new Dictionary<string, string>
    {
        { "add_line", "res://scenes/add_line.tscn" },
        { "prompt", "res://scenes/command_prompt.tscn" }
    };
    
    // Configuration
    [Export] private Color _defaultTextColor = Colors.LightGreen;
    [Export] private Color _systemTextColor = Colors.Yellow;
    [Export] private Color _errorTextColor = Colors.Red;
    [Export] private Color _inputTextColor = Colors.White;
    [Export] private string _prompt = "> ";
    [Export] private string _introText = "";
    [Export] private string _gameName = "ARMOUR";
    [Export] private string _versionText = "v0";
    [Export] private string _configPath = "game_data/config.json";
    [Export] private bool _useClassicTerminalStyle = true;
    
    // History
    private List<string> _commandHistory = new List<string>();
    private int _historyIndex = -1;
    
    // For timed events
    private float _timeSinceLastUpdate = 0;
    private const float UPDATE_INTERVAL = 1.0f; // Update game state every second
    
    // Indicates content has been added and needs scrolling
    private bool _contentAdded = false;
    private Timer _scrollTimer;
    
    public override void _Ready()
    {
        // Create fade overlay first
        CreateFadeOverlay();
        
        // Initialize the scroll timer for delayed scrolling
        _scrollTimer = new Timer();
        AddChild(_scrollTimer);
        _scrollTimer.WaitTime = 0.05f;
        _scrollTimer.OneShot = true;
        _scrollTimer.Timeout += OnScrollTimerTimeout;
        
        // Get UI container
        _vContainer = GetNode<VBoxContainer>("ScrollContainer/VBoxContainer");
        
        // Find and store ScrollContainer
        _scrollContainer = GetNode<ScrollContainer>("ScrollContainer");
        
        // Get the DirectScrollController if it exists
        _scrollController = GetNodeOrNull<DirectScrollController>("ScrollContainer");
        
        // Get the UpdateScroll if it exists
        _updateScroll = GetNodeOrNull<UpdateScroll>("ScrollContainer/VBoxContainer");
        
        // NEW: Get or create scene node for 3D scenes
        _sceneNode = GetNodeOrNull<Node>("scene");
        if (_sceneNode == null)
        {
            _sceneNode = new Node();
            _sceneNode.Name = "scene";
            AddChild(_sceneNode);
            GD.Print("AdvancedCLIEmulator: Created scene node for 3D scene instantiation");
        }
        else
        {
            GD.Print("AdvancedCLIEmulator: Found existing scene node");
        }
        
        // NEW: Get VHS filter node reference
        _vhsFilter = GetNodeOrNull<Node>("CanvasLayer/VHS");
        if (_vhsFilter != null)
        {
            GD.Print("AdvancedCLIEmulator: Found VHS filter node");
        }
        else
        {
            GD.Print("AdvancedCLIEmulator: VHS filter node not found at CanvasLayer/VHS");
        }
        
        // Initialize game systems FIRST
        _resourceLoader = new GameResourceLoader(_configPath);
        _gameState = new GameState(_resourceLoader);
        _commandProcessor = new CommandProcessor(_gameState);
        
        // Initialize SceneLoadingManager
        var sceneLoadingManager = new SceneLoadingManager();
        AddChild(sceneLoadingManager);
        
        // THEN initialize media manager with valid game state
        _mediaManager = new MediaManager();
        AddChild(_mediaManager);
        _mediaManager.Initialize(this, _gameState);
        _mediaManager.MediaViewerClosed += OnMediaViewerClosed;
        
        // NEW: Set MediaManager reference in GameState
        _gameState.SetMediaManager(_mediaManager);
        
        // Initialize ImprovedUI system
        _improvedUI = new ImprovedUI(this);
        
        // Set the CLI emulator reference in command processor for image viewing
        _commandProcessor.SetCLIEmulator(this);
        
        // Apply terminal styling
        if (_useClassicTerminalStyle)
        {
            ApplyTerminalStyling();
        }
        
        // Set up the UI
        DisplayStartupScreen();

        DisplayOutput(_gameState.GetCurrentLocationDescription());
        
        // Add initial command prompt
        AddNewCommandPrompt();

        // Critical: Ensure focus is grabbed after a short delay
        CallDeferred(nameof(InitialFocusGrab));
        
        // Force a scroll to bottom after setup
        CallDeferred(nameof(ForceScrollToBottom));
        
        // Start fade in animation
        StartFadeIn();
    }
    
    // NEW: Create fade overlay
    private void CreateFadeOverlay()
    {
        _fadeOverlay = new ColorRect();
        _fadeOverlay.Color = Colors.Black;
        _fadeOverlay.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        _fadeOverlay.ZIndex = 1000; // High z-index to be on top
        AddChild(_fadeOverlay);
    }
    
    // NEW: Start fade in animation
    private void StartFadeIn()
    {
        if (_fadeOverlay == null) return;
        
        var tween = CreateTween();
        tween.TweenProperty(_fadeOverlay, "modulate:a", 0.0f, 1.0f);
        tween.TweenCallback(Callable.From(() => {
            _fadeOverlay.QueueFree();
            _fadeOverlay = null;
        }));
    }
    
    // NEW: Get media manager reference
    public MediaManager GetMediaManager()
    {
        return _mediaManager;
    }
    
    // Method to control CLI input processing
    public void SetCLIInputEnabled(bool enabled)
    {
        SetProcessInput(enabled);
        
        // Also control focus
        if (enabled)
        {
            CallDeferred(nameof(GrabLineEditFocus));
        }
        else if (_currentLineEdit != null)
        {
            _currentLineEdit.ReleaseFocus();
        }
    }
    
    // NEW: Method to show location media automatically
    public void ShowLocationMedia(string locationId, MediaManager.DisplayMode? mode = null)
    {
        if (_mediaManager != null)
        {
            _mediaManager.DisplayLocationMedia(locationId, mode);
        }
    }
    
    // NEW: Method to show ASCII image (backward compatibility)
    public void ShowLocationImage(string imagePath)
    {
        if (_mediaManager != null)
        {
            // For backward compatibility, try to find location by image path
            string currentLocationId = _gameState.GetCurrentLocation()?.Id ?? "unknown";
            _mediaManager.DisplayLocationMedia(currentLocationId, MediaManager.DisplayMode.ASCII);
        }
    }
    
    // NEW: Method to show 3D scene
    public void ShowLocationScene(string scenePath, Vector3? cameraPos = null, Vector3? cameraTarget = null)
    {
        if (_mediaManager != null)
        {
            // Use DisplayLocationMedia for 3D scenes as well
            string currentLocationId = _gameState.GetCurrentLocation()?.Id ?? "unknown";
            _mediaManager.DisplayLocationMedia(currentLocationId, MediaManager.DisplayMode.Scene3D);
        }
    }
    
    // NEW: Called when any media viewer is closed
    private void OnMediaViewerClosed()
    {
        // Add new command prompt when media closes
        AddNewCommandPrompt();
        
        // Return focus to the command line
        CallDeferred(nameof(GrabLineEditFocus));
    }
    
    // Called when scrolling completes
    private void OnScrollCompleted()
    {
        // Refocus the line edit to ensure keyboard input works
        if (_currentLineEdit != null && !_currentLineEdit.HasFocus())
        {
            _currentLineEdit.GrabFocus();
        }
    }
    
    // Timer timeout handler
    private void OnScrollTimerTimeout()
    {
        ForceScrollToBottom();
    }

    // Process input to catch the enter key press
    public override void _Input(InputEvent @event)
    {
        // Check if media manager has visible media that should handle input
        if (_mediaManager != null && _mediaManager.IsMediaVisible())
        {
            // Let media handle input, don't process it here
            return;
        }
        
        // Process CLI input normally
        base._Input(@event);
    }

    // NEW: Handle Tab completion
    private void HandleTabCompletion()
    {
        if (_commandProcessor == null || _currentLineEdit == null) return;
        
        string currentInput = _currentLineEdit.Text;
        if (string.IsNullOrWhiteSpace(currentInput)) return;
        
        var suggestions = _commandProcessor.GetAutoCompleteSuggestions(currentInput);
        
        if (suggestions.Count == 0)
        {
            // No suggestions available - could add a subtle sound or visual feedback
            return;
        }
        else if (suggestions.Count == 1)
        {
            // Auto-complete with the single suggestion
            string suggestion = suggestions[0];
            
            // Handle contextual completions (for multi-word commands)
            string[] currentWords = currentInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (currentWords.Length > 1)
            {
                // Replace the last word with the suggestion
                currentWords[currentWords.Length - 1] = suggestion;
                _currentLineEdit.Text = string.Join(" ", currentWords) + " ";
            }
            else
            {
                // Single word completion
                _currentLineEdit.Text = suggestion + " ";
            }
            
            _currentLineEdit.CaretColumn = _currentLineEdit.Text.Length;
        }
        else
        {
            // Multiple suggestions - find common prefix and show suggestions
            string commonPrefix = FindCommonPrefix(suggestions, currentInput);
            
            // If we can extend the current input with a common prefix
            if (commonPrefix.Length > currentInput.Length)
            {
                _currentLineEdit.Text = commonPrefix;
                _currentLineEdit.CaretColumn = _currentLineEdit.Text.Length;
            }
            
            // Show suggestions (limit to prevent spam)
            string suggestionText = "[TAB] Suggestions: " + string.Join(", ", suggestions.Take(8));
            if (suggestions.Count > 8)
            {
                suggestionText += $" ... (+{suggestions.Count - 8} more)";
            }
            
            DisplayOutput(suggestionText, new Color(0.7f, 0.7f, 0.9f)); // Light blue for suggestions
        }
    }
    
    // NEW: Find common prefix among suggestions
    private string FindCommonPrefix(List<string> suggestions, string currentInput)
    {
        if (suggestions.Count == 0) return currentInput;
        
        // For contextual suggestions, work with the last word
        string[] currentWords = currentInput.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        string lastWord = currentWords.Length > 0 ? currentWords[currentWords.Length - 1] : "";
        
        string prefix = suggestions[0];
        foreach (string suggestion in suggestions.Skip(1))
        {
            int i = 0;
            while (i < prefix.Length && i < suggestion.Length && 
                   char.ToLower(prefix[i]) == char.ToLower(suggestion[i]))
            {
                i++;
            }
            prefix = prefix.Substring(0, i);
        }
        
        // If we have a meaningful prefix that's longer than the last word
        if (prefix.Length > lastWord.Length && currentWords.Length > 1)
        {
            currentWords[currentWords.Length - 1] = prefix;
            return string.Join(" ", currentWords);
        }
        
        return prefix.Length > currentInput.Length ? prefix : currentInput;
    }

    // Special method for initial focus grab
    private void InitialFocusGrab() 
    {
        // First immediate attempt
        if (_currentLineEdit != null)
        {
            _currentLineEdit.GrabFocus();
        }
        
        // Create multiple focus timers at different intervals
        for (float delay = 0.1f; delay <= 0.5f; delay += 0.2f)
        {
            var focusTimer = new Timer();
            AddChild(focusTimer);
            focusTimer.WaitTime = delay;
            focusTimer.OneShot = true;
            focusTimer.Timeout += () => {
                if (_currentLineEdit != null)
                {
                    _currentLineEdit.ReleaseFocus();
                    _currentLineEdit.GrabFocus();
                }
                focusTimer.QueueFree();
            };
            focusTimer.Start();
        }
    }
    
    private void ApplyTerminalStyling()
    {
        // Terminal background
        var panelStyle = new StyleBoxFlat();
        panelStyle.BgColor = new Color(0.1f, 0.1f, 0.1f);
        panelStyle.ContentMarginLeft = 5;
        panelStyle.ContentMarginRight = 5;
        panelStyle.ContentMarginTop = 5;
        panelStyle.ContentMarginBottom = 5;
        
        // Make the whole control have dark background
        var controlStyle = new StyleBoxFlat();
        controlStyle.BgColor = new Color(0.1f, 0.1f, 0.1f);
        AddThemeStyleboxOverride("panel", controlStyle);
    }
    
    public override void _Process(double delta)
    {
        // Handle command history navigation
        if (Input.IsActionJustPressed("ui_up") && _commandHistory.Count > 0 && _currentLineEdit != null)
        {
            _historyIndex = Mathf.Clamp(_historyIndex + 1, 0, _commandHistory.Count - 1);
            _currentLineEdit.Text = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
            _currentLineEdit.CaretColumn = _currentLineEdit.Text.Length;
        }
        else if (Input.IsActionJustPressed("ui_down") && _historyIndex >= 0 && _currentLineEdit != null)
        {
            _historyIndex--;
            if (_historyIndex < 0)
            {
                _currentLineEdit.Text = "";
                _historyIndex = -1;
            }
            else
            {
                _currentLineEdit.Text = _commandHistory[_commandHistory.Count - 1 - _historyIndex];
                _currentLineEdit.CaretColumn = _currentLineEdit.Text.Length;
            }
        }
        
        // Update game state at regular intervals
        _timeSinceLastUpdate += (float)delta;
        if (_timeSinceLastUpdate >= UPDATE_INTERVAL)
        {
            _gameState.Update(_timeSinceLastUpdate);
            _timeSinceLastUpdate = 0;
            
            // Display any pending messages
            var pendingMessages = _gameState.GetAndClearPendingMessages();
            if (pendingMessages.Count > 0)
            {
                foreach (var message in pendingMessages)
                {
                    DisplayOutput(message);
                }
                
                // Add new command prompt after displaying pending messages
                AddNewCommandPrompt();
                
                // Trigger scroll to bottom after messages are added
                _contentAdded = true;
                _scrollTimer.Start();
            }
        }
        
        // Make sure input field stays focused (only when image viewer is not visible)
        if (_currentLineEdit != null && !_currentLineEdit.HasFocus() && 
            (_mediaManager == null || !_mediaManager.IsMediaVisible()))
        {
            _currentLineEdit.GrabFocus();
        }
        
        // Handle scrolling if content was added
        if (_contentAdded)
        {
            _contentAdded = false;
            ForceScrollToBottom();
        }
    }
    
    // NEW: Display minimap
    public void DisplayMinimap()
    {
        if (_improvedUI != null)
        {
            string minimap = _improvedUI.GenerateMinimap(_gameState);
            if (!string.IsNullOrEmpty(minimap))
            {
                DisplayOutput(minimap, new Color(0.8f, 0.8f, 1.0f));
            }
        }
    }
    
    // NEW: Show notification
    public void ShowNotification(string message, NotificationType type)
    {
        // Display temporary notification
        string prefix = type switch
        {
            NotificationType.Achievement => "🏆 ",
            NotificationType.Quest => "📜 ",
            NotificationType.Warning => "⚠️ ",
            NotificationType.Info => "ℹ️ ",
            _ => ""
        };
        
        Color? color = type switch
        {
            NotificationType.Achievement => Colors.Gold,
            NotificationType.Quest => Colors.LightBlue,
            NotificationType.Warning => Colors.Orange,
            NotificationType.Info => Colors.LightGray,
            _ => null
        };
        
        DisplayOutput($"\n{prefix}{message}\n", color);
    }
    
    // NEW: Toggle minimap display
    public void ToggleMinimap()
    {
        _improvedUI?.ToggleMinimap();
    }
    
    // NEW: Get UI information
    public ImprovedUI GetImprovedUI()
    {
        return _improvedUI;
    }
    
    // Method to add a new text line to the container
    private void AddLine(string text, Color? color = null)
    {
        // Create text line instance
        var lineScene = GD.Load<PackedScene>(_scenes["add_line"]);
        var lineInstance = lineScene.Instantiate<RichTextLabel>();
        
        // Try to set the text property on the instantiated node
        if (lineInstance != null)
            {
                lineInstance.Text = text;
                if (color.HasValue)
                {
                    lineInstance.Modulate = color.Value;
                }
            }
        
        // Apply color if provided
        if (color.HasValue)
        {
            lineInstance.Modulate = color.Value;
        }
        
        // Add to container
        _vContainer.AddChild(lineInstance);
        
        // Mark content as added for scrolling
        _contentAdded = true;
        _scrollTimer.Start();
    }
    
    // Method to add a new command prompt
    private void AddNewCommandPrompt()
    {
        // Create prompt instance
        var promptScene = GD.Load<PackedScene>(_scenes["prompt"]);
        var promptInstance = promptScene.Instantiate<Control>();
        
        // Unregister previous signal handler
        if (_currentLineEdit != null)
        {
            try
            {
                if (_currentLineEdit.IsConnected("text_submitted", new Callable(this, nameof(OnTextSubmitted))))
                {
                    _currentLineEdit.TextSubmitted -= OnTextSubmitted;
                }
            }
            catch (Exception e)
            {
                GD.PrintErr("Failed to disconnect signal: " + e.Message);
            }
        }
        
        // Configure LineEdit
        _currentLineEdit = promptInstance.GetNode<LineEdit>("LineEdit");
        if (_currentLineEdit != null)
        {
            _currentLineEdit.FocusMode = Control.FocusModeEnum.All;
            _currentLineEdit.TextSubmitted += OnTextSubmitted;
        }
        else
        {
            GD.PrintErr("Could not find LineEdit 'LineEdit' in prompt scene");
        }
        
        // Add to container
        _vContainer.AddChild(promptInstance);
        
        // Ensure focus is grabbed and scrolling occurs
        CallDeferred(nameof(GrabLineEditFocus));
        _contentAdded = true;
        _scrollTimer.Start();
    }
    
    // Method to ensure focus is properly grabbed
    private void GrabLineEditFocus()
    {
        if (_currentLineEdit != null)
        {
            // First deselect any active focus
            if (_currentLineEdit.HasFocus())
            {
                _currentLineEdit.ReleaseFocus();
            }
            
            // Then grab focus
            _currentLineEdit.FocusMode = Control.FocusModeEnum.All;
            _currentLineEdit.GrabFocus();
            
            // Try to force focus through viewport
            var viewport = GetViewport();
            if (viewport != null)
            {
                viewport.GuiReleaseFocus();
            }
        }
    }
    
    private void OnTextSubmitted(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) 
        {
            return;
        }
        
        // Add command to history
        _commandHistory.Add(text);
        _historyIndex = -1;
        
        // Show the command with prompt
        DisplayCommand(text);
        
        // Handle the command using CommandProcessor
        string response = HandleCommand(text);
        
        // Check for special responses that shouldn't add a new prompt
        if (response == "[NO_PROMPT]")
        {
            // Don't add new command prompt - wait for media to close
            return;
        }
        
        // Add new command prompt for normal responses
        AddNewCommandPrompt();
        
        // Reset manual scrolling state in both controllers
        if (_scrollController != null)
        {
            _scrollController.ResetManualScrolling();
        }
        
        if (_updateScroll != null)
        {
            _updateScroll.ResetManualScrollOnCommand();
        }
        else
        {
            ForceScrollToBottom();
        }
    }
    
    // Method to force scroll to bottom (more reliable)
    private void ForceScrollToBottom()
    {
        // If we have a direct scroll controller, use it first
        if (_scrollController != null)
        {
            _scrollController.ForceScrollToBottom();
            return;
        }
        
        // Fallback: try direct manipulation of ScrollContainer
        if (_scrollContainer != null)
        {
            var vScrollBar = _scrollContainer.GetVScrollBar();
            if (vScrollBar != null)
            {
                _scrollContainer.ScrollVertical = (int)vScrollBar.MaxValue;
                GD.Print("AdvancedCLIEmulator: Manual scroll to bottom");
            }
        }
    }
    
    private string HandleCommand(string text)
    {
        // Process the command using the CommandProcessor
        string response = _commandProcessor.ProcessCommand(text);
        
        // Handle special responses
        if (response == "[CLEAR]")
        {
            ClearOutput();
            return "[CLEAR]"; // Return the special code
        }
        
        if (response == "[NO_PROMPT]")
        {
            return "[NO_PROMPT]"; // Return the special code
        }
        
        // Display response only if it's not empty
        if (!string.IsNullOrEmpty(response))
        {
            DisplayOutput(response);
        }
        
        return response;
    }

    private void ClearOutput()
    {
        // Remove all children from the container
        foreach (var child in _vContainer.GetChildren())
        {
            child.QueueFree();
        }
    }
    
    private void DisplayStartupScreen()
    {
        // Add boot-up sequence for terminal feel
        if (_useClassicTerminalStyle)
        {
            DisplayBootSequence();
        }
        else
        {
            AddLine(_introText, _defaultTextColor);
        }
    }
    
    private void DisplayBootSequence()
    {
        // ASCII Art Title (with old-school terminal styling)
        string asciiTitle = @"[color=#D00000]`[/color][color=#D00000]7[/color][color=#D00000]M[/color][color=#CF0000]N[/color][color=#CF0000].[/color]   [color=#CE0000]`[/color][color=#CE0000]7[/color][color=#CD0000]M[/color][color=#CD0000]F[/color][color=#CD0000]'[/color] [color=#CC0000].[/color][color=#CC0000]g[/color][color=#CB0000]8[/color][color=#CB0000]""[/color][color=#CB0000]""[/color][color=#CB0000]8[/color][color=#CA0000]q[/color][color=#CA0000].[/color][color=#CA0000]`[/color][color=#C90000]7[/color][color=#C90000]M[/color][color=#C90000]M[/color][color=#C80000]F[/color][color=#C80000]'[/color]     [color=#C60000]A[/color]     [color=#C50000]`[/color][color=#C40000]7[/color][color=#C40000]M[/color][color=#C40000]F[/color][color=#C30000]'[/color] [color=#C30000]d[/color][color=#C30000]b[/color]      [color=#C00000]`[/color][color=#C00000]7[/color][color=#C00000]M[/color][color=#C00000]M[/color][color=#BF0000]""[/color][color=#BF0000]""[/color][color=#BF0000]""[/color][color=#BE0000]M[/color][color=#BE0000]q[/color][color=#BE0000].[/color]  [color=#BD0000]`[/color][color=#BD0000]7[/color][color=#BC0000]M[/color][color=#BC0000]M[/color][color=#BC0000]""[/color][color=#BB0000]""[/color][color=#BB0000]""[/color][color=#BB0000]Y[/color][color=#BB0000]M[/color][color=#BA0000]M[/color]  [color=#B90000]
[/color]  [color=#B80000]M[/color][color=#B80000]M[/color][color=#B80000]N[/color][color=#B80000].[/color]    [color=#B60000]M[/color] [color=#B50000].[/color][color=#B50000]d[/color][color=#B50000]P[/color][color=#B50000]'[/color]    [color=#B30000]`[/color][color=#B30000]Y[/color][color=#B30000]M[/color][color=#B20000].[/color][color=#B20000]`[/color][color=#B20000]M[/color][color=#B10000]A[/color]     [color=#B00000],[/color][color=#AF0000]M[/color][color=#AF0000]A[/color]     [color=#AD0000],[/color][color=#AD0000]V[/color]  [color=#AC0000];[/color][color=#AC0000]M[/color][color=#AB0000]M[/color][color=#AB0000]:[/color]       [color=#A90000]M[/color][color=#A80000]M[/color]   [color=#A70000]`[/color][color=#A70000]M[/color][color=#A70000]M[/color][color=#A60000].[/color]   [color=#A50000]M[/color][color=#A50000]M[/color]    [color=#A30000]`[/color][color=#A30000]7[/color]  [color=#A20000]
[/color]  [color=#A10000]M[/color] [color=#A10000]Y[/color][color=#A00000]M[/color][color=#A00000]b[/color]   [color=#9F0000]M[/color] [color=#9E0000]d[/color][color=#9E0000]M[/color][color=#9E0000]'[/color]      [color=#9C0000]`[/color][color=#9B0000]M[/color][color=#9B0000]M[/color] [color=#9A0000]V[/color][color=#9A0000]M[/color][color=#9A0000]:[/color]   [color=#990000],[/color][color=#980000]V[/color][color=#980000]V[/color][color=#980000]M[/color][color=#970000]:[/color]   [color=#960000],[/color][color=#960000]V[/color]  [color=#950000],[/color][color=#950000]V[/color][color=#950000]^[/color][color=#940000]M[/color][color=#940000]M[/color][color=#940000].[/color]      [color=#920000]M[/color][color=#910000]M[/color]   [color=#900000],[/color][color=#900000]M[/color][color=#8F0000]9[/color]    [color=#8E0000]M[/color][color=#8E0000]M[/color]   [color=#8D0000]d[/color]    [color=#8B0000]
[/color]  [color=#8A0000]M[/color]  [color=#890000]`[/color][color=#890000]M[/color][color=#890000]N[/color][color=#880000].[/color] [color=#880000]M[/color] [color=#870000]M[/color][color=#870000]M[/color]        [color=#840000]M[/color][color=#840000]M[/color]  [color=#830000]M[/color][color=#830000]M[/color][color=#820000].[/color]  [color=#820000]M[/color][color=#810000]'[/color] [color=#810000]M[/color][color=#800000]M[/color][color=#800000].[/color]  [color=#7F0000]M[/color][color=#7F0000]'[/color] [color=#7E0000],[/color][color=#7E0000]M[/color]  [color=#7D0000]`[/color][color=#7D0000]M[/color][color=#7C0000]M[/color]      [color=#7A0000]M[/color][color=#7A0000]M[/color][color=#7A0000]m[/color][color=#7A0000]m[/color][color=#790000]d[/color][color=#790000]M[/color][color=#790000]9[/color]     [color=#770000]M[/color][color=#770000]M[/color][color=#760000]m[/color][color=#760000]m[/color][color=#760000]M[/color][color=#750000]M[/color]    [color=#740000]
[/color]  [color=#730000]M[/color]   [color=#720000]`[/color][color=#710000]M[/color][color=#710000]M[/color][color=#710000].[/color][color=#710000]M[/color] [color=#700000]M[/color][color=#700000]M[/color][color=#6F0000].[/color]      [color=#6D0000],[/color][color=#6D0000]M[/color][color=#6D0000]P[/color]  [color=#6C0000]`[/color][color=#6C0000]M[/color][color=#6B0000]M[/color] [color=#6B0000]A[/color][color=#6A0000]'[/color]  [color=#690000]`[/color][color=#690000]M[/color][color=#690000]M[/color] [color=#680000]A[/color][color=#680000]'[/color]  [color=#670000]A[/color][color=#670000]b[/color][color=#670000]m[/color][color=#660000]m[/color][color=#660000]m[/color][color=#660000]q[/color][color=#650000]M[/color][color=#650000]A[/color]     [color=#630000]M[/color][color=#630000]M[/color]  [color=#620000]Y[/color][color=#620000]M[/color][color=#610000].[/color]     [color=#600000]M[/color][color=#5F0000]M[/color]   [color=#5E0000]Y[/color]  [color=#5D0000],[/color] [color=#5D0000]
[/color]  [color=#5C0000]M[/color]     [color=#5A0000]Y[/color][color=#5A0000]M[/color][color=#590000]M[/color] [color=#590000]`[/color][color=#590000]M[/color][color=#580000]b[/color][color=#580000].[/color]    [color=#560000],[/color][color=#560000]d[/color][color=#560000]P[/color][color=#560000]'[/color]   [color=#540000]:[/color][color=#540000]M[/color][color=#540000]M[/color][color=#540000];[/color]    [color=#520000]:[/color][color=#520000]M[/color][color=#510000]M[/color][color=#510000];[/color]  [color=#500000]A[/color][color=#500000]'[/color]     [color=#4E0000]V[/color][color=#4E0000]M[/color][color=#4E0000]L[/color]    [color=#4C0000]M[/color][color=#4C0000]M[/color]   [color=#4B0000]`[/color][color=#4A0000]M[/color][color=#4A0000]b[/color][color=#4A0000].[/color]   [color=#490000]M[/color][color=#480000]M[/color]     [color=#460000],[/color][color=#460000]M[/color] [color=#460000]
[/color][color=#450000].[/color][color=#450000]J[/color][color=#450000]M[/color][color=#440000]L[/color][color=#440000].[/color]    [color=#430000]Y[/color][color=#420000]M[/color]   [color=#410000]`[/color][color=#410000]""[/color][color=#410000]b[/color][color=#400000]m[/color][color=#400000]m[/color][color=#400000]d[/color][color=#3F0000]""[/color][color=#3F0000]'[/color]      [color=#3D0000]V[/color][color=#3D0000]F[/color]      [color=#3B0000]V[/color][color=#3A0000]F[/color] [color=#3A0000].[/color][color=#390000]A[/color][color=#390000]M[/color][color=#390000]A[/color][color=#380000].[/color]   [color=#370000].[/color][color=#370000]A[/color][color=#370000]M[/color][color=#360000]M[/color][color=#360000]A[/color][color=#360000].[/color][color=#360000].[/color][color=#350000]J[/color][color=#350000]M[/color][color=#350000]M[/color][color=#340000]L[/color][color=#340000].[/color] [color=#330000].[/color][color=#330000]J[/color][color=#330000]M[/color][color=#330000]M[/color][color=#320000].[/color][color=#320000].[/color][color=#320000]J[/color][color=#310000]M[/color][color=#310000]M[/color][color=#310000]m[/color][color=#300000]m[/color][color=#300000]m[/color][color=#300000]m[/color][color=#300000]M[/color][color=#2F0000]M[/color][color=#2F0000]M[/color] 

NoWareOS [Version 88614]  
(c) 20dX NoWare™. All rights reserved.";
        AddLine(asciiTitle, Colors.White);
        AddLine("\n", Colors.White);
        AddLine(_introText, _defaultTextColor);
    }
    
    private void DisplayCommand(string text)
    {
        // Display prompt and command as a single line
        AddLine($"{_prompt}{text}", _inputTextColor);
    }
    
    public void DisplayOutput(string text, Color? color = null)
    {
        Color textColor = color ?? _defaultTextColor;
        AddLine(text, textColor);
    }
    
    public void DisplayError(string text)
    {
        DisplayOutput(text, _errorTextColor);
    }
    
    // NEW: Display error with custom color
    public void DisplayError(string text, Color color)
    {
        DisplayOutput(text, color);
    }
    
    // NEW: Add a method to display system messages (e.g., warnings, info)
    public void DisplaySystemMessage(string text)
    {
        DisplayOutput(text, _systemTextColor);
    }
    
    // NEW: Update last line (for loading bars, etc.)
    public void UpdateLastLine(string newText)
    {
        if (_vContainer == null) return;
        
        var children = _vContainer.GetChildren();
        if (children.Count > 0)
        {
            // Get the last child that's a RichTextLabel (not the LineEdit prompt)
            for (int i = children.Count - 1; i >= 0; i--)
            {
                if (children[i] is RichTextLabel lastLabel)
                {
                    lastLabel.Text = newText;
                    break;
                }
            }
        }
        
        // Scroll to bottom
        CallDeferred(nameof(ForceScrollToBottom));
    }
    
    // Additional methods for game functionality...
    
    // Add keyboard shortcuts
    public override void _UnhandledKeyInput(InputEvent @event)
    {
        // Don't process if image viewer is visible
        if (_mediaManager != null && _mediaManager.IsMediaVisible())
            return;
            
        if (@event is InputEventKey keyEvent && keyEvent.Pressed)
        {
            // Ctrl+L to clear screen (like in real terminals)
            if (keyEvent.Keycode == Key.L && keyEvent.CtrlPressed)
            {
                ClearOutput();
                AddNewCommandPrompt();
                GetViewport().SetInputAsHandled();
            }
        }
    }
    
    // NEW: Set VHS filter visibility
    public void SetVHSFilter(bool enabled)
    {
        if (_vhsFilter != null)
        {
            _vhsFilter.SetDeferred("visible", enabled);
            GD.Print($"AdvancedCLIEmulator: VHS filter {(enabled ? "enabled" : "disabled")}");
        }
        else
        {
            GD.PrintErr("AdvancedCLIEmulator: VHS filter node not available");
        }
    }
    
    // NEW: Check if VHS filter is enabled
    public bool IsVHSFilterEnabled()
    {
        if (_vhsFilter != null)
        {
            return _vhsFilter.Get("visible").AsBool();
        }
        return false;
    }
}

// NEW: Add NotificationType enum
public enum NotificationType
{
    Achievement,
    Quest,
    Warning,
    Info,
    Error,
    System
}