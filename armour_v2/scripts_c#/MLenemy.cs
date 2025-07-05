using Godot;
using System;
using System.Collections.Generic;

public partial class MLEnemy : BaseEnemy
{
    // Q-learning parameters
    [Export] private float learningRate = 0.1f;
    [Export] private float discountFactor = 0.9f;
    [Export] private float explorationRate = 0.2f;

	// Original enemy parameters
	private float currentHealth = 100f;
	private float maxHealth = 100f;
	private float moveSpeed = 2.0f;
	private float detectionRange = 15.0f;
	private float shootingRange = 10.0f;
	private float fireRate = 1.0f;
    
    // State space parameters
    private const int DISTANCE_BINS = 5;  // Number of distance ranges
    private const int HEALTH_BINS = 3;    // Number of health ranges
    private const int ANGLE_BINS = 8;     // Number of angle ranges
    
    // Action space
    private enum Action { 
        Idle, 
        Chase, 
        Attack, 
        Retreat, 
        StrafeCW,    // Clockwise circle strafe
        StrafeCCW    // Counter-clockwise circle strafe
    }
    
    // Q-table: Dictionary with state-action pairs as keys
    private Dictionary<(int, int, int, Action), float> qTable = new();
    
    // Current state tracking
    private (int distanceBin, int healthBin, int angleBin) currentState;
    private Action currentAction = Action.Idle;

	private PlayerController player;
    
    public override void _Ready()
    {
        base._Ready();
        InitializeQTable();

		player = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
    }
    
    private void InitializeQTable()
    {
        // Initialize Q-values for all state-action pairs
        for (int d = 0; d < DISTANCE_BINS; d++)
        {
            for (int h = 0; h < HEALTH_BINS; h++)
            {
                for (int a = 0; a < ANGLE_BINS; a++)
                {
                    foreach (Action action in Enum.GetValues(typeof(Action)))
                    {
                        qTable[(d, h, a, action)] = 0f;
                    }
                }
            }
        }
    }
    
    private (int, int, int) GetCurrentState()
    {
        if (player == null) return (0, 0, 0);
        
        // Distance binning
        float distance = GlobalPosition.DistanceTo(player.GlobalPosition);
        int distanceBin = (int)(distance / detectionRange * DISTANCE_BINS);
        distanceBin = Math.Clamp(distanceBin, 0, DISTANCE_BINS - 1);
        
        // Health binning
        float healthPercent = currentHealth / maxHealth;
        int healthBin = (int)(healthPercent * HEALTH_BINS);
        healthBin = Math.Clamp(healthBin, 0, HEALTH_BINS - 1);
        
        // Angle binning (relative to player)
        Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
        float angle = Mathf.Atan2(toPlayer.X, toPlayer.Z);
        int angleBin = (int)((angle + Mathf.Pi) / (2 * Mathf.Pi) * ANGLE_BINS) % ANGLE_BINS;
        
        return (distanceBin, healthBin, angleBin);
    }
    
    private Action ChooseAction()
    {
        if (GD.Randf() < explorationRate)
        {
            // Exploration: choose random action
            Array actions = Enum.GetValues(typeof(Action));
            return (Action)actions.GetValue(GD.Randi() % actions.Length);
        }
        
        // Exploitation: choose best action from Q-table
        float maxQ = float.MinValue;
        Action bestAction = Action.Idle;
        
        foreach (Action action in Enum.GetValues(typeof(Action)))
        {
            float qValue = qTable[(currentState.distanceBin, currentState.healthBin, currentState.angleBin, action)];
            if (qValue > maxQ)
            {
                maxQ = qValue;
                bestAction = action;
            }
        }
        
        return bestAction;
    }
    
    private float CalculateReward()
    {
        float reward = 0f;
        
        // Reward for being at optimal range
        float distanceToPlayer = GlobalPosition.DistanceTo(player.GlobalPosition);
        float optimalRange = shootingRange * 0.75f;
        float rangeDiff = Math.Abs(distanceToPlayer - optimalRange);
        reward -= rangeDiff * 0.1f;
        
        // Reward for facing player
        Vector3 toPlayer = (player.GlobalPosition - GlobalPosition).Normalized();
        float dotProduct = toPlayer.Dot(Transform.Basis.Z);
        reward += dotProduct * 2f;
        
        // Reward for having line of sight
        var spaceState = GetWorld3D().DirectSpaceState;
        var query = PhysicsRayQueryParameters3D.Create(GlobalPosition, player.GlobalPosition);
        var result = spaceState.IntersectRay(query);
        if (result.Count == 0 || result["collider"].As<Node>() == player)
        {
            reward += 1f;
        }
        
        // Penalty for low health
        float healthPercent = currentHealth / maxHealth;
        if (healthPercent < 0.3f)
        {
            reward -= (0.3f - healthPercent) * 5f;
        }
        
        return reward;
    }
    
    public override void _PhysicsProcess(double delta)
    {
        if (player == null) return;
        
        var newState = GetCurrentState();
        float reward = CalculateReward();
        
        // Update Q-value for previous state-action pair
        if (currentState != default)
        {
            float oldQ = qTable[(currentState.distanceBin, currentState.healthBin, currentState.angleBin, currentAction)];
            float maxNextQ = float.MinValue;
            
            foreach (Action action in Enum.GetValues(typeof(Action)))
            {
                float nextQ = qTable[(newState.Item1, newState.Item2, newState.Item3, action)];
                maxNextQ = Math.Max(maxNextQ, nextQ);
            }
            
            float newQ = oldQ + learningRate * (reward + discountFactor * maxNextQ - oldQ);
            qTable[(currentState.distanceBin, currentState.healthBin, currentState.angleBin, currentAction)] = newQ;
        }
        
        currentState = newState;
        currentAction = ChooseAction();
        
        // Execute chosen action
        ProcessMLAction(currentAction, delta);
        UpdateAim();
    }
    
    private void ProcessMLAction(Action action, double delta)
    {
        Vector3 moveDirection = Vector3.Zero;
        
        switch (action)
        {
            case Action.Chase:
                moveDirection = (player.GlobalPosition - GlobalPosition).Normalized();
                break;
                
            case Action.Retreat:
                moveDirection = (GlobalPosition - player.GlobalPosition).Normalized();
                break;
                
            case Action.Attack:
                if (canShoot)
                {
                    Shoot();
                }
                break;
                
            case Action.StrafeCW:
                {
                    Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
                    moveDirection = new Vector3(toPlayer.Z, 0, -toPlayer.X).Normalized();
                }
                break;
                
            case Action.StrafeCCW:
                {
                    Vector3 toPlayer = player.GlobalPosition - GlobalPosition;
                    moveDirection = new Vector3(-toPlayer.Z, 0, toPlayer.X).Normalized();
                }
                break;
        }
        
        if (moveDirection != Vector3.Zero)
        {
            Velocity = moveDirection * moveSpeed;
            
            if (!IsOnFloor())
            {
                Velocity += Vector3.Down * 9.8f * (float)delta;
            }
            
            MoveAndSlide();
        }
    }
    
    // Override TakeDamage to include additional learning feedback
    public new void TakeDamage(float amount)
    {
        float oldHealth = currentHealth;
        base.TakeDamage(amount);
        
        // Extra negative reward for taking damage
        float damageReward = -amount * 0.5f;
        
        // Update Q-value with damage penalty
        if (currentState != default)
        {
            float oldQ = qTable[(currentState.distanceBin, currentState.healthBin, currentState.angleBin, currentAction)];
            float newQ = oldQ + learningRate * damageReward;
            qTable[(currentState.distanceBin, currentState.healthBin, currentState.angleBin, currentAction)] = newQ;
        }
    }
}