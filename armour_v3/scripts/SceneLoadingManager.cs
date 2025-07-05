using Godot;
using System;

public partial class SceneLoadingManager : Node
{
    public static SceneLoadingManager Instance { get; private set; }

    private string _scenePath;
    private Action<float> _onProgress;
    private Action<PackedScene> _onComplete;
    private float _progress = 0f;
    private bool _isLoading = false;

    public override void _Ready()
    {
        if (Instance == null)
        {
            Instance = this;
            GD.Print("SceneLoadingManager: Instance initialized");
        }
        else
        {
            GD.PrintErr("SceneLoadingManager: Multiple instances detected!");
        }
    }

    public void LoadSceneAsync(string scenePath, Action<float> onProgress, Action<PackedScene> onComplete)
    {
        if (_isLoading)
        {
            GD.PrintErr("SceneLoadingManager: Already loading a scene.");
            return;
        }

        GD.Print($"SceneLoadingManager: Starting to load scene: {scenePath}");
        
        _scenePath = scenePath;
        _onProgress = onProgress;
        _onComplete = onComplete;
        _progress = 0f;
        _isLoading = true;

        // Use Godot 4's threaded loading
        var error = ResourceLoader.LoadThreadedRequest(scenePath);
        if (error != Error.Ok)
        {
            GD.PrintErr($"SceneLoadingManager: Failed to start threaded loading: {error}");
            _isLoading = false;
            onProgress?.Invoke(1f);
            onComplete?.Invoke(null);
            return;
        }

        GD.Print("SceneLoadingManager: Threaded loading started successfully");
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (!_isLoading)
        {
            SetProcess(false);
            return;
        }

        var progressArray = new Godot.Collections.Array();
        var status = ResourceLoader.LoadThreadedGetStatus(_scenePath, progressArray);
        
        if (progressArray.Count > 0)
        {
            _progress = (float)progressArray[0];
            _onProgress?.Invoke(_progress);
        }

        if (status == ResourceLoader.ThreadLoadStatus.Loaded)
        {
            GD.Print("SceneLoadingManager: Scene loaded successfully");
            var scene = ResourceLoader.LoadThreadedGet(_scenePath) as PackedScene;
            _isLoading = false;
            SetProcess(false);
            _onProgress?.Invoke(1f);
            _onComplete?.Invoke(scene);
        }
        else if (status == ResourceLoader.ThreadLoadStatus.Failed)
        {
            GD.PrintErr($"SceneLoadingManager: Error loading scene: {_scenePath}");
            _isLoading = false;
            SetProcess(false);
            _onProgress?.Invoke(1f);
            _onComplete?.Invoke(null);
        }
    }
}
