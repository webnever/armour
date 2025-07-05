using Godot;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface ISaveable
{
    string Id { get; }
    Godot.Collections.Dictionary GetState();
    void SetState(Godot.Collections.Dictionary state);
}

public partial class SaveGameData : Resource
{
    [Export]
    public string Timestamp { get; set; }
    [Export]
    public Vector3 PlayerPosition { get; set; }
    [Export]
    public Vector3 PlayerRotation { get; set; }
    [Export]
    public string CurrentLevel { get; set; }
    [Export]
    public Godot.Collections.Dictionary InventoryState { get; set; }
    [Export]
    public Godot.Collections.Dictionary LevelStates { get; set; }
    [Export]
    public Image Screenshot { get; set; }
}

public partial class GameSceneManager : Node
{
    [Signal]
    public delegate void SceneAndFadeInFinishedEventHandler();
    [Signal]
    public delegate void LoadFinishedEventHandler();
    [Signal]
    public delegate void SaveCompletedEventHandler();

    private bool _fadeInFinished = false;
    private bool _sceneLoaded = false;
    private bool _isSwitchingScene = false;
    public bool InMainMenu { get; private set; } = true;
    public bool InGameMenu { get; private set; } = false;

    // Core references
    private PlayerController _playerController;
    private Control _control;
    private MenuManager _menuManager;
    private SubViewport _levelContainer;
    private Control _uiElements;
    private Crossfader _crossfader;
    private Node _currentLevel;
    private Node _inventory;
    private TextureRect _diceLoader;
    private ShaderMaterial _loaderMaterial;
    private TerminalConsole _debugConsole;
    private DialogueBox dialogueBox;

    // State management
    private Dictionary<string, Dictionary<string, ISaveable>> _registeredSaveables = new();
    private const string SAVES_DIRECTORY = "user://saves/";
    private Tween _diceFadeTween;
    private const float FADE_DURATION = 0.5f;  // For dice loader

    private AnimationPlayer _startScreenAnimPlayer;
    private StartScreenButtonManager _startScreenButtonManager;
    private AnimationPlayer _blackholeAnimation;
    public string FirstScenePath { get; set; } = "res://game_scenes/prototype_level_1_scene.tscn";

    public override void _Ready()
    {
        InitializeNodeReferences();
        SetupDirectoriesAndSignals();
        InitializeUI();
    }

    private void InitializeNodeReferences()
    {
        _playerController = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
        _control = GetNode<Control>("/root/mainScene/Control");
        _menuManager = GetNode<MenuManager>("/root/mainScene/menuManager");
        _levelContainer = GetNode<SubViewport>("/root/mainScene/Control/SubViewport");
        _uiElements = GetNode<Control>("/root/mainScene/Control/uiElements");
        _crossfader = GetNode<Crossfader>("/root/mainScene/crossfader");
        _inventory = GetNode("/root/mainScene/Inventory");
        _diceLoader = GetNode<TextureRect>("/root/mainScene/diceLoader");
        _debugConsole = GetNode<TerminalConsole>("/root/mainScene/Control/uiElements/TerminalConsole");
        dialogueBox = GetNode<DialogueBox>("/root/mainScene/DialogueBox");

        // Start screen references
        _startScreenAnimPlayer = GetNode<AnimationPlayer>("/root/mainScene/Control/start_screen/AnimationPlayer");
        _startScreenButtonManager = GetNode<StartScreenButtonManager>("/root/mainScene/Control/start_screen/Control/menuButtonManager");
        _blackholeAnimation = GetNode<AnimationPlayer>("/root/mainScene/Control/start_screen/Control/blackhole2/AnimationPlayer");
        
        // Separate handlers for each animation player
        _startScreenAnimPlayer.AnimationFinished += OnSplashScreenAnimationFinished;
        _blackholeAnimation.AnimationFinished += OnBlackholeAnimationFinished;
        
        // Start the splash screen animation
        _startScreenAnimPlayer.Play("fade_in");
    }

    private void OnSplashScreenAnimationFinished(StringName animationName)
    {
        if (animationName == "fade_in")  // Make sure this matches the actual animation name
        {
            _startScreenButtonManager.CanScroll = true;
            _startScreenButtonManager.EnableButtonInteraction();
        }
    }

    private void OnBlackholeAnimationFinished(StringName animationName)
    {
        if (animationName == "blackHoleExpand")
        {
            SwitchScene(FirstScenePath);
        }
    }

    private void SetupDirectoriesAndSignals()
    {
        DirAccess.MakeDirAbsolute(SAVES_DIRECTORY);
        if (_crossfader != null)
        {
            _crossfader.GetNode<AnimationPlayer>("AnimationPlayer").AnimationFinished += _OnFadeInFinished;
        }
    }

    private void InitializeUI()
    {
        if (_diceLoader != null)
        {
            _loaderMaterial = (ShaderMaterial)_diceLoader.Material;
            _loaderMaterial.SetShaderParameter("alpha", 0.0f);
        }

        dialogueBox.Visible = false;
    }

    // Scene Management Methods
    public async void SwitchScene(string sceneToSwitchTo, bool saveCurrentState = false)
    {
        if (_isSwitchingScene) return;

        try
        {
            await ExecuteSceneSwitch(sceneToSwitchTo, saveCurrentState);
        }
        finally
        {
            CompleteSceneSwitch();
        }
    }

    private async Task ExecuteSceneSwitch(string sceneToSwitchTo, bool saveCurrentState)
    {
        PrepareForSceneSwitch();

        // Start crossfader fade in
        if (_crossfader != null)
        {
            _crossfader.FadeIn();
            await ToSignal(_crossfader.GetNode<AnimationPlayer>("AnimationPlayer"), "animation_finished");
        }

        // Now show and fade in the dice
        await ShowLoadingTransition();
        ShowGameUI(true);
        
        if (saveCurrentState)
        {
            SaveCurrentLevelState();
        }

        // Clean up and load new scene
        CleanupOldScene();
        await LoadNewScene(sceneToSwitchTo, saveCurrentState);

        // Fade out dice first
        await HideLoadingTransition();

        // Finally fade out crossfader
        if (_crossfader != null)
        {
            _crossfader.FadeOut();
            await ToSignal(_crossfader.GetNode<AnimationPlayer>("AnimationPlayer"), "animation_finished");
        }
    }

    // Save/Load System Methods
    public void RegisterSaveable(ISaveable saveable, string sceneName)
    {
        if (!_registeredSaveables.ContainsKey(sceneName))
        {
            _registeredSaveables[sceneName] = new Dictionary<string, ISaveable>();
        }
        _registeredSaveables[sceneName][saveable.Id] = saveable;
        DebugLog($"Registered saveable: {saveable.Id}");
    }

    public async void SaveGame()
    {
        DebugLog("Starting save game process", "warning");
        await ShowLoadingTransition();

        var saveData = new SaveGameData
        {
            Timestamp = DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss"),
            PlayerPosition = _playerController.GlobalPosition,
            PlayerRotation = _playerController.Rotation,
            CurrentLevel = _levelContainer.GetChild(0).Name,
            InventoryState = GetInventoryState(),
            LevelStates = CollectLevelStates(),
            Screenshot = CaptureScreenshot()
        };

        SaveGameToDisk(saveData);
        
        await HideLoadingTransition();
        EmitSignal(nameof(SaveCompleted));
    }

    public async void LoadGame(string filename)
    {
        DebugLog($"Starting load game process: {filename}", "warning");
        await ShowLoadingTransition();

        var saveData = LoadSaveFile(filename);
        if (saveData == null)
        {
            DebugLog($"Failed to load save file: {filename}", "error");
            await HideLoadingTransition();
            return;
        }

        await LoadGameState(saveData);
        await HideLoadingTransition();
    }

    // UI Management Methods
    public async void ToggleGameMenu()
    {
        // Use the proper AnimFinished property
        if (!_menuManager.AnimFinished) return;

        if (!InGameMenu)
        {
            OpenGameMenu();
        }
        else
        {
            await CloseGameMenu();
        }
    }

    private void PrepareForSceneSwitch()
    {
        _isSwitchingScene = true;
        _playerController.Set("inputEnabled", false);
        _fadeInFinished = false;
        _sceneLoaded = false;
    }

    private void SaveCurrentLevelState()
    {
        DebugLog("Saving current level state");
        GetTree().CallGroup("Saveable", "OnSaveGame");
    }

    private void CleanupOldScene()
    {
        if (_levelContainer.GetChildCount() > 0)
        {
            _levelContainer.GetChild(0).QueueFree();
        }
        else if (GetTree().Root.HasNode("mainScene/Control/start_screen"))
        {
            GetTree().Root.GetNode("mainScene/Control/start_screen").QueueFree();
            InMainMenu = false;
        }
    }

    private async Task LoadNewScene(string sceneName, bool loadSavedState)
    {
        DebugLog($"Loading scene: {sceneName}");
        try 
        {
            var newPackedScene = ResourceLoader.Load<PackedScene>(sceneName);
            if (newPackedScene == null)
            {
                DebugLog("Failed to load scene!", "error");
                return;
            }

            var newSceneInstance = newPackedScene.Instantiate();
            await ToSignal(GetTree(), "process_frame");
            
            _levelContainer.AddChild(newSceneInstance, true);
            _currentLevel = newSceneInstance;
            
            if (loadSavedState)
            {
                await Task.Run(() => LoadLevelState());
            }

            await ToSignal(GetTree(), "process_frame");
            UpdatePlayerPosition(newSceneInstance);
        }
        catch (Exception e)
        {
            DebugLog($"Error loading scene: {e.Message}", "error");
        }
    }

    private void LoadLevelState()
    {
        var states = GetLevelStates();
        if (states != null && states.Count > 0)
        {
            LoadLevelStates(states);
            DebugLog("Loaded level states");
        }
    }

    public Godot.Collections.Dictionary GetLevelStates()
    {
        DebugLog("Getting current level states");
        return CollectLevelStates();
    }


    private void UpdatePlayerPosition(Node newScene)
    {
        if (newScene.GetNode("spawnPoint") is Node3D spawnPoint)
        {
            _playerController.GlobalPosition = spawnPoint.GlobalPosition;
            _playerController.Rotation = spawnPoint.Rotation;
        }
    }

    private void CompleteSceneSwitch()
    {
        _isSwitchingScene = false;
        _playerController.Set("inputEnabled", true);
        ShowGameUI(true);
        dialogueBox.Visible = true;
        EmitSignal(SignalName.LoadFinished);
    }

    private void ShowGameUI(bool visible)
    {
        if (_uiElements != null)
        {
            _uiElements.Visible = visible;
        }
    }

    private async Task ShowLoadingTransition()
    {
        if (_diceLoader == null)
        {
            return;
        }
        if (_diceFadeTween != null && _diceFadeTween.IsValid())
        {
            _diceFadeTween.Kill();
        }
        
        _diceLoader.Visible = true; // Ensure it's visible before tweening
        _diceFadeTween = CreateTween();
        _diceFadeTween.TweenMethod(
            Callable.From((float value) => _loaderMaterial.SetShaderParameter("alpha", value)),
            0.0f,
            1.0f,
            FADE_DURATION
        );
        await ToSignal(_diceFadeTween, "finished");
    }

    private async Task HideLoadingTransition()
    {
        if (_diceLoader == null)
        {
            return;
        }
        if (_diceFadeTween != null && _diceFadeTween.IsValid())
        {
            _diceFadeTween.Kill();
        }
        
        _diceFadeTween = CreateTween();
        _diceFadeTween.TweenMethod(
            Callable.From((float value) => _loaderMaterial.SetShaderParameter("alpha", value)),
            1.0f,
            0.0f,
            FADE_DURATION
        );
        await ToSignal(_diceFadeTween, "finished");
        _diceLoader.Visible = false; // Hide it completely after fading
    }

    private void _OnFadeInFinished(StringName animationName)
    {
        if (animationName == "fadeIn")
        {
            _fadeInFinished = true;
            CheckConditionsAndEmit();
        }
    }

    private void _OnSceneLoaded()
    {
        _sceneLoaded = true;
        CheckConditionsAndEmit();
    }

    private void CheckConditionsAndEmit()
    {
        if (_fadeInFinished && _sceneLoaded)
        {
            EmitSignal(SignalName.SceneAndFadeInFinished);
        }
    }

    private void OpenGameMenu()
    {
        InGameMenu = true;
        _menuManager.RevealUIElement();
        _playerController.DisableInput();
        DebugLog("Game menu opened");
    }

    private async Task CloseGameMenu()
    {
        _menuManager.HideUIElement();
        await ToSignal(GetTree().CreateTimer(0.5), "timeout");
        InGameMenu = false;
        _playerController.EnableInput();
        DebugLog("Game menu closed");
    }

    private Godot.Collections.Dictionary GetInventoryState()
    {
        DebugLog("Getting inventory state");
        return (Godot.Collections.Dictionary)_inventory.Call("serialize");
    }

    private Godot.Collections.Dictionary CollectLevelStates()
    {
        var states = new Godot.Collections.Dictionary();
        
        foreach (var sceneEntry in _registeredSaveables)
        {
            var sceneStates = new Godot.Collections.Dictionary();
            foreach (var saveable in sceneEntry.Value.Values)
            {
                sceneStates[saveable.Id] = saveable.GetState();
            }
            states[sceneEntry.Key] = sceneStates;
        }
        
        DebugLog("Collected level states");
        return states;
    }

    private Image CaptureScreenshot()
    {
        var screenshot = _levelContainer.GetTexture().GetImage();
        screenshot.SavePng($"{SAVES_DIRECTORY}{DateTime.Now:yyyy-MM-dd-HH-mm-ss}.png");
        return screenshot;
    }

    private void SaveGameToDisk(SaveGameData saveData)
    {
        var path = $"{SAVES_DIRECTORY}{saveData.Timestamp}.tres";
        var error = ResourceSaver.Save(saveData, path);
        if (error != Error.Ok)
        {
            DebugLog($"Failed to save game: {error}", "error");
            return;
        }
        DebugLog($"Game saved to: {path}", "success");
    }

    private SaveGameData LoadSaveFile(string filename)
    {
        try
        {
            DebugLog($"Loading save file: {filename}");
            return (SaveGameData)ResourceLoader.Load(filename);
        }
        catch (Exception e)
        {
            DebugLog($"Failed to load save file: {e.Message}", "error");
            return null;
        }
    }

    private async Task LoadGameState(SaveGameData saveData)
    {
        DebugLog("Loading game state");
        _inventory.Call("deserialize", saveData.InventoryState);

        if (_currentLevel?.Name != saveData.CurrentLevel)
        {
            await SwitchSceneAndLoadState(saveData);
        }
        else
        {
            await LoadStateInCurrentScene(saveData);
        }
    }

    private async Task SwitchSceneAndLoadState(SaveGameData saveData)
    {
        await ShowLoadingTransition();
        
        SwitchScene(saveData.CurrentLevel, false);
        await ToSignal(this, "SceneAndFadeInFinished");
        
        UpdatePlayerState(saveData);
        LoadLevelStates(saveData.LevelStates);
        
        await HideLoadingTransition();
    }

    private async Task LoadStateInCurrentScene(SaveGameData saveData)
    {
        await ShowLoadingTransition();
        
        _crossfader.Call("fadeIn");
        await ToSignal(_crossfader.GetNode<AnimationPlayer>("AnimationPlayer"), "animation_finished");
        
        UpdatePlayerState(saveData);
        LoadLevelStates(saveData.LevelStates);
        _crossfader.Call("fadeOut");
        
        await HideLoadingTransition();
    }

    private void UpdatePlayerState(SaveGameData saveData)
    {
        _playerController.GlobalPosition = saveData.PlayerPosition;
        _playerController.Rotation = saveData.PlayerRotation;
    }

    public void LoadLevelStates(Godot.Collections.Dictionary levelStates)
    {
        if (levelStates == null)
        {
            DebugLog("Attempted to load null level states", "error");
            return;
        }

        foreach (var sceneEntry in levelStates)
        {
            var sceneName = (string)sceneEntry.Key;
            var sceneStates = (Godot.Collections.Dictionary)sceneEntry.Value;
            
            foreach (var objectEntry in sceneStates)
            {
                var objectId = (string)objectEntry.Key;
                if (_registeredSaveables.TryGetValue(sceneName, out var sceneObjects) &&
                    sceneObjects.TryGetValue(objectId, out var saveable))
                {
                    saveable.SetState((Godot.Collections.Dictionary)objectEntry.Value);
                    DebugLog($"Loaded state for object: {objectId}");
                }
            }
        }
    }

    public void StartNewGame()
    {
        _blackholeAnimation.Play("blackHoleExpand");
    }

    private void OnStartScreenAnimationFinished(StringName animationName)
    {
        if (animationName == "fadeIn")
        {
            _startScreenButtonManager.CanScroll = true;
            _startScreenButtonManager.EnableButtonInteraction(); // Enable interaction after fade
        }
        else if (animationName == "blackHoleExpand")
        {
            SwitchScene(FirstScenePath);
        }
    }

    private void DebugLog(string message, string type = "normal")
    {
        if (_debugConsole != null)
        {
            _debugConsole.PrintMessage($"{message}", type);
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (Input.IsActionJustReleased("enter") && !InMainMenu)
        {
            ToggleGameMenu();
        }
    }
}