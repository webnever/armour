using Godot;

public partial class VisionOfLuh : Node
{
    [Export] public Node3D KingScene { get; set; }
    [Export] public Node3D LuhScene { get; set; }
    [Export] public Control LightningScene { get; set; }
   
    [Export] public Camera3D KingCamera { get; set; }
    [Export] public Camera3D LuhCamera { get; set; }
   
    [Export] public float KingSceneDuration { get; set; } = 3.0f;
    [Export] public float LuhSceneDuration { get; set; } = 1.0f;
    [Export] public float LightningSceneDuration { get; set; } = 0.5f;
    
    [Export] public int StartingScene { get; set; } = 0; // 0 = King, 1 = Lightning, 2 = Luh, 3 = Lightning
   
    // Audio resources - preloaded
    [Export] public AudioStream RingingAudio { get; set; }
    [Export] public AudioStream ThunderAudio { get; set; }
    [Export] public AudioStream WaveAudio { get; set; }
    
    private int _currentSceneIndex;
    private Timer _timer;
    private AudioStreamPlayer _audioPlayer;
    private AudioStreamPlayer _wavePlayer; // Dedicated player for wave audio
    private RandomNumberGenerator _rng;
    
    private readonly float[] _sceneDurations = new float[4];
    private readonly string[] _sceneNames = { "King", "Lightning", "Luh", "Lightning" };
    private readonly int[] _sceneMapping = { 0, 2, 1, 2 }; // Maps sequence index to actual scene (0=King, 1=Luh, 2=Lightning)
   
    public override void _Ready()
    {
        // Get nodes by path if not assigned in inspector
        KingScene ??= GetNode<Node3D>("king");
        LuhScene ??= GetNode<Node3D>("luh");
        LightningScene ??= GetNode<Control>("lightning");
        
        KingCamera ??= GetNode<Camera3D>("king/Camera3D");
        LuhCamera ??= GetNode<Camera3D>("luh/Camera3D");
       
        // Preload audio resources if not assigned in inspector
        RingingAudio ??= GD.Load<AudioStream>("res://ringing.mp3");
        ThunderAudio ??= GD.Load<AudioStream>("res://thunder.mp3");
        WaveAudio ??= GD.Load<AudioStream>("res://wave.mp3");
        
        // Create main audio player for king and thunder sounds
        _audioPlayer = new AudioStreamPlayer();
        _audioPlayer.Autoplay = true;
        _audioPlayer.Stream = RingingAudio; // Default to ringing for king scene
        AddChild(_audioPlayer);
        
        // Create dedicated wave audio player that will loop continuously
        _wavePlayer = new AudioStreamPlayer();
        _wavePlayer.Stream = WaveAudio;
        _wavePlayer.Autoplay = false;
        _wavePlayer.StreamPaused = true; // Start paused (muted)
        AddChild(_wavePlayer);
        
        // Start the wave audio playing and looping, but muted initially
        if (WaveAudio != null)
        {
            _wavePlayer.Play();
            // Connect the finished signal to restart the audio for looping
            _wavePlayer.Finished += OnWaveFinished;
        }
        
        // Initialize random number generator for thunder pitch variation
        _rng = new RandomNumberGenerator();
        _rng.Randomize();
        
        // Initialize scene durations array
        UpdateSceneDurations();
        
        // Initialize scene state
        _currentSceneIndex = Mathf.Clamp(StartingScene, 0, 3);
        SetActiveScene(_currentSceneIndex);
       
        // Create and configure timer
        _timer = new Timer();
        _timer.WaitTime = _sceneDurations[_currentSceneIndex];
        _timer.Timeout += OnTimerTimeout;
        _timer.Autostart = true;
        AddChild(_timer);
    }
    
    private void OnWaveFinished()
    {
        // Restart wave audio for continuous looping
        _wavePlayer.Play();
    }
    
    private void UpdateSceneDurations()
    {
        _sceneDurations[0] = KingSceneDuration;      // King
        _sceneDurations[1] = LightningSceneDuration; // Lightning (after King)
        _sceneDurations[2] = LuhSceneDuration;       // Luh
        _sceneDurations[3] = LightningSceneDuration; // Lightning (after Luh)
    }
   
    private void OnTimerTimeout()
    {
        // Move to next scene in the sequence (King -> Lightning -> Luh -> Lightning)
        _currentSceneIndex = (_currentSceneIndex + 1) % 4;
        SetActiveScene(_currentSceneIndex);
        
        // Update timer for next scene's duration
        UpdateSceneDurations();
        _timer.WaitTime = _sceneDurations[_currentSceneIndex];
        _timer.Start();
        
        GD.Print($"Switched to: {_sceneNames[_currentSceneIndex]} scene (Duration: {_timer.WaitTime}s)");
    }
    
    private void SetSceneAudio(int sequenceIndex)
    {
        // Map sequence index to actual scene for audio
        int actualSceneIndex = _sceneMapping[sequenceIndex];
        
        // First, pause/unpause wave audio based on scene
        if (actualSceneIndex == 1) // Luh scene
        {
            _wavePlayer.StreamPaused = false; // Unmute wave audio
            _audioPlayer.StreamPaused = true; // Mute other audio
        }
        else
        {
            _wavePlayer.StreamPaused = true; // Mute wave audio
            _audioPlayer.StreamPaused = false; // Unmute other audio
            
            // Set the appropriate audio for non-Luh scenes
            switch (actualSceneIndex)
            {
                case 0: // King
                    _audioPlayer.Stream = RingingAudio;
                    _audioPlayer.PitchScale = 1.0f; // Normal pitch
                    break;
                case 2: // Lightning
                    _audioPlayer.Stream = ThunderAudio;
                    _audioPlayer.PitchScale = _rng.RandfRange(0.8f, 1.2f); // Random pitch variation
                    break;
            }
            
            // Restart audio playback for non-Luh scenes
            _audioPlayer.Stop();
            _audioPlayer.Play();
        }
    }

   
    private void SetActiveScene(int sequenceIndex)
    {
        // Map sequence index to actual scene
        int actualSceneIndex = _sceneMapping[sequenceIndex];
        
        // Hide all scenes first
        KingScene.Visible = false;
        LuhScene.Visible = false;
        LightningScene.Visible = false;
        
        // Disable all 3D cameras first
        KingCamera.Current = false;
        LuhCamera.Current = false;
        
        // Activate the selected scene and camera
        switch (actualSceneIndex)
        {
            case 0: // King
                KingScene.Visible = true;
                KingCamera.Current = true;
                break;
            case 1: // Luh
                LuhScene.Visible = true;
                LuhCamera.Current = true;
                break;
            case 2: // Lightning (Control - no camera needed)
                LightningScene.Visible = true;
                // No camera activation needed for Control nodes
                break;
        }
        
        // Set audio for the scene
        SetSceneAudio(sequenceIndex);
    }
   
    private void RestartTimer()
    {
        UpdateSceneDurations();
        _timer.WaitTime = _sceneDurations[_currentSceneIndex];
        _timer.Start();
    }
   
    // Public methods for external control
    public void StartToggling()
    {
        _timer.Start();
    }
   
    public void StopToggling()
    {
        _timer.Stop();
    }
   
    public void SetSceneDuration(int sequenceIndex, float duration)
    {
        // Map sequence index to the correct duration property
        switch (sequenceIndex)
        {
            case 0: // King
                KingSceneDuration = duration;
                break;
            case 1: // Lightning (after King)
                LightningSceneDuration = duration;
                break;
            case 2: // Luh
                LuhSceneDuration = duration;
                break;
            case 3: // Lightning (after Luh) 
                LightningSceneDuration = duration;
                break;
        }
        
        UpdateSceneDurations();
        
        // Update current timer if we're changing the active scene's duration
        if (sequenceIndex == _currentSceneIndex)
        {
            _timer.WaitTime = duration;
        }
    }
    
    public void SwitchToScene(int sequenceIndex)
    {
        if (sequenceIndex >= 0 && sequenceIndex <= 3)
        {
            _currentSceneIndex = sequenceIndex;
            SetActiveScene(_currentSceneIndex);
            RestartTimer();
        }
    }
    
    public int GetCurrentSceneIndex()
    {
        return _currentSceneIndex;
    }
    
    public string GetCurrentSceneName()
    {
        return _sceneNames[_currentSceneIndex];
    }
    
    // Audio control methods
    public void SetAudioVolume(float volume)
    {
        _audioPlayer.VolumeDb = Mathf.LinearToDb(volume);
        _wavePlayer.VolumeDb = Mathf.LinearToDb(volume);
    }
    
    public void MuteAudio(bool mute)
    {
        _audioPlayer.StreamPaused = mute;
        _wavePlayer.StreamPaused = mute;
    }
    
    // Additional method to control wave audio specifically
    public void SetWaveVolume(float volume)
    {
        _wavePlayer.VolumeDb = Mathf.LinearToDb(volume);
    }
}