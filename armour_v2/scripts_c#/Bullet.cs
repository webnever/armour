using Godot;

public interface IDamageable
{
    void TakeDamage(float amount);
}

public partial class Bullet : RigidBody3D
{
    [Export] private float damage = 25f;
    [Export] private float lifetime = 5.0f;
    [Export] private PackedScene hitEffectScene;
    [Export] private Color bulletColor = new Color(1, 0.8f, 0, 1); // Yellow default

    private Timer lifetimeTimer;
    private bool hasHit = false;
    private Node shooter;
    
    private GpuParticles3D trailParticles;
    private GpuParticles3D impactParticles;
    private MeshInstance3D bulletMesh;
    private OmniLight3D bulletLight;
    
    private const float IMPACT_LIFETIME = 0.5f;

    public override void _Ready()
    {
        // Configure RigidBody properties
        GravityScale = 0;
        CustomIntegrator = true;
        ContinuousCd = true;
        ContactMonitor = true;
        MaxContactsReported = 4;

        // Create bullet mesh
        SetupBulletMesh();

        // Create bullet light
        SetupBulletLight();

        // Create particle systems
        SetupTrailParticles();
        SetupImpactParticles();

        // Setup collision detection
        BodyEntered += OnBodyEntered;

        // Setup lifetime timer
        lifetimeTimer = new Timer();
        AddChild(lifetimeTimer);
        lifetimeTimer.OneShot = true;
        lifetimeTimer.Timeout += QueueFree;
        lifetimeTimer.Start(lifetime);
    }

    private void SetupBulletMesh()
    {
        bulletMesh = new MeshInstance3D();
        AddChild(bulletMesh);

        var cylinder = new CylinderMesh
        {
            Height = 0.1f,
            TopRadius = 0.03f,
            BottomRadius = 0.03f
        };

        var material = new StandardMaterial3D
        {
            Metallic = 1.0f,
            Roughness = 0.1f,
            EmissionEnabled = true,
            EmissionTexture = null,
            Emission = bulletColor,
            EmissionEnergyMultiplier = 2.0f
        };

        cylinder.Material = material;
        bulletMesh.Mesh = cylinder;
        bulletMesh.RotationDegrees = new Vector3(90, 0, 0);

        // Add collision shape
        var collision = new CollisionShape3D();
        AddChild(collision);
        collision.Shape = new CylinderShape3D
        {
            Height = 0.12f,
            Radius = 0.04f
        };
        collision.RotationDegrees = new Vector3(90, 0, 0);
    }

    private void SetupBulletLight()
    {
        bulletLight = new OmniLight3D();
        AddChild(bulletLight);
        
        bulletLight.LightColor = bulletColor;
        bulletLight.LightEnergy = 1.0f;
        bulletLight.LightSize = 0.1f;
        bulletLight.OmniRange = 2.0f;
        bulletLight.Position = new Vector3(0, 0, 0.05f);
    }

    private void SetupTrailParticles()
    {
        trailParticles = new GpuParticles3D();
        AddChild(trailParticles);

        var material = new ParticleProcessMaterial();
        
        // Basic particle properties
        material.Direction = new Vector3(0, 0, 1);
        material.Spread = 5.0f;
        material.InitialVelocityMin = 0.2f;
        material.InitialVelocityMax = 0.4f;
        material.Gravity = Vector3.Zero;
        
        // Particle scale
        material.ScaleMin = 0.1f;
        material.ScaleMax = 0.2f;

        // Color over lifetime
        var gradientTexture = new GradientTexture1D();
        var gradient = new Gradient();
        gradient.AddPoint(0.0f, new Color(bulletColor, 1.0f));
        gradient.AddPoint(1.0f, new Color(bulletColor, 0.0f));
        gradientTexture.Gradient = gradient;
        material.ColorRamp = gradientTexture;

        // Particle mesh
        var sphereMesh = new SphereMesh
        {
            Radius = 0.025f,
            Height = 0.05f
        };

        // Configure GpuParticles3D node
        trailParticles.Amount = 20;
        trailParticles.Lifetime = 0.5f;
        trailParticles.LocalCoords = true;
        trailParticles.ProcessMaterial = material;
        trailParticles.DrawPass1 = sphereMesh;
        trailParticles.Position = new Vector3(0, 0, 0.05f);
        trailParticles.Emitting = true;
    }

    private void SetupImpactParticles()
    {
        impactParticles = new GpuParticles3D();
        AddChild(impactParticles);

        var material = new ParticleProcessMaterial();
        
        // Basic particle properties
        material.Direction = Vector3.Zero;
        material.Spread = 180.0f;
        material.InitialVelocityMin = 2.0f;
        material.InitialVelocityMax = 3.0f;
        material.Gravity = new Vector3(0, -2, 0);
        
        // Particle scale
        material.ScaleMin = 0.1f;
        material.ScaleMax = 0.3f;

        // Angular velocity for spinning particles
        material.AngularVelocityMin = 0;
        material.AngularVelocityMax = 360;

        // Color over lifetime
        var gradientTexture = new GradientTexture1D();
        var gradient = new Gradient();
        gradient.AddPoint(0.0f, new Color(bulletColor.Lightened(0.5f), 1.0f));
        gradient.AddPoint(0.5f, new Color(bulletColor, 0.5f));
        gradient.AddPoint(1.0f, new Color(bulletColor.Darkened(0.5f), 0.0f));
        gradientTexture.Gradient = gradient;
        material.ColorRamp = gradientTexture;

        // Use same mesh as trail
        var sphereMesh = new SphereMesh
        {
            Radius = 0.025f,
            Height = 0.05f
        };

        // Configure GpuParticles3D node
        impactParticles.Amount = 15;
        impactParticles.Lifetime = 0.5f;
        impactParticles.OneShot = true;
        impactParticles.Explosiveness = 1.0f;
        impactParticles.LocalCoords = true;
        impactParticles.ProcessMaterial = material;
        impactParticles.DrawPass1 = sphereMesh;
        impactParticles.Emitting = false;
    }

    public override void _PhysicsProcess(double delta)
    {
        if (!hasHit)
        {
            RotateZ(Mathf.Sin((float)Time.GetTicksMsec() / 100f) * 0.01f);
        }
    }

    public override void _IntegrateForces(PhysicsDirectBodyState3D state)
    {
        state.LinearVelocity = LinearVelocity;
    }

    public void Initialize(float newDamage, Vector3 direction, float speed)
    {
        damage = newDamage;
        LinearVelocity = direction * speed;
        LookAt(Position + direction);

        if (trailParticles?.ProcessMaterial is ParticleProcessMaterial material)
        {
            material.InitialVelocityMin = speed * 0.1f;
            material.InitialVelocityMax = speed * 0.2f;
        }
    }

    public void SetShooter(Node node)
    {
        shooter = node;
    }

    private void OnBodyEntered(Node body)
    {
        if (hasHit || body == shooter) return;

        hasHit = true;

        if (body is IDamageable damageable)
        {
            damageable.TakeDamage(damage);
        }

        if (bulletMesh != null) bulletMesh.Visible = false;
        if (trailParticles != null) trailParticles.Emitting = false;
        if (bulletLight != null) bulletLight.Visible = false;

        if (impactParticles != null)
        {
            impactParticles.Emitting = true;
        }

        if (hitEffectScene != null)
        {
            var hitEffect = hitEffectScene.Instantiate<Node3D>();
            GetTree().Root.AddChild(hitEffect);
            hitEffect.GlobalPosition = GlobalPosition;
        }

        var impactTimer = GetTree().CreateTimer(IMPACT_LIFETIME);
        impactTimer.Timeout += QueueFree;

        Freeze = true;
    }
}