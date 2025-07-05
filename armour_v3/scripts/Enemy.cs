using Godot;
using System;
using System.Collections.Generic;

// Represents an enemy in the game
public class Enemy
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    // Combat stats
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    
    // Rewards
    public int ExperienceReward { get; set; }
    public int SatoshiReward { get; set; }
    
    // Loot table - items this enemy might drop
    public List<LootEntry> PossibleLoot { get; set; } = new List<LootEntry>();
    
    // Special abilities or behaviors
    public Dictionary<string, Action<GameState>> SpecialAbilities { get; set; } = new Dictionary<string, Action<GameState>>();
    
    // Dialog for intelligent enemies
    public Dictionary<string, string> Dialog { get; set; } = new Dictionary<string, string>();
    
    // Additional properties that affect combat behavior
    public bool CanFlee { get; set; } = true;
    public bool IsAggressive { get; set; } = true;
    public string DeathMessage { get; set; }
    
    // Tags for categorization
    public List<string> Tags { get; set; } = new List<string>();
    
    // Create a copy of this enemy (useful for combat instances)
    public Enemy Clone()
    {
        return new Enemy
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            Health = this.Health,
            MaxHealth = this.MaxHealth,
            AttackPower = this.AttackPower,
            Defense = this.Defense,
            ExperienceReward = this.ExperienceReward,
            SatoshiReward = this.SatoshiReward,
            PossibleLoot = new List<LootEntry>(this.PossibleLoot),
            SpecialAbilities = new Dictionary<string, Action<GameState>>(this.SpecialAbilities),
            Dialog = new Dictionary<string, string>(this.Dialog),
            CanFlee = this.CanFlee,
            IsAggressive = this.IsAggressive,
            DeathMessage = this.DeathMessage,
            Tags = new List<string>(this.Tags)
        };
    }
    
    // Calculate a special attack (can be overridden by specific enemy types)
    public virtual (string description, int damage) SpecialAttack()
    {
        // Default implementation just does a slightly stronger attack
        int damage = (int)(AttackPower * 1.5);
        return ($"{Name} performs a powerful strike!", damage);
    }
    
    // Check if this enemy should use a special attack this turn
    public virtual bool ShouldUseSpecialAttack(int turnNumber, float healthPercentage)
    {
        // By default, use special attack if health below 50% and on every third turn
        return healthPercentage < 0.5f && turnNumber % 3 == 0;
    }
    
    // Get a response to specific player actions
    public string GetResponse(string playerAction)
    {
        if (Dialog.TryGetValue(playerAction.ToLower(), out string response))
        {
            return response;
        }
        
        return null; // No specific response
    }
}

// Represents a potential loot drop with a chance
public class LootEntry
{
    public string ItemId { get; set; }
    public float DropChance { get; set; } // 0.0 to 1.0
    
    public LootEntry(string itemId, float dropChance)
    {
        ItemId = itemId;
        DropChance = dropChance;
    }
}