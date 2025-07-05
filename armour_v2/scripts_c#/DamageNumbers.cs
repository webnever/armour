using Godot;
using System;

public enum DamageType
{
    PlayerHP,
    PlayerMP,
    Enemy
}

public partial class DamageNumbers : Node
{
    private Font font;
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    
    public static readonly Color DamageColorHP = new Color(1, 0.2f, 0.2f);
    public static readonly Color DamageColorMP = new Color(0.2f, 0.2f, 1);
    public static readonly Color DamageColorEnemy = new Color(1, 0.8f, 0.2f);

    private Camera3D gameCamera;
    private SubViewport gameViewport;
    //private readonly Vector2 VIEWPORT_SCALE = new(1920f / 480f, 1080f / 270f);

    private partial class DamageLabel : Label
    {
        public Vector3 InitialWorldPos { get; set; }
        public Vector2 InitialScreenPos { get; set; }
        private Camera3D camera;

        public DamageLabel(Camera3D camera)
        {
            this.camera = camera;
        }

        public override void _Process(double delta)
        {
            if (camera != null)
            {
                var updatedScreenPos = camera.UnprojectPosition(InitialWorldPos);
                GlobalPosition = updatedScreenPos;
            }
        }
    }

    public override void _Ready()
    {
        font = GD.Load<Font>("res://fonts/MicrogrammaDMedExt.ttf");
    }

    public void Initialize(Camera3D camera, SubViewport viewport)
    {
        gameCamera = camera;
        gameViewport = viewport;
    }

    public async void DisplayNumber(int value, Vector2 basePosition, DamageType type)
    {
        if (gameCamera == null || gameViewport == null)
        {
            GD.PrintErr("DamageNumbers: Camera or Viewport not initialized!");
            return;
        }

        Color color = type switch
        {
            DamageType.PlayerHP => DamageColorHP,
            DamageType.PlayerMP => DamageColorMP,
            DamageType.Enemy => DamageColorEnemy,
            _ => DamageColorHP
        };
        
        // Smaller random offset
        // var randomOffset = new Vector2(
        //     rng.RandfRange(-32, 32), // Direct pixel values for 1920x1080
        //     rng.RandfRange(-32, 32)
        // );

        // Scale the base position
        var screenPosition = basePosition;

        var number = new DamageLabel(gameCamera);

        number.InitialScreenPos = screenPosition;
        number.InitialWorldPos = gameCamera.ProjectPosition(screenPosition, 1.0f); // Unscale for world position

        number.GlobalPosition = screenPosition;// + new Vector2(rng.RandfRange(-48, 48), rng.RandfRange(-48, 48));
        number.Text = value.ToString();
        number.ZIndex = 10;
        number.LabelSettings = new LabelSettings();
        number.LabelSettings.Font = font;
        number.LabelSettings.FontColor = color;
        number.LabelSettings.FontSize = 48;
        number.LabelSettings.OutlineColor = new Color("#000");
        number.LabelSettings.OutlineSize = 8;
        
        CallDeferred("add_child", number);
        await ToSignal(number, "resized");
        
        number.PivotOffset = new Vector2(number.Size.X / 2, number.Size.Y / 2);
        
        var initialY = number.Position.Y;
        var tween = GetTree().CreateTween();
        
        // Remove SetParallel to make tweens sequential
        // First tween: Move up
        tween.TweenProperty(
            number, "position:y", initialY - 42, 0.25f
        ).SetEase(Tween.EaseType.InOut);
        
        // Second tween: Move down
        tween.TweenProperty(
            number, "position:y", initialY + 21, 0.5f
        ).SetEase(Tween.EaseType.In);
        
        // Finally: Fade out with scale
        tween.TweenProperty(
            number, "scale", Vector2.Zero, 0.25f
        ).SetEase(Tween.EaseType.In);

        await ToSignal(tween, "finished");
        number.QueueFree();
    }
}