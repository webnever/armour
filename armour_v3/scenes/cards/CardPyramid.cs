using Godot;

public partial class CardPyramid : Node3D
{
    // Card properties
    [Export] public float CardWidth = 0.25f;
    [Export] public float CardHeight = 0.35f;
    [Export] public float CardThickness = 0.001f;
    [Export] public float PyramidAngle = 30.0f; // Rotation angle for the upward cards
    [Export] public float ImpulseFactor = 0.0f;
    [Export] public float Randomise = 0.1f;
    [Export] public float Mass = 0.01f;
    
    private RandomNumberGenerator _rng = new RandomNumberGenerator();
    private Timer _simulationTimer;

    public override void _Ready()
    {
        _simulationTimer = new Timer();
        _simulationTimer.WaitTime = 5.0;
        _simulationTimer.OneShot = false;
        _simulationTimer.Timeout += OnSimulationTimerTimeout;
        AddChild(_simulationTimer);
        _simulationTimer.Start();
        
        ResetSimulation();
        StartSimulation();
    }

    private void OnSimulationTimerTimeout()
    {
        _rng.Randomize();
        ResetSimulation();
        StartSimulation();
    }

    private void ResetSimulation()
    {
        foreach (Node child in GetChildren())
        {
            if (child is RigidBody3D)
            {
                child.QueueFree();
            }
        }
        
        // Disable collisions temporarily
        SetPhysicsProcess(false);
        
        // Bottom layer
        CreateLayer(1.5f + 0, 3, false);
        // Middle layer
        CreateLayer(1.5f + CardHeight * Mathf.Cos(Mathf.DegToRad(PyramidAngle)), 2, true);
        // Top layer
        CreateLayer(1.5f + 2 * CardHeight * Mathf.Cos(Mathf.DegToRad(PyramidAngle)), 1, true);
    }

    private void StartSimulation()
    {
        foreach (Node child in GetChildren())
        {
            if (child is RigidBody3D card)
            {
                card.ApplyCentralImpulse(new Vector3(
                    _rng.RandfRange(-Randomise, Randomise), 
                    -ImpulseFactor, 
                    _rng.RandfRange(-Randomise, Randomise)
                )); // A downward force
            }
        }
        
        // Re-enable physics for simulation
        SetPhysicsProcess(true);
    }

    private void CreateLayer(float yPosition, int triangles, bool includeBottom)
    {
        // The horizontal spacing between the apex of each triangle
        float spacing = CardHeight * Mathf.Sin(Mathf.DegToRad(PyramidAngle));
        float totalWidth = triangles * 2 * spacing - spacing * 2;
        float offset = -totalWidth / 2;
        
        for (int i = 0; i < triangles; i++)
        {
            // Center x position for the current triangle
            float triangleCenterX = i * spacing * 2 + offset;
            
            if (includeBottom)
            {
                Vector3 flatCardPosition = new Vector3(triangleCenterX, yPosition, 0);
                RigidBody3D flatCard = CreateCard(flatCardPosition, new Vector3(90, 90, 0));
                AddChild(flatCard);
            }
            
            // Calculate offset from the triangle's center for the upright cards
            float offsetFromCenter = spacing / 2; // Half the base width of the triangle
            
            // Position and add the right upright card
            Vector3 uprightCardPositionRight = new Vector3(triangleCenterX + offsetFromCenter, yPosition + CardHeight / 2, 0);
            RigidBody3D uprightCardRight = CreateCard(uprightCardPositionRight, new Vector3(-PyramidAngle, 90, 0));
            AddChild(uprightCardRight);
            
            // Position and add the left upright card
            Vector3 uprightCardPositionLeft = new Vector3(triangleCenterX - offsetFromCenter, yPosition + CardHeight / 2, 0);
            RigidBody3D uprightCardLeft = CreateCard(uprightCardPositionLeft, new Vector3(PyramidAngle, 90, 0));
            AddChild(uprightCardLeft);
        }
    }

    private RigidBody3D CreateCard(Vector3 position, Vector3 rotation)
    {
        RigidBody3D card = new RigidBody3D();
        card.Position = position;
        
        // Rotate the card with the given rotation values
        card.RotationDegrees = rotation;
        card.Mass = Mass;
        
        // MeshInstance
        MeshInstance3D meshInstance = new MeshInstance3D();
        QuadMesh quadMesh = new QuadMesh();
        quadMesh.Size = new Vector2(CardWidth, CardHeight);
        meshInstance.Mesh = quadMesh;
        
        // Set material with cull mode disabled and add texture
        StandardMaterial3D material = new StandardMaterial3D();
        material.CullMode = BaseMaterial3D.CullModeEnum.Disabled;
        Texture2D texture = GD.Load<Texture2D>("res://scenes/cards/card_Tex.png");
        material.AlbedoTexture = texture;
        meshInstance.MaterialOverride = material;
        
        card.AddChild(meshInstance);
        
        // CollisionShape
        CollisionShape3D collisionShape = new CollisionShape3D();
        BoxShape3D boxShape = new BoxShape3D();
        boxShape.Size = new Vector3(CardWidth, CardHeight, CardThickness);
        collisionShape.Shape = boxShape;
        card.AddChild(collisionShape);
        
        return card;
    }
}