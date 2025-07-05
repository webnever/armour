using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Handles combat encounters
public class CombatSystem
{
    protected GameState _gameState;
    protected Player _player;
    protected Enemy _currentEnemy;
    protected bool _isInCombat;
    private Random _random = new Random();
    
    // Combat settings
    private const float PlayerFleeChance = 0.5f;
    private const float EnemyFleeThreshold = 0.2f; // Enemy might flee when below 20% health
    
    public bool IsInCombat => _isInCombat;
    
    // Add this missing method
    public Enemy GetCurrentEnemy()
    {
        return _currentEnemy;
    }
    
    public CombatSystem(GameState gameState, Player player)
    {
        _gameState = gameState;
        _player = player;
        _isInCombat = false;
    }
    
    public string StartCombat(Enemy enemy)
    {
        _currentEnemy = enemy;
        _isInCombat = true;
        
        // Clone the enemy to avoid modifying the original
        _currentEnemy = enemy.Clone();
        
        return $"You engage in combat with the {_currentEnemy.Name}!\n" +
               $"The {_currentEnemy.Name} has {_currentEnemy.Health} health.\n" +
               "What will you do? (attack, flee, or use [item])";
    }
    
    public virtual string Attack()
    {
        if (!_isInCombat || _currentEnemy == null)
        {
            return "You're not in combat with anyone.";
        }
        
        // Player's turn
        string result = ProcessPlayerAttack();
        
        // Check if enemy is defeated
        if (_currentEnemy.Health <= 0)
        {
            return result + ProcessEnemyDefeat();
        }
        
        // Enemy's turn
        result += ProcessEnemyAction();
        
        // Check if player is defeated
        if (_player.Health <= 0)
        {
            result += ProcessPlayerDefeat();
            return result;
        }
        
        // Combat status
        result += $"\n\nThe {_currentEnemy.Name} has {_currentEnemy.Health}/{_currentEnemy.MaxHealth} HP remaining.\n" +
                  $"You have {_player.Health}/{_player.MaxHealth} HP remaining.\n" +
                  "What will you do? (attack, flee, or use [item])";
        
        return result;
    }
    
    private string ProcessPlayerAttack()
    {
        // Calculate damage with some randomness
        int baseDamage = _player.AttackPower;
        int damageVariation = Math.Max(1, baseDamage / 4); // 25% variation
        int damage = _random.Next(baseDamage - damageVariation, baseDamage + damageVariation + 1);
        
        // Apply enemy defense
        damage = Math.Max(1, damage - _currentEnemy.Defense);
        
        // Apply damage
        _currentEnemy.Health -= damage;
        
        return $"You attack the {_currentEnemy.Name} for {damage} damage!";
    }
    
    // Add missing properties and methods for Enhanced combat system
    public virtual bool CanFlee => true;
    
    // Add method to check if enemy exists and has CanFlee property
    protected bool CanEnemyFlee()
    {
        return _currentEnemy != null && 
               (_currentEnemy.GetType().GetProperty("CanFlee")?.GetValue(_currentEnemy) as bool? ?? true);
    }
    
    // Update ProcessEnemyAction to be virtual so EnhancedCombatSystem can override
    protected virtual string ProcessEnemyAction()
    {
        // Check if enemy wants to flee when low on health
        if (_currentEnemy.Health < _currentEnemy.MaxHealth * EnemyFleeThreshold && 
            CanEnemyFlee() && _random.NextDouble() < 0.3)
        {
            _isInCombat = false;
            return $"\n\nThe {_currentEnemy.Name} flees from combat!";
        }
        
        // Enemy attacks
        int baseDamage = _currentEnemy.AttackPower;
        int damageVariation = Math.Max(1, baseDamage / 4);
        int damage = _random.Next(baseDamage - damageVariation, baseDamage + damageVariation + 1);
        
        // Apply player defense
        damage = Math.Max(1, damage - _player.Defense);
        
        // Apply damage
        _player.Health -= damage;
        
        return $"\n\nThe {_currentEnemy.Name} attacks you for {damage} damage!";
    }
    
    // Make ProcessEnemyDefeat virtual for override
    protected virtual string ProcessEnemyDefeat()
    {
        _isInCombat = false;
        
        // Remove enemy from current location
        var currentLocation = _gameState.GetCurrentLocation();
        if (currentLocation.Enemies != null)
        {
            Enemy enemyToRemove = currentLocation.Enemies.FirstOrDefault(e => 
                e.Name.Equals(_currentEnemy.Name, StringComparison.OrdinalIgnoreCase));
                
            if (enemyToRemove != null)
            {
                currentLocation.Enemies.Remove(enemyToRemove);
            }
        }
        
        // Award experience
        _player.GainExperience(_currentEnemy.ExperienceReward);
        
        // Award satoshi
        _player.Satoshi += _currentEnemy.SatoshiReward; // Changed from Gold to Satoshi
        
        // Chance to drop loot
        string lootMessage = GenerateLoot();
        
        return $"\n\nYou defeated the {_currentEnemy.Name}!\n" +
               $"You gained {_currentEnemy.ExperienceReward} experience and ${_currentEnemy.SatoshiReward}." + // Added $ symbol
               lootMessage;
    }
    
    private string ProcessPlayerDefeat()
    {
        _isInCombat = false;
        
        // Game state will handle what happens when player is defeated
        _gameState.PlayerDefeated();
        
        return $"\n\nYou have been defeated by the {_currentEnemy.Name}!";
    }
    
    private string GenerateLoot()
    {
        // 30% chance to drop an item
        if (_random.NextDouble() >= 0.3)
        {
            return "";
        }
        
        // Create a random item based on enemy type
        Item lootItem = null;
        
        switch (_currentEnemy.Id)
        {
            case "goblin_guard":
                lootItem = new Item
                {
                    Id = "goblin_dagger",
                    Name = "Goblin Dagger",
                    Description = "A crude but effective dagger used by goblins.",
                    ItemType = ItemType.Weapon,
                    UseValue = 3,
                    Category = "Weapon"
                };
                break;
            
            case "skeleton_warrior":
                lootItem = new Item
                {
                    Id = "bone_fragments",
                    Name = "Bone Fragments",
                    Description = "Fragments of yellowed bone. Might be useful for crafting or trade.",
                    ItemType = ItemType.Crafting,
                    Category = "Crafting"
                };
                break;
            
            default:
                // Generic healing potion
                lootItem = new Item
                {
                    Id = "small_healing_potion",
                    Name = "Small Healing Potion",
                    Description = "A tiny vial containing a red liquid that can restore some health.",
                    ItemType = ItemType.Healing,
                    UseValue = 10,
                    Category = "Potion",
                    DefaultUseAction = () => {
                        _player.Health = Math.Min(_player.MaxHealth, _player.Health + 10);
                        return "You drink the small healing potion and recover 10 health.";
                    }
                };
                break;
        }
        
        if (lootItem != null)
        {
            // Add the item to the player's inventory
            _player.Inventory.Add(lootItem);
            return $"\nThe {_currentEnemy.Name} dropped a {lootItem.Name}!";
        }
        
        return "";
    }
    
    public string Flee()
    {
        if (!_isInCombat)
        {
            return "You're not in combat with anyone.";
        }
        
        // Determine if flee attempt is successful
        if (_random.NextDouble() < PlayerFleeChance)
        {
            _isInCombat = false;
            return "You successfully flee from combat!";
        }
        else
        {
            // Failed to flee, enemy gets a free attack
            string result = "You fail to escape!\n\n";
            result += ProcessEnemyAction();
            
            // Check if player is defeated
            if (_player.Health <= 0)
            {
                result += ProcessPlayerDefeat();
                return result;
            }
            
            // Combat status
            result += $"\n\nThe {_currentEnemy.Name} has {_currentEnemy.Health}/{_currentEnemy.MaxHealth} HP remaining.\n" +
                      $"You have {_player.Health}/{_player.MaxHealth} HP remaining.\n" +
                      "What will you do? (attack, flee, or use [item])";
            
            return result;
        }
    }
    
    public string UseItem(Item item)
    {
        if (!_isInCombat)
        {
            return "You're not in combat.";
        }
        
        if (item.ItemType == ItemType.Healing)
        {
            int healAmount = item.UseValue;
            _player.Health = Math.Min(_player.Health + healAmount, _player.MaxHealth);
            _player.Inventory.Remove(item);
            
            return $"You use the {item.Name} and recover {healAmount} health.";
        }
        
        return $"You can't use the {item.Name} in combat.";
    }
    
    // Add method for companion system integration
    public virtual void ProcessCompanionTurn()
    {
        // Base implementation does nothing
        // EnhancedCombatSystem will override this
    }
    
    private void EndCombat()
    {
        _isInCombat = false;
        _currentEnemy = null;
    }
}