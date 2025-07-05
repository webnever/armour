using System.Runtime.CompilerServices;
using Godot;
using Godot.Collections;

public enum WeaponType
{
    Gun,
    Sword,
    SMG
}

// Create a new class to handle weapon data
public class WeaponData
{
    public string Name { get; set; }
    public WeaponType Type { get; set; }
    public float Damage { get; set; }
    public float AttackRate { get; set; } // Attacks per second (applies to both gun fire rate and sword swing rate)
    public float Range { get; set; }

    // Gun-specific properties
    public float ReloadTime { get; set; }
    public int MagazineSize { get; set; }
    public int CurrentAmmo { get; set; }
    public int ReserveAmmo { get; set; }
    public int MaxReserveAmmo { get; set; }
    public PackedScene BulletScene { get; set; }
    public float BulletSpeed { get; set; }

    // Sword-specific properties
    public float SwingArc { get; set; } // Angle of sword swing in degrees
    public float SwingDuration { get; set; } // How long the swing animation takes
}

public partial class PlayerController : CharacterBody3D, IDamageable
{
    // Cursor & Camera <---------------------------------->
    private enum CursorState { Default, Interact, Dialogue, Shooting }

    private CursorState currentCursorState = CursorState.Default;
    private Resource cursorArrow;
    private Resource cursorLink;
    private Resource cursorShoot;

    private InputEventMouseButton mouseButtonEvent;
    private Vector2 mousePosition;
    private Vector2 viewportMousePosition;
    private Camera3D camera;

    private readonly Vector2 VIEWPORT_SCALE = new(1920f, 1080f);

    private float defaultSize;  // Will be set from scene
    private float aimingSize;   // Will be calculated based on aimSizeModifier
    private float tweenDuration = 0.2f;
    private Tween currentSizeTween;

    [Export(PropertyHint.Range, "0.1,2.0")]
    private float aimSizeModifier = 0.9f;  // Default to 90%, can be changed in editor

    [Export]
    private float maxCameraOffset = 1.0f;  // Maximum distance camera can move from center

    [Export(PropertyHint.Range, "0.0,1.0")]
    private float cameraMouseInfluence = 0.5f;  // How much mouse position affects camera

    [Export(PropertyHint.Range, "0.0,1.0")]
    private float cameraSmoothing = 0.15f;  // Lower = smoother camera movement

    private Vector3 targetCameraOffset = Vector3.Zero;
    private Vector3 currentCameraOffset = Vector3.Zero;
    private Vector3 defaultCameraPosition;

    private SubViewport weaponViewport;
    private Camera3D weaponCamera;
    [Export] private WeaponDisplay weaponDisplay;
    // <--------------------------------------------------->

    // Cache frequently accessed nodes
    private GameSceneManager mainScene;
    private SubViewport subViewport;
    private DialogueManager dialogueManager;
    private AnimationPlayer animationPlayer;
    private TextureProgressBar healthBar;
    private TextureProgressBar manaBar;
    private DamageNumbers damageNumbers;
    private Node3D damageNumbersOrigin;
    private IInteractable currentInteractable;
    private bool isHoveringInteractable = false;
    private bool isContextMenuOpen = false;

    // Player Properties <---------------->
    public float HP = 500.0f;
    public float MP = 20.0f;
    private float maxHP = 500.0f;
    private float maxMP = 20.0f;
    private float previousHP = 500.0f;
    private float previousMP = 20.0f;

    private float walkSpeed = 3.50f;
    private float runSpeed = 3.50f * 1.5f;

    // Physics constants
    private const float GRAVITY_CONSTANT = -9.81f;  // Standard gravity in m/s²
    private const float GRAVITY_SCALE = 3.0f;       // Adjust this to make gravity feel stronger
    private float gravity = GRAVITY_CONSTANT * GRAVITY_SCALE;
    private Vector3 gravityVelocity = Vector3.Zero;

    private float interactRange = 4.0f;
    private float raycastRange = 1000.0f;

    public bool inputEnabled = false;

    private bool isAiming = false;
    // <---------------------------------->

    // Weapon <--------------------------->
    private Node3D weaponHolder;
    private Timer shootCooldown;
    private Timer reloadTimer;
    private WeaponData currentWeapon;
    private bool isReloading = false;
    private bool canShoot = true;

    private WeaponData swordData;
    private WeaponData gunData;
    private Node3D sword;
    private Node3D swordOrigin;
    private bool isSwordSwinging = false;
    private Tween swordSwingTween;
    private Tween fadeTween;

    private Node3D handgun;
    private MeshInstance3D handgunMesh;
    private Tween handgunFadeTween;

    private WeaponData smgData;
    private Node3D smg;
    private MeshInstance3D smgMesh;
    private Tween smgFadeTween;

    // Weapon rotation
    private Vector3 aimDirection = Vector3.Forward;

    [Export] private PackedScene bulletScene;

    private Control ammoCounter;

    private Node3D handgunMuzzle;
    private Node3D smgMuzzle;
    // <---------------------------------->

    private ContextMenu contextMenu;

    private TerminalConsole _debugConsole;
    [Export]
    public bool EnableDebugConsole = true;

    public delegate void InteractableChangedHandler(IInteractable newInteractable);
    public event InteractableChangedHandler CurrentInteractableChanged;

    // VEHICLE
    private bool isInVehicle = false;
    private IVehicle currentVehicle = null;

    public override void _Ready()
    {
        // Cache node references
        cursorArrow = ResourceLoader.Load("res://cursor/tex_cursor_pointer.png");
        cursorLink = ResourceLoader.Load("res://cursor/tex_cursor_link.png");
        cursorShoot = ResourceLoader.Load("res://cursor/crosshair-normal.png");

        subViewport = GetNode<SubViewport>("/root/mainScene/Control/SubViewport");
        animationPlayer = GetNode<AnimationPlayer>("player/blockbench_export/AnimationPlayer");
        mainScene = GetNode<GameSceneManager>("/root/mainScene/GameSceneManager");
        dialogueManager = GetNode<DialogueManager>("/root/DialogueManager/");

        damageNumbersOrigin = GetNode<Node3D>("DamageNumbersOrigin");
        damageNumbers = GetNode<DamageNumbers>("/root/DamageNumbers");
        healthBar = GetNode<TextureProgressBar>("/root/mainScene/Control/uiElements/healthBar");
        manaBar = GetNode<TextureProgressBar>("/root/mainScene/Control/uiElements/healthBar2");

        weaponHolder = GetNode<Node3D>("WeaponHolder");

        handgun = GetNode<Node3D>("handgun");
        handgunMesh = handgun.GetNode<MeshInstance3D>("mesh");
        handgun.Visible = false; // Hide handgun by default

        _debugConsole = GetNode<TerminalConsole>("/root/mainScene/Control/uiElements/TerminalConsole");

        shootCooldown = new Timer();
        AddChild(shootCooldown);
        shootCooldown.OneShot = true;
        shootCooldown.Timeout += OnShootCooldownComplete;

        reloadTimer = new Timer();
        AddChild(reloadTimer);
        reloadTimer.OneShot = true;
        reloadTimer.Timeout += OnReloadComplete;

        // Initialize sword
        sword = GetNode<Node3D>("sword");
        swordOrigin = GetNode<Node3D>("sword/swordOrigin");
        sword.Visible = false; // Hide sword by default

        smg = GetNode<Node3D>("smg");
        smgMesh = smg.GetNode<MeshInstance3D>("mesh");
        smg.Visible = false;

        handgunMuzzle = handgun.GetNode<Node3D>("muzzle");
        smgMuzzle = smg.GetNode<Node3D>("muzzle");

        // Initialize weapon data
        gunData = new WeaponData
        {
            Name = "Handgun",
            Type = WeaponType.Gun,
            Damage = 25f,
            AttackRate = 2f,
            Range = 20f,
            ReloadTime = 1.5f,
            MagazineSize = 12,
            CurrentAmmo = 12,
            ReserveAmmo = 60,
            MaxReserveAmmo = 120,
            BulletScene = bulletScene,
            BulletSpeed = 20f
        };

        swordData = new WeaponData
        {
            Name = "Sword",
            Type = WeaponType.Sword,
            Damage = 50f,
            AttackRate = 1.5f,
            Range = 2f,
            SwingArc = 180f,
            SwingDuration = 0.15f
        };

        smgData = new WeaponData
        {
            Name = "SMG",
            Type = WeaponType.SMG,
            Damage = 15f,          // Less damage per shot than regular gun
            AttackRate = 8f,       // Much higher fire rate
            Range = 15f,           // Slightly less range than regular gun
            ReloadTime = 2.0f,     // Longer reload time due to larger magazine
            MagazineSize = 30,     // Larger magazine size
            CurrentAmmo = 30,
            ReserveAmmo = 120,
            MaxReserveAmmo = 240,
            BulletScene = bulletScene,
            BulletSpeed = 25f      // Slightly faster bullets
        };

        currentWeapon = swordData;
        SwitchWeapon(WeaponType.Sword);

        ammoCounter = GetNode<Control>("/root/mainScene/Control/uiElements/ammoCounter");
        UpdateAmmoCounter();

        contextMenu = GetNode<ContextMenu>("/root/mainScene/contextMenu");
        if (contextMenu == null)
        {
            DebugLog("Failed to find context menu!", "error");
        }
    }

    // Update physics process to properly handle movement
    public override void _PhysicsProcess(double delta)
    {
        Vector3 inputVector = GetInputVector();

        if (inputVector.Length() > 0)
        {
            if (contextMenu != null && contextMenu.Visible)
            {
                contextMenu.Hide();
                isContextMenuOpen = false;
            }
        }

        Vector3 rotatedInput = RotateInput(inputVector);

        // Apply movement and gravity
        Vector3 velocity = CalculateVelocity(rotatedInput, delta);
        Velocity = velocity;

        // Apply movement
        MoveAndSlide();

        // Handle animations based on movement
        HandleAnimation(rotatedInput.Length());
    }

    // Optional: Add jumping capability
    private float jumpForce = 4.5f;

    public void Jump()
    {
        if (IsOnFloor())
        {
            gravityVelocity.Y = jumpForce;
        }
    }

    public override void _Process(double delta)
    {
        UpdateHealthBars();
        UpdateDamageNumbers();
        HandleRaycast();
        UpdateCamera(delta);

        if (CanProcessInput())
        {
            UpdateAimDirection();
            // Handle shooting - now requires both right click (aim) and left click
            if (isAiming && Input.IsActionPressed("shoot") && canShoot && !isReloading &&
    (currentWeapon.Type == WeaponType.Gun || currentWeapon.Type == WeaponType.SMG) &&
    currentWeapon.CurrentAmmo > 0)
            {
                Shoot();
            }

            // Handle reloading
            if (Input.IsActionJustPressed("reload") && !isReloading && currentWeapon.CurrentAmmo < currentWeapon.MagazineSize)
            {
                StartReload();
            }
        }
    }

    private Vector3 GetInputVector()
    {
        if (!CanProcessInput())
            return Vector3.Zero;

        Vector3 input = new Vector3(
            Input.GetActionStrength("left_right") - Input.GetActionStrength("left_left"),
            0,
            Input.GetActionStrength("left_down") - Input.GetActionStrength("left_up")
        );

        return SnapToEightDirections(input.Normalized());
    }

    private bool CanProcessInput()
    {
        bool canProcess = inputEnabled &&
               !mainScene.InGameMenu &&
               !dialogueManager.inDialogue;

        if (!canProcess)
        {
        }

        return canProcess;
    }

    private Vector3 RotateInput(Vector3 input)
    {
        const float ROTATION_ANGLE = -45;
        float radians = Mathf.DegToRad(ROTATION_ANGLE);
        float cosAngle = Mathf.Cos(radians);
        float sinAngle = Mathf.Sin(radians);

        return new Vector3(
            input.X * cosAngle - input.Z * sinAngle,
            0,
            input.X * sinAngle + input.Z * cosAngle
        );
    }

    // Update this method to properly handle gravity with delta time
    private Vector3 CalculateVelocity(Vector3 input, double delta)
    {
        float currentSpeed = Input.IsActionPressed("shift") ? runSpeed : walkSpeed;

        // Calculate horizontal velocity
        Vector3 horizontalVelocity = new Vector3(
            input.X * currentSpeed,
            0,
            input.Z * currentSpeed
        );

        // Apply gravity with proper delta time integration
        gravityVelocity.Y += gravity * (float)delta;

        // Terminal velocity check (optional but recommended)
        const float terminalVelocity = -53.0f; // Typical terminal velocity
        gravityVelocity.Y = Mathf.Max(gravityVelocity.Y, terminalVelocity);

        // If we're on the floor, reset gravity accumulation
        if (IsOnFloor())
        {
            gravityVelocity.Y = 0;
        }

        // Combine horizontal movement with gravity
        return horizontalVelocity + gravityVelocity;
    }

    private Vector3 SnapToEightDirections(Vector3 vector)
    {
        if (vector.Length() == 0)
            return vector;

        float angle = Mathf.Atan2(vector.Z, vector.X);
        int octant = Mathf.RoundToInt(4 * angle / Mathf.Pi);
        float snappedAngle = Mathf.Pi / 4 * octant;

        return new Vector3(
            Mathf.Cos(snappedAngle),
            0,
            Mathf.Sin(snappedAngle)
        ).Normalized();
    }

    private void HandleAnimation(float movementLength)
    {
        // Don't change animation if we're shooting or reloading
        if (animationPlayer.CurrentAnimation == "shoot" || animationPlayer.CurrentAnimation == "reload")
            return;

        string newAnimation = movementLength == 0
            ? "idle"
            : Input.IsActionPressed("shift") ? "sprintcycle2" : "runcycle";

        if (animationPlayer.CurrentAnimation != newAnimation)
        {
            animationPlayer.Play(newAnimation);
        }
    }

    public void EnableInput()
    {
        inputEnabled = true;
    }

    public void DisableInput()
    {
        inputEnabled = false;
        Velocity = Vector3.Zero;  // Stop any current movement
        HandleAnimation(0);  // Reset to idle animation
    }

    private void UpdateHealthBars()
    {
        float oldHP = HP;
        float oldMP = MP;

        healthBar.Size = new Vector2(maxHP, healthBar.Size.Y);
        manaBar.Size = new Vector2(maxMP * 10, manaBar.Size.Y);

        HP = Mathf.Clamp(HP, 0, maxHP);
        MP = Mathf.Clamp(MP, 0, maxMP);

        if (oldHP != HP || oldMP != MP)
        {
            DebugLog($"Health updated - HP: {HP}/{maxHP}, MP: {MP}/{maxMP}", "info");
        }

        healthBar.Value = HP / maxHP * 100;
        manaBar.Value = MP / maxMP * 100;
    }

    private void UpdateDamageNumbers()
    {
        if (camera == null || subViewport?.GetCamera3D() == null) return;

        var viewportPos = subViewport.GetCamera3D().UnprojectPosition(damageNumbersOrigin.GlobalTransform.Origin);

        int damageTakenHP = (int)previousHP - (int)HP;
        int damageTakenMP = (int)previousMP - (int)MP;

        if (damageTakenHP > 0)
        {
            DebugLog($"Player took {damageTakenHP} HP damage", "warning");
            damageNumbers.DisplayNumber(damageTakenHP, viewportPos, DamageType.PlayerHP);
        }

        if (damageTakenMP > 0)
        {
            DebugLog($"Player lost {damageTakenMP} MP", "warning");
            damageNumbers.DisplayNumber(damageTakenMP, viewportPos, DamageType.PlayerMP);
        }

        previousHP = HP;
        previousMP = MP;
    }

    public void TakeDamage(float amount)
    {
        HP -= amount;
        if (HP <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        DisableInput();
        DebugLog("Player died!", "error");
    }

    public override void _Input(InputEvent @event)
    {
        // Skip all input if context menu is visible (except for closing it)
        if (contextMenu != null && contextMenu.Visible)
        {
            if (@event is InputEventMouseButton clickEvent && clickEvent.ButtonIndex == MouseButton.Left && clickEvent.Pressed)
            {
                // Check if click is outside the context menu
                if (!contextMenu.GetGlobalRect().HasPoint(clickEvent.Position))
                {
                    contextMenu.Hide();
                    isContextMenuOpen = false;
                    GetTree().Root.SetInputAsHandled();
                }
            }
            return;
        }

        // Handle mouse events
        if (@event is InputEventMouseButton mouseButton)
        {
            // Handle weapon switching with scroll wheel
            if (!isReloading && !isSwordSwinging)
            {
                if (mouseButton.ButtonIndex == MouseButton.WheelUp)
                {
                    // Cycle through weapons in reverse order
                    switch (currentWeapon.Type)
                    {
                        case WeaponType.Gun:
                            SwitchWeapon(WeaponType.SMG);
                            break;
                        case WeaponType.SMG:
                            SwitchWeapon(WeaponType.Sword);
                            break;
                        case WeaponType.Sword:
                            SwitchWeapon(WeaponType.Gun);
                            break;
                    }
                    return;
                }
                else if (mouseButton.ButtonIndex == MouseButton.WheelDown)
                {
                    // Cycle through weapons in order
                    switch (currentWeapon.Type)
                    {
                        case WeaponType.Sword:
                            SwitchWeapon(WeaponType.Gun);
                            break;
                        case WeaponType.Gun:
                            SwitchWeapon(WeaponType.SMG);
                            break;
                        case WeaponType.SMG:
                            SwitchWeapon(WeaponType.Sword);
                            break;
                    }
                    return;
                }
            }

            HandleMouseButtonInput(mouseButton);
        }
        else if (@event is InputEventMouseMotion mouseMotionEvent)
        {
            mousePosition = mouseMotionEvent.Position;
            viewportMousePosition = mousePosition;
            HandleMouseMotion(mouseMotionEvent);
        }

        // Console command
        if (Input.IsActionJustPressed("open_console") && _debugConsole != null && _debugConsole._timerFinished)
        {
            _debugConsole.ToggleConsole();
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion mouseMotionEvent)
    {
        if (camera == null) return;

        var spaceState = GetWorld3D().DirectSpaceState;
        var from = camera.ProjectRayOrigin(mouseMotionEvent.Position);
        var to = from + camera.ProjectRayNormal(mouseMotionEvent.Position) * raycastRange;

        var query = new PhysicsRayQueryParameters3D
        {
            From = from,
            To = to,
            CollideWithAreas = true,
            CollideWithBodies = true,
            CollisionMask = 1 << 2
        };

        var result = spaceState.IntersectRay(query);
        isHoveringInteractable = false;

        if (result != null && result.Count > 0 && result.ContainsKey("collider"))
        {
            var collider = (Node3D)result["collider"];
            if (collider != null && (collider.GlobalPosition - GlobalPosition).Length() <= interactRange && collider is IInteractable)
            {
                isHoveringInteractable = true;
                currentInteractable = collider as IInteractable;
                UpdateCursorState(CursorState.Interact);
            }
            else
            {
                currentInteractable = null;
                UpdateCursorState(CursorState.Default);
            }
        }
        else
        {
            currentInteractable = null;
            UpdateCursorState(CursorState.Default);
        }
    }

    private void HandleMouseButtonInput(InputEventMouseButton mouseButtonEvent)
    {
        // Skip all input handling if context menu is open
        if (contextMenu != null && contextMenu.Visible)
        {
            if (mouseButtonEvent.ButtonIndex == MouseButton.Left && mouseButtonEvent.Pressed)
            {
                // Check if click is outside the context menu
                if (!contextMenu.GetGlobalRect().HasPoint(mouseButtonEvent.Position))
                {
                    contextMenu.Hide();
                    isContextMenuOpen = false;
                    GetTree().Root.SetInputAsHandled();
                }
            }
            return;
        }

        // Handle dialogue clicks first
        if (dialogueManager.inDialogue && dialogueManager.dialogueBox.animCompleted &&
            mouseButtonEvent.ButtonIndex == MouseButton.Left && mouseButtonEvent.Pressed)
        {
            // Only progress dialogue if clicking within the dialogue box
            if (dialogueManager.dialogueBox.HasPoint(mouseButtonEvent.Position))
            {
                dialogueManager.ShowNextPart();
                GetTree().Root.SetInputAsHandled();
                return;
            }
        }

        // Handle weapon switching with scroll wheel
        if (!isReloading && !isSwordSwinging)
        {
            if (mouseButtonEvent.ButtonIndex == MouseButton.WheelUp)
            {
                // Cycle through weapons in reverse order
                switch (currentWeapon.Type)
                {
                    case WeaponType.Gun:
                        SwitchWeapon(WeaponType.SMG);
                        break;
                    case WeaponType.SMG:
                        SwitchWeapon(WeaponType.Sword);
                        break;
                    case WeaponType.Sword:
                        SwitchWeapon(WeaponType.Gun);
                        break;
                }
                return;
            }
            else if (mouseButtonEvent.ButtonIndex == MouseButton.WheelDown)
            {
                // Cycle through weapons in order
                switch (currentWeapon.Type)
                {
                    case WeaponType.Sword:
                        SwitchWeapon(WeaponType.Gun);
                        break;
                    case WeaponType.Gun:
                        SwitchWeapon(WeaponType.SMG);
                        break;
                    case WeaponType.SMG:
                        SwitchWeapon(WeaponType.Sword);
                        break;
                }
                return;
            }
        }

        // Right Click
        if (mouseButtonEvent.ButtonIndex == MouseButton.Right)
        {
            if (mouseButtonEvent.Pressed)
            {
                if (isHoveringInteractable && currentInteractable != null)
                {
                    // Show context menu for interactable
                    var actions = currentInteractable.GetContextActions();
                    if (actions != null && actions.Count > 0)
                    {
                        contextMenu.ShowMenu(mouseButtonEvent.Position, actions);
                        isContextMenuOpen = true;
                        GetTree().Root.SetInputAsHandled();
                    }
                }
                else
                {
                    HandleAimStart();
                }
            }
            else
            {
                if (!isHoveringInteractable)
                {
                    HandleAimEnd();
                }
            }
        }
        // Left Click
        else if (mouseButtonEvent.ButtonIndex == MouseButton.Left && mouseButtonEvent.Pressed)
        {
            if (isAiming)
            {
                if (currentWeapon.Type == WeaponType.Sword && !isSwordSwinging)
                {
                    SwingSword();
                }
            }
            else if (currentInteractable != null && !isContextMenuOpen)
            {
                HandleInteraction(currentInteractable);
            }
        }
    }

    // Replace your entire HandleAimStart method with:
    private void HandleAimStart()
    {
        isAiming = true;
        currentCursorState = CursorState.Shooting;
        SetCursorForState(CursorState.Shooting);

        switch (currentWeapon.Type)
        {
            case WeaponType.Gun:
                TweenSize(aimingSize);
                if (handgun != null)
                {
                    handgun.Visible = true;
                    FadeWeapon(true, WeaponType.Gun);
                }
                break;

            case WeaponType.SMG:
                TweenSize(aimingSize);
                if (smg != null)
                {
                    smg.Visible = true;
                    FadeWeapon(true, WeaponType.SMG);
                }
                break;

            case WeaponType.Sword:
                sword.Visible = true;
                swordOrigin.Rotation = new Vector3(
                    Mathf.DegToRad(-15),
                    Mathf.DegToRad(90),
                    0
                );
                FadeSword(1.0f, 0.2f);
                break;
        }
    }

    // Replace your entire HandleAimEnd method with:
    private void HandleAimEnd()
    {
        isAiming = false;
        currentCursorState = CursorState.Default;
        SetCursorForState(CursorState.Default);

        switch (currentWeapon.Type)
        {
            case WeaponType.Gun:
                TweenSize(defaultSize);
                FadeWeapon(false, WeaponType.Gun);
                break;

            case WeaponType.SMG:
                TweenSize(defaultSize);
                FadeWeapon(false, WeaponType.SMG);
                break;

            case WeaponType.Sword:
                FadeWeapon(false, WeaponType.Sword);
                swordOrigin.Rotation = Vector3.Zero;
                break;
        }
    }


    private void FadeWeapon(bool fadeIn, WeaponType weaponType)
    {
        float targetAlpha = fadeIn ? 1.0f : 0.0f;
        float duration = 0.2f;

        switch (weaponType)
        {
            case WeaponType.Sword:
                FadeSword(targetAlpha, duration);
                break;
            case WeaponType.Gun:
                FadeHandgun(targetAlpha, duration);
                break;
            case WeaponType.SMG:
                FadeSMG(targetAlpha, duration);
                break;
        }
    }

    private void FadeSMG(float targetAlpha, float duration)
    {
        if (smg == null || smgMesh == null)
        {
            DebugLog("SMG nodes are null!", "error");
            return;
        }

        if (smgFadeTween != null && smgFadeTween.IsValid())
        {
            smgFadeTween.Kill();
        }

        smgFadeTween = CreateTween();
        bool tweenerCreated = false;

        var material = smgMesh.GetActiveMaterial(0);
        if (material is ShaderMaterial shaderMaterial)
        {
            smgFadeTween.TweenProperty(
                shaderMaterial,
                "shader_parameter/alpha",
                targetAlpha,
                duration
            ).SetTrans(Tween.TransitionType.Linear);

            tweenerCreated = true;
        }

        if (!tweenerCreated)
        {
            DebugLog("No valid shader materials found for SMG fade effect!", "warning");
            smgFadeTween.Kill();
            return;
        }

        // Hide the SMG when fade out completes
        if (targetAlpha == 0.0f)
        {
            smgFadeTween.TweenCallback(Callable.From(() =>
            {
                smg.Visible = false;
            }));
        }

        smgFadeTween.Play();
    }

    private void FadeHandgun(float targetAlpha, float duration)
    {
        if (handgun == null || handgunMesh == null)
        {
            DebugLog("Handgun nodes are null!", "error");
            return;
        }

        if (handgunFadeTween != null && handgunFadeTween.IsValid())
        {
            handgunFadeTween.Kill();
        }

        handgunFadeTween = CreateTween();
        bool tweenerCreated = false;

        var material = handgunMesh.GetActiveMaterial(0);
        if (material is ShaderMaterial shaderMaterial)
        {
            handgunFadeTween.TweenProperty(
                shaderMaterial,
                "shader_parameter/alpha",
                targetAlpha,
                duration
            ).SetTrans(Tween.TransitionType.Linear);

            tweenerCreated = true;
        }

        if (!tweenerCreated)
        {
            DebugLog("No valid shader materials found for handgun fade effect!", "warning");
            handgunFadeTween.Kill();
            return;
        }

        // Hide the handgun when fade out completes
        if (targetAlpha == 0.0f)
        {
            handgunFadeTween.TweenCallback(Callable.From(() =>
            {
                handgun.Visible = false;
            }));
        }

        handgunFadeTween.Play();
    }

    private void SwingSword()
    {
        if (isSwordSwinging) return;

        isSwordSwinging = true;

        if (swordSwingTween != null && swordSwingTween.IsValid())
        {
            swordSwingTween.Kill();
        }

        swordSwingTween = CreateTween();

        // We're already at 90 degrees from aiming, so start the swing from there
        swordSwingTween.TweenProperty(
            swordOrigin,
            "rotation:y",
            Mathf.DegToRad(-90),  // Swing through to -90 degrees
            swordData.SwingDuration
        ).SetTrans(Tween.TransitionType.Sine)
         .SetEase(Tween.EaseType.InOut);

        // Add vertical tilt animation
        swordSwingTween.Parallel().TweenProperty(
            swordOrigin,
            "rotation:x",
            Mathf.DegToRad(15),  // End with downward tilt
            swordData.SwingDuration
        ).SetTrans(Tween.TransitionType.Sine)
         .SetEase(Tween.EaseType.In);

        // Check for hits at the midpoint
        swordSwingTween.TweenCallback(Callable.From(() =>
        {
            CheckSwordCollision();
        })).SetDelay(swordData.SwingDuration * 0.5f);

        // Reset position after swing
        swordSwingTween.TweenCallback(Callable.From(() =>
        {
            isSwordSwinging = false;
            // Return to aiming position
            swordOrigin.Rotation = new Vector3(
                Mathf.DegToRad(-15),
                Mathf.DegToRad(90),
                0
            );
        }));

        swordSwingTween.Play();
    }

    private async void HandleInteraction(IInteractable interactable)
    {
        if (dialogueManager.inDialogue && dialogueManager.dialogueBox.animCompleted)
        {
            await dialogueManager.ShowNextPart();
            return;
        }

        if (interactable != null && !mainScene.InGameMenu && !dialogueManager.inDialogue)
        {
            // Check if the interactable is a vehicle
            if (interactable is IVehicle vehicle)
            {
                currentVehicle = vehicle;
                isInVehicle = true;
            }

            interactable.Interact();
            UpdateCursorState(CursorState.Default);
        }
    }

    private bool IsLeftMouseClick() =>
        mouseButtonEvent?.Pressed == true &&
        mouseButtonEvent.ButtonIndex == MouseButton.Left;

    private void UpdateCursorState(CursorState newState)
    {
        if (currentCursorState != newState)
        {
            currentCursorState = newState;
            SetCursorForState(newState);
        }
    }

    private void SetCursorForState(CursorState state)
    {
        var cursorResource = state switch
        {
            CursorState.Interact => cursorLink,
            CursorState.Dialogue => cursorLink,
            CursorState.Shooting => cursorShoot,
            _ => cursorArrow
        };

        if (cursorResource == null) return;

        Vector2 hotspot = state == CursorState.Shooting
            ? new Vector2(9, 9)
            : Vector2.Zero;

        Input.SetCustomMouseCursor(cursorResource, Input.CursorShape.Arrow, hotspot);
    }

    private void UpdateAimDirection()
    {
        if (camera == null) return;

        var mousePos = GetViewport().GetMousePosition();
        var from = camera.ProjectRayOrigin(mousePos);
        var to = from + camera.ProjectRayNormal(mousePos) * 1000;

        float projectileHeight = 1.0f;
        var groundPlane = new Plane(Vector3.Up, GlobalPosition.Y);
        var projectilePlane = new Plane(Vector3.Up, GlobalPosition.Y + projectileHeight);

        var projectileHitPoint = projectilePlane.IntersectsRay(from, to);
        if (!projectileHitPoint.HasValue) return;

        Vector3 M = projectileHitPoint.Value;
        Vector3 G = new Vector3(M.X, GlobalPosition.Y, M.Z);
        Vector3 C = GlobalPosition;

        aimDirection = (M - new Vector3(C.X, M.Y, C.Z)).Normalized();
        float angle = -Mathf.Atan2(G.Z - C.Z, G.X - C.X) + Mathf.Pi / 2 + Mathf.Pi;

        float currentAngle = Rotation.Y;
        float targetAngle = angle;
        float smoothing = 0.15f;
        float newAngle = Mathf.LerpAngle(currentAngle, targetAngle, smoothing);

        Rotation = new Vector3(0, newAngle, 0);
        weaponHolder.Rotation = Vector3.Zero;
    }

    private void UpdateCamera(double delta)
    {
        if (camera == null) return;

        if (isAiming)
        {
            if (currentWeapon.Type == WeaponType.Gun)
                targetCameraOffset = CalculateCameraOffset();
            else if (currentWeapon.Type == WeaponType.Sword)
                targetCameraOffset = Vector3.Zero; // Or different offset logic
        }
        else
        {
            targetCameraOffset = Vector3.Zero;
        }

        currentCameraOffset = currentCameraOffset.Lerp(
            targetCameraOffset,
            (float)(1 - Mathf.Pow(1 - cameraSmoothing, delta * 60))
        );

        camera.Position = defaultCameraPosition + currentCameraOffset;
    }

    private Vector3 CalculateCameraOffset()
    {
        Vector2 viewSize = GetViewport().GetVisibleRect().Size;
        Vector2 mousePos = GetViewport().GetMousePosition();
        Vector2 normalizedMouse = new Vector2(
            (mousePos.X / viewSize.X) * 2 - 1,
            (mousePos.Y / viewSize.Y) * 2 - 1
        );

        float xOffset = normalizedMouse.X * maxCameraOffset * cameraMouseInfluence;
        float yOffset = normalizedMouse.Y * maxCameraOffset * cameraMouseInfluence;

        return new Vector3(xOffset, 0, yOffset);
    }

    public void SetCamera(Camera3D newCamera)
    {
        camera = newCamera;
        defaultSize = camera.Size;
        aimingSize = defaultSize * aimSizeModifier;
        defaultCameraPosition = camera.Position;

        currentCursorState = CursorState.Default;
        SetCursorForState(CursorState.Default);
    }

    private void SwitchWeapon(WeaponType type)
    {
        if (isReloading || isSwordSwinging) return;

        switch (type)
        {
            case WeaponType.Sword:
                currentWeapon = swordData;
                break;
            case WeaponType.Gun:
                currentWeapon = gunData;
                break;
            case WeaponType.SMG:
                currentWeapon = smgData;
                break;
        }

        // Hide all weapons
        if (sword != null) sword.Visible = false;
        if (handgun != null) handgun.Visible = false;
        if (smg != null) smg.Visible = false;
        if (weaponHolder != null) weaponHolder.Visible = false;

        // Reset states
        isAiming = false;
        currentCursorState = CursorState.Default;
        SetCursorForState(CursorState.Default);

        // Update UI elements
        if (ammoCounter != null)
        {
            ammoCounter.Visible = type == WeaponType.Gun || type == WeaponType.SMG;
            if (type == WeaponType.Gun || type == WeaponType.SMG)
            {
                UpdateAmmoCounter();
            }
        }

        weaponDisplay?.UpdateWeaponVisibility(type);
        DebugLog($"Switched to {currentWeapon.Name}", "info");
    }


    private void OnShootCooldownComplete()
    {
        canShoot = true;
    }

    private void OnReloadComplete()
    {
        int ammoNeeded = currentWeapon.MagazineSize - currentWeapon.CurrentAmmo;
        int ammoToAdd = Mathf.Min(ammoNeeded, currentWeapon.ReserveAmmo);

        currentWeapon.CurrentAmmo += ammoToAdd;
        currentWeapon.ReserveAmmo -= ammoToAdd;

        DebugLog($"Reload complete. New ammo: {currentWeapon.CurrentAmmo}, Reserve: {currentWeapon.ReserveAmmo}", "info");

        isReloading = false;
        UpdateAmmoCounter();
    }

    private void TweenSize(float targetSize)
    {
        if (camera == null) return;

        if (currentSizeTween != null && currentSizeTween.IsValid())
        {
            currentSizeTween.Kill();
        }

        currentSizeTween = CreateTween();
        currentSizeTween.TweenProperty(
            camera,
            "size",
            targetSize,
            tweenDuration
        ).SetTrans(Tween.TransitionType.Sine)
         .SetEase(Tween.EaseType.InOut);
    }

    private void FadeSword(float targetAlpha, float duration)
    {
        if (sword == null)
        {
            DebugLog("Sword node is null!", "error");
            return;
        }

        if (fadeTween != null && fadeTween.IsValid())
        {
            fadeTween.Kill();
        }

        fadeTween = CreateTween();
        bool tweenerCreated = false;

        foreach (var child in swordOrigin.GetChildren())
        {
            if (child is MeshInstance3D mesh)
            {
                var material = mesh.GetActiveMaterial(0);
                if (material is ShaderMaterial shaderMaterial)
                {

                    // Create a property tweener for the shader's alpha uniform
                    fadeTween.TweenProperty(
                        shaderMaterial,
                        "shader_parameter/alpha",
                        targetAlpha,
                        duration
                    ).SetTrans(Tween.TransitionType.Linear);

                    tweenerCreated = true;
                }
            }
        }

        if (!tweenerCreated)
        {
            DebugLog("No valid shader materials found for fade effect!", "warning");
            fadeTween.Kill();
            return;
        }

        // If fading out, hide the sword when the tween completes
        if (targetAlpha == 0.0f)
        {
            fadeTween.TweenCallback(Callable.From(() =>
            {
                sword.Visible = false;
            }));
        }

        fadeTween.Play();
    }

    private void CheckSwordCollision()
    {
        var spaceState = GetWorld3D().DirectSpaceState;

        float arcRadius = 2.0f;
        float arcAngle = 160.0f;
        int numRays = 8;

        Vector3 swordBase = swordOrigin.GlobalPosition;
        Vector3 forward = -Transform.Basis.Z;
        Vector3 right = Transform.Basis.X;

        for (int i = 0; i < numRays; i++)
        {
            float angle = Mathf.DegToRad(-arcAngle / 2 + (arcAngle * i / (numRays - 1)));
            Vector3 rayDirection = (forward * Mathf.Cos(angle) + right * Mathf.Sin(angle)).Normalized();

            var query = new PhysicsRayQueryParameters3D
            {
                From = swordBase,
                To = swordBase + rayDirection * arcRadius,
                CollideWithAreas = true,
                CollideWithBodies = true,
                CollisionMask = 1
            };

            var result = spaceState.IntersectRay(query);
            if (result.Count > 0 && result.ContainsKey("collider"))
            {
                var hitObject = (Node3D)result["collider"];
                if (hitObject is IDamageable damageable && hitObject != this)
                {
                    damageable.TakeDamage(currentWeapon.Damage);
                    DebugLog($"Sword hit: {hitObject.Name}", "debug");
                }
            }
        }
    }

    private void UpdateAmmoCounter()
    {
        if (ammoCounter == null) return;

        Label label = ammoCounter.GetNode<Label>("Label");
        if (label != null)
        {
            label.Text = $"{currentWeapon.CurrentAmmo}/{currentWeapon.ReserveAmmo}";
        }
    }

    private void DebugLog(string message, string type = "normal")
    {
        if (EnableDebugConsole && _debugConsole != null)
        {
            _debugConsole.PrintMessage($"{message}", type);
        }
    }

    private void HandleRaycast()
    {
        // Skip raycast if context menu is visible
        if (contextMenu != null && contextMenu.Visible)
            return;

        if (HandleDialogueCursor())
            return;

        if (camera == null)
        {
            DebugLog("Camera not initialized for raycast", "warning");
            return;
        }

        var spaceState = GetWorld3D().DirectSpaceState;
        var rayResult = CastInteractionRay(spaceState);
        UpdateCursorFromRaycast(rayResult);
    }

    private void Shoot()
    {
        if (!canShoot || isReloading || currentWeapon.CurrentAmmo <= 0)
        {
            return;
        }

        // Get the correct muzzle position based on weapon type
        Node3D muzzle = currentWeapon.Type switch
        {
            WeaponType.Gun => handgunMuzzle,
            WeaponType.SMG => smgMuzzle,
            _ => weaponHolder
        };

        // Spawn bullet
        var bullet = (Node3D)currentWeapon.BulletScene.Instantiate();
        GetTree().Root.AddChild(bullet);

        // Set bullet position and rotation using the muzzle
        bullet.GlobalPosition = muzzle.GlobalPosition;
        bullet.Rotation = muzzle.GlobalRotation;

        // Add velocity to bullet
        if (bullet is Bullet bulletComponent)
        {
            bulletComponent.Initialize(currentWeapon.Damage, aimDirection, currentWeapon.BulletSpeed);
            bulletComponent.SetShooter(this);
        }

        // Update ammo
        currentWeapon.CurrentAmmo--;
        DebugLog($"Ammo remaining: {currentWeapon.CurrentAmmo}/{currentWeapon.ReserveAmmo}", "debug");
        UpdateAmmoCounter();

        // Start cooldown
        canShoot = false;
        shootCooldown.Start(1.0f / currentWeapon.AttackRate);

        // Play animation
        if (animationPlayer.HasAnimation("shoot"))
        {
            animationPlayer.Play("shoot");
        }
    }

    private void StartReload()
    {
        if (currentWeapon.ReserveAmmo <= 0)
        {
            DebugLog("Cannot reload: No reserve ammo", "warning");
            return;
        }

        if (currentWeapon.CurrentAmmo >= currentWeapon.MagazineSize)
        {
            DebugLog("Cannot reload: Magazine full", "warning");
            return;
        }

        DebugLog($"Starting reload. Current ammo: {currentWeapon.CurrentAmmo}, Reserve: {currentWeapon.ReserveAmmo}", "info");
        isReloading = true;
        reloadTimer.Start(currentWeapon.ReloadTime);

        if (animationPlayer.HasAnimation("reload"))
        {
            animationPlayer.Play("reload");
        }
    }

    private bool HandleDialogueCursor()
    {
        if (!dialogueManager.inDialogue)
            return false;

        var newState = dialogueManager.dialogueBox.animCompleted
            ? CursorState.Dialogue
            : CursorState.Default;

        if (currentCursorState != newState)
        {
            UpdateCursorState(newState);
        }

        return true;
    }

    private void UpdateCursorFromRaycast(Dictionary rayResult)
    {
        if (isAiming)
        {
            if (currentCursorState != CursorState.Shooting)
            {
                UpdateCursorState(CursorState.Shooting);
            }
            return;
        }

        CursorState newState = CursorState.Default;
        IInteractable newInteractable = null;

        if (!dialogueManager.inDialogue)
        {
            if (rayResult != null && rayResult.Count > 0 && rayResult.ContainsKey("collider"))
            {
                var collider = (Node3D)rayResult["collider"];
                if (IsValidInteractable(collider))
                {
                    newState = CursorState.Interact;
                    newInteractable = collider as IInteractable;
                }
            }
        }

        // Check if the interactable has changed
        if (currentInteractable != newInteractable)
        {
            currentInteractable = newInteractable;
            CurrentInteractableChanged?.Invoke(currentInteractable);
        }

        if (currentCursorState != newState)
        {
            UpdateCursorState(newState);
        }
    }

    private Dictionary CastInteractionRay(PhysicsDirectSpaceState3D spaceState)
    {
        var query = new PhysicsRayQueryParameters3D
        {
            From = camera.ProjectRayOrigin(viewportMousePosition),
            To = camera.ProjectRayOrigin(viewportMousePosition) + camera.ProjectRayNormal(viewportMousePosition) * raycastRange,
            CollideWithAreas = true,
            CollideWithBodies = true,
            CollisionMask = 1 << 2,
        };

        var result = spaceState.IntersectRay(query);
        if (result.Count > 0)
        {
        }
        return result;
    }

    private bool IsValidInteractable(Node3D collider)
    {
        return collider != null &&
               (collider.GlobalPosition - GlobalPosition).Length() <= interactRange &&
               collider is IInteractable;
    }

    public void SetPhysicsMode(bool enabled)
    {
        ProcessMode = enabled ? ProcessModeEnum.Always : ProcessModeEnum.Disabled;
    }

    // Add this method to play specific animations
    public void PlayAnimation(string animationName)
    {
        if (animationPlayer.HasAnimation(animationName))
        {
            animationPlayer.Play(animationName);
        }
    }
}