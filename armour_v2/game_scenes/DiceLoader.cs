using Godot;
using System.Collections.Generic;

public partial class DiceLoader : TextureRect
{
    private List<Texture2D> _diceTextures = new();
    private int _currentFrame = 0;
    private Timer _rotationTimer;
    
    public override void _Ready()
    {
        // Load all dice rotation images
        for (int i = 0; i < 24; i++)
        {
            var texture = GD.Load<Texture2D>($"res://dice_rotations/dice_{i:000}.png");
            _diceTextures.Add(texture);
        }
        
        // Set initial texture
        Texture = _diceTextures[0];
        
        // Setup rotation timer
        _rotationTimer = new Timer();
        AddChild(_rotationTimer);
        _rotationTimer.WaitTime = 0.10; // 5 frames per second
        _rotationTimer.Timeout += OnRotationTimeout;

		StartRotation();
    }
    
    public void StartRotation()
    {
        _rotationTimer.Start();
    }
    
    public void StopRotation()
    {
        _rotationTimer.Stop();
    }
    
    private void OnRotationTimeout()
    {
        NextFrame();
    }
    
    private void NextFrame()
    {
        _currentFrame = (_currentFrame + 1) % _diceTextures.Count;
        Texture = _diceTextures[_currentFrame];
    }
    
    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton mouseEvent && 
            mouseEvent.Pressed && 
            mouseEvent.ButtonIndex == MouseButton.Left)
        {
            NextFrame();
        }
    }
    
    // Optional: Clean up
    public override void _ExitTree()
    {
        _rotationTimer?.QueueFree();
    }
}