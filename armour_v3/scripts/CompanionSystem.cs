using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CompanionSystem
{
    private GameState _gameState;
    private List<Companion> _companions;
    private List<Companion> _activeCompanions;
    private int _maxActiveCompanions = 2;
    private Random _random = new Random();
    
    public CompanionSystem(GameState gameState)
    {
        _gameState = gameState;
        _companions = new List<Companion>();
        _activeCompanions = new List<Companion>();
        
        InitializeCompanions();
    }
    
    private void InitializeCompanions()
    {
        // Create potential companions
        
        // VAL - Combat specialist
        _companions.Add(new Companion
        {
            Id = "val_companion",
            Name = "VAL",
            Description = "Interdimensional operative with combat expertise",
            BaseHealth = 80,
            BaseAttack = 15,
            BaseDefense = 8,
            Personality = CompanionPersonality.Aggressive,
            SpecialAbilities = new List<CompanionAbility>
            {
                new CompanionAbility
                {
                    Id = "dimensional_strike",
                    Name = "Dimensional Strike",
                    Description = "Attack from multiple dimensions",
                    Cooldown = 3,
                    Effect = (combat) => {
                        int damage = 25;
                        return $"VAL phases through dimensions and strikes for {damage} damage!";
                    }
                },
                new CompanionAbility
                {
                    Id = "reality_shield",
                    Name = "Reality Shield",
                    Description = "Create a protective barrier",
                    Cooldown = 5,
                    Effect = (combat) => {
                        // Add shield status
                        return "VAL creates a reality-warping shield around the party!";
                    }
                }
            },
            DialoguePool = new List<CompanionDialogue>
            {
                new CompanionDialogue
                {
                    Trigger = DialogueTrigger.Combat,
                    Lines = new[] { 
                        "Another dimension, another fight.",
                        "Reality bleeds, but we persist.",
                        "I've faced worse in the void between worlds."
                    }
                },
                new CompanionDialogue
                {
                    Trigger = DialogueTrigger.LowHealth,
                    Lines = new[] {
                        "MXI, you're bleeding across dimensions!",
                        "Don't you dare die on me. Not here.",
                        "I've seen too many fall. Not you."
                    }
                }
            }
        });
        
        // MAZE - Tech specialist
        _companions.Add(new Companion
        {
            Id = "maze_companion",
            Name = "MAZE",
            Description = "Neural interface specialist with hacking abilities",
            BaseHealth = 60,
            BaseAttack = 8,
            BaseDefense = 5,
            Personality = CompanionPersonality.Analytical,
            SpecialAbilities = new List<CompanionAbility>
            {
                new CompanionAbility
                {
                    Id = "neural_hack",
                    Name = "Neural Hack",
                    Description = "Confuse an enemy's neural patterns",
                    Cooldown = 4,
                    Effect = (combat) => {
                        // Apply confusion status
                        return "MAZE scrambles the enemy's neural pathways!";
                    }
                },
                new CompanionAbility
                {
                    Id = "data_mine",
                    Name = "Data Mine",
                    Description = "Extract information from digital entities",
                    Cooldown = 6,
                    Effect = (combat) => {
                        return "MAZE extracts valuable data from the enemy's consciousness!";
                    }
                }
            },
            DialoguePool = new List<CompanionDialogue>
            {
                new CompanionDialogue
                {
                    Trigger = DialogueTrigger.Discovery,
                    Lines = new[] {
                        "Fascinating. The data patterns here are... alive.",
                        "I'm detecting consciousness fragments in the architecture.",
                        "This technology predates our understanding."
                    }
                },
                new CompanionDialogue
                {
                    Trigger = DialogueTrigger.Puzzle,
                    Lines = new[] {
                        "Let me interface with this. The solution is in the code.",
                        "Every puzzle has a pattern. Every pattern has a key.",
                        "The HIJACK resonates with this frequency. Try using it."
                    }
                }
            }
        });
        
        // GHOST - Stealth specialist
        _companions.Add(new Companion
        {
            Id = "ghost_companion",
            Name = "GHOST",
            Description = "Quantum reconnaissance expert",
            BaseHealth = 50,
            BaseAttack = 12,
            BaseDefense = 3,
            Personality = CompanionPersonality.Cautious,
            SpecialAbilities = new List<CompanionAbility>
            {
                new CompanionAbility
                {
                    Id = "phase_cloak",
                    Name = "Phase Cloak",
                    Description = "Make the party harder to detect",
                    Cooldown = 5,
                    Effect = (combat) => {
                        return "GHOST phases the party partially out of reality!";
                    }
                },
                new CompanionAbility
                {
                    Id = "quantum_recon",
                    Name = "Quantum Recon",
                    Description = "Scout ahead through quantum states",
                    Cooldown = 8,
                    Effect = (combat) => {
                        return "GHOST scouts possible futures and reveals enemy weaknesses!";
                    }
                }
            }
        });
        
        // Shadow Children - Unique companion
        _companions.Add(new Companion
        {
            Id = "shadow_children",
            Name = "The Lost Innocents",
            Description = "ETA-C victims who escaped digital imprisonment",
            BaseHealth = 40,
            BaseAttack = 5,
            BaseDefense = 10,
            Personality = CompanionPersonality.Mysterious,
            SpecialAbilities = new List<CompanionAbility>
            {
                new CompanionAbility
                {
                    Id = "digital_scream",
                    Name = "Digital Scream",
                    Description = "Emit a haunting cry that damages all enemies",
                    Cooldown = 6,
                    Effect = (combat) => {
                        return "The children's digital screams tear at reality itself!";
                    }
                },
                new CompanionAbility
                {
                    Id = "innocence_shield",
                    Name = "Shield of Innocence",
                    Description = "Their pure souls protect the party",
                    Cooldown = 10,
                    Effect = (combat) => {
                        return "The children's innocence forms a protective barrier!";
                    }
                }
            },
            DialoguePool = new List<CompanionDialogue>
            {
                new CompanionDialogue
                {
                    Trigger = DialogueTrigger.Idle,
                    Lines = new[] {
                        "We remember... before the needles...",
                        "The machines promised forever... but forever hurts...",
                        "You're different... you still have warmth..."
                    }
                }
            },
            RecruitRequirements = new CompanionRequirements
            {
                RequiredQuest = "save_the_children",
                RequiredItem = "digital_key",
                SpecialCondition = "Show mercy to all shadow entities"
            }
        });
    }
    
    public string RecruitCompanion(string companionId)
    {
        var companion = _companions.FirstOrDefault(c => c.Id == companionId);
        
        if (companion == null)
        {
            return "No such companion exists.";
        }
        
        if (companion.IsRecruited)
        {
            return $"{companion.Name} has already joined your party.";
        }
        
        // Check requirements
        if (companion.RecruitRequirements != null)
        {
            var requirements = companion.RecruitRequirements;
            
            if (!string.IsNullOrEmpty(requirements.RequiredQuest))
            {
                // Check quest completion - fix method name
                var completedQuests = _gameState.GetCompletedQuestIds();
                if (!completedQuests.Contains(requirements.RequiredQuest))
                {
                    return $"You must complete the quest '{requirements.RequiredQuest}' first.";
                }
            }
            
            if (!string.IsNullOrEmpty(requirements.RequiredItem))
            {
                if (!_gameState.GetPlayer().HasItem(requirements.RequiredItem))
                {
                    return $"You need the {requirements.RequiredItem} to recruit {companion.Name}.";
                }
            }
            
            if (requirements.MinimumLevel > 0)
            {
                if (_gameState.GetPlayer().Level < requirements.MinimumLevel)
                {
                    return $"You must be at least level {requirements.MinimumLevel}.";
                }
            }
            
            if (!string.IsNullOrEmpty(requirements.SpecialCondition))
            {
                // Check special conditions through game flags
                if (!_gameState.CheckSpecialCondition(requirements.SpecialCondition))
                {
                    return $"Special requirement not met: {requirements.SpecialCondition}";
                }
            }
        }
        
        // Recruit the companion
        companion.IsRecruited = true;
        companion.CurrentHealth = companion.BaseHealth;
        companion.Loyalty = 50; // Start at neutral
        
        // Add to active if space available
        if (_activeCompanions.Count < _maxActiveCompanions)
        {
            _activeCompanions.Add(companion);
            companion.IsActive = true;
        }
        
        // Recruitment dialogue
        string recruitMessage = companion.Name switch
        {
            "VAL" => "VAL nods grimly. 'I've seen what's coming. We'll face it together.'",
            "MAZE" => "MAZE's neural ports glow. 'Your HIJACK frequencies... fascinating. I'm in.'",
            "GHOST" => "GHOST materializes fully. 'I exist between certainties. Perhaps together we'll find truth.'",
            "The Lost Innocents" => "The shadow children whisper in unison: 'We remember warmth... we'll follow...'",
            _ => $"{companion.Name} joins your party!"
        };
        
        return recruitMessage;
    }
    
    public string DismissCompanion(string companionName)
    {
        var companion = _activeCompanions.FirstOrDefault(c => 
            c.Name.Equals(companionName, StringComparison.OrdinalIgnoreCase));
            
        if (companion == null)
        {
            return $"{companionName} is not in your active party.";
        }
        
        _activeCompanions.Remove(companion);
        companion.IsActive = false;
        
        // Loyalty penalty for dismissal
        companion.Loyalty -= 10;
        
        return $"{companion.Name} leaves the party.";
    }
    
    public string SetActiveCompanion(string companionName)
    {
        var companion = _companions.FirstOrDefault(c => 
            c.Name.Equals(companionName, StringComparison.OrdinalIgnoreCase) && 
            c.IsRecruited);
            
        if (companion == null)
        {
            return "You haven't recruited this companion yet.";
        }
        
        if (companion.IsActive)
        {
            return $"{companion.Name} is already in your active party.";
        }
        
        if (_activeCompanions.Count >= _maxActiveCompanions)
        {
            return $"Your party is full. Dismiss someone first (max {_maxActiveCompanions} companions).";
        }
        
        _activeCompanions.Add(companion);
        companion.IsActive = true;
        
        return $"{companion.Name} joins your active party.";
    }
    
    public void ProcessCombatTurn(EnhancedCombatSystem combat)
    {
        foreach (var companion in _activeCompanions)
        {
            if (companion.CurrentHealth <= 0)
                continue;
                
            // Companion AI
            var action = DetermineCompanionAction(companion, combat);
            ExecuteCompanionAction(companion, action, combat);
            
            // Trigger combat dialogue occasionally
            if (_random.NextDouble() < 0.2)
            {
                var dialogue = GetCompanionDialogue(companion, DialogueTrigger.Combat);
                if (!string.IsNullOrEmpty(dialogue))
                {
                    _gameState.AddMessage($"{companion.Name}: \"{dialogue}\"");
                }
            }
        }
    }
    
    private CompanionAction DetermineCompanionAction(Companion companion, EnhancedCombatSystem combat)
    {
        // AI based on personality
        switch (companion.Personality)
        {
            case CompanionPersonality.Aggressive:
                // Prefer attacking
                if (_random.NextDouble() < 0.7)
                    return CompanionAction.Attack;
                break;
                
            case CompanionPersonality.Cautious:
                // Check health first
                if (companion.CurrentHealth < companion.BaseHealth * 0.3)
                    return CompanionAction.Defend;
                break;
                
            case CompanionPersonality.Supportive:
                // Check player health
                if (_gameState.GetPlayer().Health < _gameState.GetPlayer().MaxHealth * 0.5)
                    return CompanionAction.Support;
                break;
        }
        
        // Check for special ability use
        foreach (var ability in companion.SpecialAbilities)
        {
            if (ability.CurrentCooldown <= 0 && _random.NextDouble() < 0.3)
            {
                return CompanionAction.UseAbility;
            }
        }
        
        return CompanionAction.Attack;
    }
    
    private void ExecuteCompanionAction(Companion companion, CompanionAction action, EnhancedCombatSystem combat)
    {
        switch (action)
        {
            case CompanionAction.Attack:
                int damage = companion.BaseAttack + _random.Next(-2, 3);
                var enemy = combat.GetCurrentEnemy();
                if (enemy != null)
                {
                    enemy.Health -= damage;
                    _gameState.AddMessage($"{companion.Name} attacks {enemy.Name} for {damage} damage!");
                }
                break;
                
            case CompanionAction.Defend:
                companion.IsDefending = true;
                _gameState.AddMessage($"{companion.Name} takes a defensive stance!");
                break;
                
            case CompanionAction.UseAbility:
                // Use first available ability
                var ability = companion.SpecialAbilities.FirstOrDefault(a => a.CurrentCooldown <= 0);
                if (ability != null)
                {
                    string effect = ability.Effect(combat);
                    _gameState.AddMessage(effect);
                    ability.CurrentCooldown = ability.Cooldown;
                }
                break;
                
            case CompanionAction.Support:
                // Heal or buff player
                int healAmount = 10;
                var player = _gameState.GetPlayer();
                player.Health = Math.Min(player.MaxHealth, player.Health + healAmount);
                _gameState.AddMessage($"{companion.Name} supports you, restoring {healAmount} health!");
                break;
        }
        
        // Reduce cooldowns
        foreach (var ability in companion.SpecialAbilities)
        {
            if (ability.CurrentCooldown > 0)
                ability.CurrentCooldown--;
        }
    }
    
    public string GetCompanionDialogue(Companion companion, DialogueTrigger trigger)
    {
        var dialogueSet = companion.DialoguePool.FirstOrDefault(d => d.Trigger == trigger);
        
        if (dialogueSet != null && dialogueSet.Lines.Length > 0)
        {
            return dialogueSet.Lines[_random.Next(dialogueSet.Lines.Length)];
        }
        
        return null;
    }
    
    public void UpdateCompanionLoyalty(string companionId, int change, string reason = "")
    {
        var companion = _companions.FirstOrDefault(c => c.Id == companionId);
        
        if (companion != null && companion.IsRecruited)
        {
            companion.Loyalty = Math.Clamp(companion.Loyalty + change, 0, 100);
            
            if (change > 0)
            {
                _gameState.AddMessage($"{companion.Name}'s loyalty increases! {reason}");
            }
            else if (change < 0)
            {
                _gameState.AddMessage($"{companion.Name}'s loyalty decreases. {reason}");
            }
            
            // Check loyalty thresholds
            if (companion.Loyalty >= 80 && !companion.HasHighLoyaltyBonus)
            {
                companion.HasHighLoyaltyBonus = true;
                companion.BaseAttack += 5;
                companion.BaseDefense += 3;
                _gameState.AddMessage($"{companion.Name} fights with renewed vigor! (High loyalty bonus)");
            }
            else if (companion.Loyalty <= 20)
            {
                // Low loyalty consequences
                if (_random.NextDouble() < 0.1)
                {
                    _gameState.AddMessage($"{companion.Name} seems reluctant to follow orders...");
                }
            }
        }
    }
    
    public List<Companion> GetActiveCompanions()
    {
        return _activeCompanions;
    }
    
    public string GetCompanionStatus()
    {
        if (_activeCompanions.Count == 0)
        {
            return "You have no active companions.";
        }
        
        string status = "=== Active Companions ===\n";
        
        foreach (var companion in _activeCompanions)
        {
            status += $"\n{companion.Name}";
            status += $"\n  Health: {companion.CurrentHealth}/{companion.BaseHealth}";
            status += $"\n  Loyalty: {GetLoyaltyDescription(companion.Loyalty)}";
            status += $"\n  Attack: {companion.BaseAttack} | Defense: {companion.BaseDefense}";
            
            if (companion.SpecialAbilities.Any(a => a.CurrentCooldown > 0))
            {
                status += "\n  Abilities on cooldown:";
                foreach (var ability in companion.SpecialAbilities.Where(a => a.CurrentCooldown > 0))
                {
                    status += $"\n    - {ability.Name} ({ability.CurrentCooldown} turns)";
                }
            }
            
            status += "\n";
        }
        
        // Show recruited but inactive
        var inactive = _companions.Where(c => c.IsRecruited && !c.IsActive).ToList();
        if (inactive.Any())
        {
            status += "\n=== Inactive Companions ===\n";
            foreach (var companion in inactive)
            {
                status += $"  - {companion.Name}\n";
            }
        }
        
        return status;
    }
    
    private string GetLoyaltyDescription(int loyalty)
    {
        return loyalty switch
        {
            >= 80 => "Devoted",
            >= 60 => "Loyal",
            >= 40 => "Neutral",
            >= 20 => "Wary",
            _ => "Hostile"
        };
    }
    
    public void ProcessCompanionInteractions()
    {
        // Companions may interact with each other
        if (_activeCompanions.Count >= 2)
        {
            if (_random.NextDouble() < 0.05) // 5% chance per update
            {
                var comp1 = _activeCompanions[_random.Next(_activeCompanions.Count)];
                var comp2 = _activeCompanions.FirstOrDefault(c => c != comp1);
                
                if (comp2 != null)
                {
                    GenerateCompanionBanter(comp1, comp2);
                }
            }
        }
    }
    
    private void GenerateCompanionBanter(Companion comp1, Companion comp2)
    {
        // Generate contextual banter between companions
        string banter = (comp1.Name, comp2.Name) switch
        {
            ("VAL", "MAZE") => "VAL: 'Your tech won't save you when reality collapses.'\nMAZE: 'But it might help us understand why it's collapsing.'",
            ("MAZE", "GHOST") => "MAZE: 'Your quantum signature is fascinating.'\nGHOST: 'I exist in states you cannot measure.'",
            ("VAL", "The Lost Innocents") => "VAL: 'I've seen what they did to you...'\nThe Lost Innocents: 'We remember... we remember everything...'",
            _ => null
        };
        
        if (!string.IsNullOrEmpty(banter))
        {
            _gameState.AddMessage($"\n{banter}\n");
        }
    }
}

public class Companion
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    
    public int BaseHealth { get; set; }
    public int CurrentHealth { get; set; }
    public int BaseAttack { get; set; }
    public int BaseDefense { get; set; }
    
    public bool IsRecruited { get; set; }
    public bool IsActive { get; set; }
    public bool IsDefending { get; set; }
    public bool HasHighLoyaltyBonus { get; set; }
    
    public int Loyalty { get; set; } // 0-100
    public CompanionPersonality Personality { get; set; }
    
    public List<CompanionAbility> SpecialAbilities { get; set; } = new List<CompanionAbility>();
    public List<CompanionDialogue> DialoguePool { get; set; } = new List<CompanionDialogue>();
    public CompanionRequirements RecruitRequirements { get; set; }
}

public class CompanionAbility
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Cooldown { get; set; }
    public int CurrentCooldown { get; set; }
    public Func<EnhancedCombatSystem, string> Effect { get; set; }
}

public class CompanionDialogue
{
    public DialogueTrigger Trigger { get; set; }
    public string[] Lines { get; set; }
}

public class CompanionRequirements
{
    public string RequiredQuest { get; set; }
    public string RequiredItem { get; set; }
    public int MinimumLevel { get; set; }
    public string SpecialCondition { get; set; }
}

public enum CompanionPersonality
{
    Aggressive,
    Cautious,
    Supportive,
    Analytical,
    Mysterious
}

public enum CompanionAction
{
    Attack,
    Defend,
    UseAbility,
    Support,
    Flee
}

public enum DialogueTrigger
{
    Combat,
    LowHealth,
    Victory,
    Discovery,
    Puzzle,
    Idle,
    Death
}