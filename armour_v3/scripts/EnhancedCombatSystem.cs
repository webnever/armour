using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public enum CombatStance
{
    Balanced,   // Normal damage and defense
    Aggressive, // +30% damage, -15% defense
    Defensive   // -15% damage, +30% defense
}

public enum StatusEffect
{
    None,
    Poisoned,    // Damage over time
    Stunned,     // Skip turn
    Blessed,     // +15% damage
    Cursed,      // -15% damage
    Shielded     // Absorbs damage
}

public class CombatStatus
{
    public StatusEffect Effect { get; set; }
    public int Duration { get; set; } // Turns remaining
    public int Strength { get; set; } // Effect strength
    
    public CombatStatus(StatusEffect effect, int duration, int strength = 1)
    {
        Effect = effect;
        Duration = duration;
        Strength = strength;
    }
}

public class EnhancedCombatSystem : CombatSystem
{
    private CombatStance _playerStance = CombatStance.Balanced;
    private List<CombatStatus> _playerStatuses = new List<CombatStatus>();
    private List<CombatStatus> _enemyStatuses = new List<CombatStatus>();
    private Random _random = new Random();

    // Simplified combat constants
    private const float CRIT_CHANCE_BASE = 0.1f;
    private const float CRIT_DAMAGE_MULTIPLIER = 1.5f;

    public EnhancedCombatSystem(GameState gameState, Player player) : base(gameState, player)
    {
    }

    public string SetStance(CombatStance stance)
    {
        if (!IsInCombat)
        {
            return "You're not in combat.";
        }

        _playerStance = stance;

        string description = stance switch
        {
            CombatStance.Aggressive => "You adopt an aggressive stance! (+30% damage, -15% defense)",
            CombatStance.Defensive => "You take a defensive stance! (-15% damage, +30% defense)",
            _ => "You return to a balanced fighting stance."
        };

        return description + "\n" + ProcessEnemyAction();
    }

    public override string Attack()
    {
        if (!IsInCombat || _currentEnemy == null)
        {
            return "You're not in combat with anyone.";
        }

        // Check if player is stunned
        if (HasStatusEffect(_playerStatuses, StatusEffect.Stunned))
        {
            RemoveStatusEffect(_playerStatuses, StatusEffect.Stunned);
            return "You're stunned and cannot act this turn!" + ProcessEnemyAction();
        }

        // Calculate damage with stance modifiers
        float stanceMultiplier = _playerStance switch
        {
            CombatStance.Aggressive => 1.3f,
            CombatStance.Defensive => 0.85f,
            _ => 1.0f
        };

        int baseDamage = (int)(_player.AttackPower * stanceMultiplier);

        // Check for critical hit (simplified)
        bool isCrit = _random.NextDouble() < CRIT_CHANCE_BASE;
        if (isCrit)
        {
            baseDamage = (int)(baseDamage * CRIT_DAMAGE_MULTIPLIER);
        }

        // Apply status effect modifiers
        if (HasStatusEffect(_playerStatuses, StatusEffect.Blessed))
            baseDamage = (int)(baseDamage * 1.15f);
        if (HasStatusEffect(_playerStatuses, StatusEffect.Cursed))
            baseDamage = (int)(baseDamage * 0.85f);

        // Apply damage
        int finalDamage = Math.Max(1, baseDamage - _currentEnemy.Defense);
        _currentEnemy.Health -= finalDamage;

        string combatLog = $"You attack the {_currentEnemy.Name} for {finalDamage} damage";
        if (isCrit) combatLog += " (CRITICAL HIT!)";
        combatLog += "!";

        // Check if enemy defeated
        if (_currentEnemy.Health <= 0)
        {
            return combatLog + "\n" + ProcessEnemyDefeat();
        }

        // Process status effects
        combatLog += ProcessStatusEffects();

        return combatLog + ProcessEnemyAction() + GetCombatStatus();
    }

    public string UseSkill(string skillName)
    {
        if (!IsInCombat)
        {
            return "You're not in combat.";
        }

        // Check if player is stunned
        if (HasStatusEffect(_playerStatuses, StatusEffect.Stunned))
        {
            RemoveStatusEffect(_playerStatuses, StatusEffect.Stunned);
            return "You're stunned and cannot act this turn!" + ProcessEnemyAction();
        }

        string result = skillName.ToLower() switch
        {
            "heavy" => HeavyAttack(),
            "defend" => DefendAction(),
            "heal" => HealAction(),
            _ => $"Unknown skill: {skillName}"
        };

        if (result.StartsWith("Unknown"))
        {
            return result;
        }

        return result + ProcessEnemyAction() + GetCombatStatus();
    }

    private string HeavyAttack()
    {
        // Heavy attack - 50% more damage
        int damage = (int)(_player.AttackPower * 1.5f);
        _currentEnemy.Health -= damage;

        if (_currentEnemy.Health <= 0)
        {
            return $"You perform a devastating heavy attack for {damage} damage!\n" + ProcessEnemyDefeat();
        }

        return $"You perform a heavy attack for {damage} damage!";
    }

    private string DefendAction()
    {
        // Add shield status for next turn
        AddStatusEffect(_playerStatuses, new CombatStatus(StatusEffect.Shielded, 1, 10));
        return "You raise your guard! (Reduces next incoming damage by 10)";
    }

    private string HealAction()
    {
        // Simple heal
        int healAmount = 15;
        _player.Health = Math.Min(_player.MaxHealth, _player.Health + healAmount);
        return $"You focus and recover {healAmount} health!";
    }

    protected override string ProcessEnemyAction()
    {
        if (_currentEnemy == null || _currentEnemy.Health <= 0)
            return "";

        // Check if enemy is stunned
        if (HasStatusEffect(_enemyStatuses, StatusEffect.Stunned))
        {
            RemoveStatusEffect(_enemyStatuses, StatusEffect.Stunned);
            return "\n\nThe enemy is stunned and cannot act!";
        }

        // Simple enemy AI - just attack with occasional special
        if (_random.NextDouble() < 0.7)
        {
            return "\n\n" + ProcessEnemyAttack();
        }
        else
        {
            return "\n\n" + ProcessEnemySpecialAttack();
        }
    }

    private string ProcessEnemyAttack()
    {
        // Calculate damage
        int baseDamage = _currentEnemy.AttackPower;

        // Apply player stance defense
        float defenseMultiplier = _playerStance switch
        {
            CombatStance.Defensive => 1.3f,
            CombatStance.Aggressive => 0.85f,
            _ => 1.0f
        };

        int playerDefense = (int)(_player.Defense * defenseMultiplier);

        // Check for shields
        var shield = _playerStatuses.FirstOrDefault(s => s.Effect == StatusEffect.Shielded);
        if (shield != null)
        {
            baseDamage -= shield.Strength;
            _playerStatuses.Remove(shield);
            if (baseDamage <= 0)
            {
                return "The enemy's attack is completely blocked by your shield!";
            }
        }

        int finalDamage = Math.Max(1, baseDamage - playerDefense);
        _player.Health -= finalDamage;

        return $"The {_currentEnemy.Name} attacks you for {finalDamage} damage!";
    }

    private string ProcessEnemySpecialAttack()
    {
        // Simple special attack
        var (description, damage) = _currentEnemy.SpecialAttack();
        damage = Math.Max(1, damage - _player.Defense);
        _player.Health -= damage;

        // 30% chance to apply poison
        if (_random.NextDouble() < 0.3)
        {
            AddStatusEffect(_playerStatuses, new CombatStatus(StatusEffect.Poisoned, 3, 3));
            return $"{description} You take {damage} damage and are poisoned!";
        }

        return $"{description} You take {damage} damage!";
    }

    private void AddStatusEffect(List<CombatStatus> statusList, CombatStatus status)
    {
        // Check if effect already exists
        var existing = statusList.FirstOrDefault(s => s.Effect == status.Effect);
        if (existing != null)
        {
            existing.Duration = Math.Max(existing.Duration, status.Duration);
        }
        else
        {
            statusList.Add(status);
        }
    }

    private bool HasStatusEffect(List<CombatStatus> statusList, StatusEffect effect)
    {
        return statusList.Any(s => s.Effect == effect && s.Duration > 0);
    }

    private void RemoveStatusEffect(List<CombatStatus> statusList, StatusEffect effect)
    {
        statusList.RemoveAll(s => s.Effect == effect);
    }

    private string ProcessStatusEffects()
    {
        string result = "";

        // Process player status effects
        for (int i = _playerStatuses.Count - 1; i >= 0; i--)
        {
            var status = _playerStatuses[i];

            switch (status.Effect)
            {
                case StatusEffect.Poisoned:
                    _player.Health -= status.Strength;
                    result += $"\nYou take {status.Strength} poison damage!";
                    break;
            }

            status.Duration--;
            if (status.Duration <= 0)
            {
                _playerStatuses.RemoveAt(i);
                if (status.Effect != StatusEffect.Shielded) // Don't announce shield expiry
                {
                    result += $"\n{status.Effect} effect wears off.";
                }
            }
        }

        // Process enemy status effects
        for (int i = _enemyStatuses.Count - 1; i >= 0; i--)
        {
            var status = _enemyStatuses[i];

            switch (status.Effect)
            {
                case StatusEffect.Poisoned:
                    _currentEnemy.Health -= status.Strength;
                    result += $"\nThe {_currentEnemy.Name} takes {status.Strength} poison damage!";
                    break;
            }

            status.Duration--;
            if (status.Duration <= 0)
            {
                _enemyStatuses.RemoveAt(i);
            }
        }

        return result;
    }

    private string GetCombatStatus()
    {
        string status = $"\n\n=== Combat Status ===";
        status += $"\nYou: {_player.Health}/{_player.MaxHealth} HP | Stance: {_playerStance}";

        if (_playerStatuses.Count > 0)
        {
            status += " | Effects: " + string.Join(", ", _playerStatuses.Select(s => s.Effect.ToString()));
        }

        status += $"\n{_currentEnemy.Name}: {_currentEnemy.Health}/{_currentEnemy.MaxHealth} HP";

        status += "\n\nActions: attack, heavy, defend, heal, stance [aggressive/defensive/balanced], flee";

        return status;
    }

    public string ApplyStatusEffect(string target, StatusEffect effect, int duration, int strength = 1)
    {
        if (!IsInCombat)
        {
            return "You're not in combat.";
        }

        var status = new CombatStatus(effect, duration, strength);

        if (target.ToLower() == "self" || target.ToLower() == "player")
        {
            AddStatusEffect(_playerStatuses, status);
            return $"You are now {effect}!";
        }
        else
        {
            AddStatusEffect(_enemyStatuses, status);
            return $"The {_currentEnemy.Name} is now {effect}!";
        }
    }

    // Override the base ProcessEnemyDefeat to handle enhanced features
    protected override string ProcessEnemyDefeat()
    {
        // Reset combat-specific states
        _playerStatuses.Clear();
        _enemyStatuses.Clear();
        _playerStance = CombatStance.Balanced;

        // Call base implementation
        return base.ProcessEnemyDefeat();
    }

    // Add method to get current stance for save system
    public CombatStance GetCurrentStance()
    {
        return _playerStance;
    }

    // Add method to get status effects for save system
    public List<CombatStatus> GetPlayerStatusEffects()
    {
        return new List<CombatStatus>(_playerStatuses);
    }

    public List<CombatStatus> GetEnemyStatusEffects()
    {
        return new List<CombatStatus>(_enemyStatuses);
    }

    // Add method to restore status effects from save
    public void RestoreStatusEffects(List<CombatStatus> playerStatuses, List<CombatStatus> enemyStatuses)
    {
        _playerStatuses = new List<CombatStatus>(playerStatuses);
        _enemyStatuses = new List<CombatStatus>(enemyStatuses);
    }
}