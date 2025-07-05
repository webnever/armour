using Godot;
using System.Collections.Generic;

public partial class BaseEnemy : CharacterBody3D, IDamageable
{
    [Export] private float maxHealth = 100f;
    [Export] private float moveSpeed = 2.0f;
    [Export] private float gravity = 9.8f;  // Added gravity export
    [Export] private float detectionRange = 15.0f;
    [Export] private float shootingRange = 10.0f;
    [Export] private float fireRate = 1.0f;
    [Export] private PackedScene bulletScene;
    [Export] private NodePath weaponHolderPath;
    
    private float currentHealth;
    private float previousHealth;
    private PlayerController player;
    private Node3D weaponHolder;
    private Timer shootCooldown;
    public bool canShoot = true;
    private Vector3 aimDirection = Vector3.Forward;
    
    // UI and Effects
    private DamageNumbers damageNumbers;
    private Node3D damageNumbersOrigin;
    private SubViewport subViewport;
    private Camera3D camera;
    private Control healthBarControl;
    private ProgressBar healthBar;
    [Export] private Color healthBarColor = Colors.Red;
    [Export] private Color healthBarBgColor = Colors.Black;
    [Export] private float healthBarOffset = -1.5f; // Height above enemy
    private const float HEALTH_BAR_WIDTH = 150.0f;  // Smaller width for better scale
    private const float HEALTH_BAR_HEIGHT = 7.5f;  // Smaller height
    private const float FADE_DURATION = 0.15f;  // Duration for fade effects
    
    private Tween fadeTween;

    // State machine for basic AI
    private enum State { Idle, Chase, Attack, Retreat }
    private State currentState = State.Idle;

    private Camera3D GetActiveCamera()
    {
        // First try to get the camera from the viewport if we have it
        if (subViewport?.GetCamera3D() != null)
        {
            return subViewport.GetCamera3D();
        }
            
        // Otherwise try to find the active camera in the scene
        var viewport = GetViewport();
        if (viewport?.GetCamera3D() != null)
        {
            return viewport.GetCamera3D();
        }
            
        // If still not found, look for any camera in the scene
        var cameras = GetTree().GetNodesInGroup("Cameras");
        if (cameras.Count > 0)
            return cameras[0] as Camera3D;
            
        GD.Print("No camera found!");
        return null;
    }
    
    public override void _Ready()
    {
        currentHealth = maxHealth;
        previousHealth = maxHealth;
        
        player = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
        weaponHolder = GetNode<Node3D>(weaponHolderPath);
        
        shootCooldown = new Timer();
        AddChild(shootCooldown);
        shootCooldown.OneShot = true;
        shootCooldown.Timeout += OnShootCooldownComplete;

        damageNumbers = GetNode<DamageNumbers>("/root/DamageNumbers");
        damageNumbersOrigin = new Node3D();
        AddChild(damageNumbersOrigin);
        damageNumbersOrigin.Position = new Vector3(0, 2, 0);
        
        SetupHealthBar();
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (player == null) return;
        
        UpdateState();
        ProcessState(delta);
        UpdateAim();
    }
    
    public override void _Process(double delta)
    {
        UpdateDamageNumbers();
        UpdateHealthBarPosition();
        UpdateHealthBarValue();
    }
    
    private void UpdateDamageNumbers()
    {
        // Get or update camera reference
        camera = GetActiveCamera();
        if (camera == null || damageNumbers == null) return;

        // Calculate damage
        int damageTaken = (int)previousHealth - (int)currentHealth;
        if (damageTaken > 0)
        {
            var viewportPos = camera.UnprojectPosition(damageNumbersOrigin.GlobalTransform.Origin);
            damageNumbers.DisplayNumber(damageTaken, viewportPos, DamageType.Enemy);
        }
        previousHealth = currentHealth;
    }
    
    private void UpdateState()
    {
        float distanceToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
        
        if (distanceToPlayer > detectionRange)
        {
            currentState = State.Idle;
        }
        else if (distanceToPlayer > shootingRange)
        {
            currentState = State.Chase;
        }
        else if (distanceToPlayer < shootingRange * 0.5f)
        {
            currentState = State.Retreat;
        }
        else
        {
            currentState = State.Attack;
        }
    }
    
    private void ProcessState(double delta)
    {
        Vector3 moveDirection = Vector3.Zero;
        
        switch (currentState)
        {
            case State.Idle:
                // Maybe patrol or stand guard
                break;
                
            case State.Chase:
                moveDirection = (player.GlobalPosition - GlobalPosition);
                moveDirection.Y = 0; // Zero out vertical component for horizontal movement
                moveDirection = moveDirection.Normalized();
                break;
                
            case State.Attack:
                if (canShoot)
                {
                    Shoot();
                }
                break;
                
            case State.Retreat:
                moveDirection = (GlobalPosition - player.GlobalPosition);
                moveDirection.Y = 0; // Zero out vertical component for horizontal movement
                moveDirection = moveDirection.Normalized();
                break;
        }
        
        // Handle horizontal movement
        Vector3 horizontalVelocity = moveDirection * moveSpeed;
        
        // Preserve current vertical velocity
        Vector3 velocity = Velocity;
        
        // Apply gravity
        if (!IsOnFloor())
        {
            velocity.Y -= gravity * (float)delta;
        }
        else if (velocity.Y < 0)
        {
            // Reset vertical velocity when on floor
            velocity.Y = 0;
        }
        
        // Combine horizontal movement with vertical velocity
        velocity.X = horizontalVelocity.X;
        velocity.Z = horizontalVelocity.Z;
        
        Velocity = velocity;
        MoveAndSlide();
    }
    
    public void UpdateAim()
    {
        if (player == null) return;

        // Define the projectile height and planes
        float projectileHeight = 1.0f;
        var groundPlane = new Plane(Vector3.Up, GlobalPosition.Y);
        var projectilePlane = new Plane(Vector3.Up, GlobalPosition.Y + projectileHeight);

        // Calculate direction to player
        Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
        
        // Project the direction onto the projectile plane to get the aim point (M)
        Vector3 M = new Vector3(
            player.GlobalPosition.X,
            GlobalPosition.Y + projectileHeight,
            player.GlobalPosition.Z
        );

        // Get ground point (G) by projecting M down to ground plane
        Vector3 G = new Vector3(M.X, GlobalPosition.Y, M.Z);

        // Get enemy position (C)
        Vector3 C = GlobalPosition;

        // Calculate aim direction for projectile
        aimDirection = (M - new Vector3(C.X, M.Y, C.Z)).Normalized();

        // Calculate rotation angle
        float angle = -Mathf.Atan2(G.Z - C.Z, G.X - C.X) + Mathf.Pi/2 + Mathf.Pi;

        // Smooth rotation
        float currentAngle = Rotation.Y;
        float targetAngle = angle;
        float smoothing = 0.15f;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, smoothing);

        // Apply rotation
        Rotation = new Vector3(0, newAngle, 0);
        weaponHolder.Rotation = Vector3.Zero;
    }
    
    public void Shoot()
    {
        if (bulletScene == null) return;
        
        var bullet = bulletScene.Instantiate<Node3D>();
        GetTree().Root.AddChild(bullet);
        
        bullet.GlobalPosition = weaponHolder.GlobalPosition;
        bullet.Rotation = Rotation;
        
        if (bullet is Bullet bulletComponent)
        {
            bulletComponent.Initialize(10f, aimDirection, 15f);
            bulletComponent.SetShooter(this);
        }
        
        canShoot = false;
        shootCooldown.Start(1.0f / fireRate);
    }
    
    private void OnShootCooldownComplete()
    {
        canShoot = true;
    }
    
    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
        
        // Flash the health bar when taking damage
        if (healthBar != null)
        {
            var stylebox = (StyleBoxFlat)healthBar.GetThemeStylebox("fill");
            stylebox.BgColor = Colors.White;
            
            // Create a timer to reset the color
            var timer = GetTree().CreateTimer(0.1);
            timer.Timeout += () => stylebox.BgColor = healthBarColor;
        }
        
        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        // Fade out health bar before destroying
        FadeOut();
        
        // Queue enemy for deletion after fade
        var deathTween = CreateTween();
        deathTween.TweenInterval(FADE_DURATION);
        deathTween.TweenCallback(Callable.From(() => QueueFree()));
    }

    private void SetupHealthBar()
    {
        var mainUI = GetNode<Control>("/root/mainScene/Control");
        if (mainUI == null)
        {
            GD.PrintErr("Failed to find main UI at /root/mainScene/Control");
            return;
        }

        healthBarControl = new Control();
        mainUI.AddChild(healthBarControl);
        
        // Create the ProgressBar with smaller dimensions
        healthBar = new ProgressBar();
        healthBarControl.AddChild(healthBar);
        
        // Configure the ProgressBar with more reasonable dimensions
        healthBar.MinValue = 0;
        healthBar.MaxValue = maxHealth;
        healthBar.Value = currentHealth;
        //healthBar.CustomMinimumSize = new Vector2(HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT);  // Much smaller values
        Vector2 size = new Vector2(HEALTH_BAR_WIDTH, HEALTH_BAR_HEIGHT);
        healthBar.SetSize(size);  // Match the custom minimum size
        
        // Center the health bar
        healthBar.Position = new Vector2(-healthBar.Size.X / 2, 0);
        
        // Style the health bar with corner radius for better look
        var stylebox = new StyleBoxFlat();
        stylebox.BgColor = healthBarColor;
        stylebox.SetCornerRadiusAll(2);   // Rounded corners
        healthBar.AddThemeStyleboxOverride("fill", stylebox);
        
        var bgStylebox = new StyleBoxFlat();
        bgStylebox.BgColor = healthBarBgColor;
        bgStylebox.SetCornerRadiusAll(2); // Rounded corners
        healthBar.AddThemeStyleboxOverride("background", bgStylebox);
        
        healthBar.ShowPercentage = false;
    }
    
    private void UpdateHealthBarPosition()
    {
        camera = GetActiveCamera();
        if (camera == null || healthBarControl == null) return;

        Vector3 worldPos = GlobalPosition + Vector3.Up * healthBarOffset;
        Vector2 screenPos = camera.UnprojectPosition(worldPos);

        healthBarControl.Position = screenPos;

        // Simpler visibility check - only hide if behind or too far
        float distanceToCamera = camera.GlobalPosition.DistanceTo(GlobalPosition);
        bool isInFront = (GlobalPosition - camera.GlobalPosition).Dot(camera.GlobalTransform.Basis.Z) < 0;
        healthBarControl.Visible = isInFront && distanceToCamera < 50f;  // 50 units view distance, adjust as needed
    }


    private void UpdateHealthBarValue()
    {
        if (healthBar == null) return;
        
        // Smoothly update the health bar value
        float smoothSpeed = 10.0f;
        healthBar.Value = Mathf.Lerp(
            (float)healthBar.Value, 
            currentHealth, 
            (float)GetProcessDeltaTime() * smoothSpeed
        );
    }

    private void FadeIn()
    {
        fadeTween?.Kill();
        fadeTween = CreateTween();
        fadeTween.TweenProperty(healthBar, "modulate:a", 1.0f, FADE_DURATION);
    }

    private void FadeOut()
    {
        fadeTween?.Kill();
        fadeTween = CreateTween();
        fadeTween.TweenProperty(healthBar, "modulate:a", 0.0f, FADE_DURATION);
        fadeTween.TweenCallback(Callable.From(() => 
        {
            healthBar?.QueueFree();
            healthBarControl?.QueueFree();
        }));
    }

    public override void _ExitTree()
    {
        // Clean up health bar UI elements
        if (healthBar != null)
        {
            healthBar.QueueFree();
            healthBar = null;
        }
        if (healthBarControl != null)
        {
            healthBarControl.QueueFree();
            healthBarControl = null;
        }
        
        camera = null;

        base._ExitTree();
    }
}