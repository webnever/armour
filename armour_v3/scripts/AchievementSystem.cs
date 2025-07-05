using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

public class AchievementSystem
{
    private Dictionary<string, Achievement> _achievements;
    private Dictionary<string, float> _progress;
    private List<string> _unlockedAchievements;
    private GameState _gameState;
    private Action<Achievement> _onAchievementUnlocked;
    
    public AchievementSystem(GameState gameState)
    {
        _gameState = gameState;
        _achievements = new Dictionary<string, Achievement>();
        _progress = new Dictionary<string, float>();
        _unlockedAchievements = new List<string>();
        
        InitializeAchievements();
    }
    
    private void InitializeAchievements()
    {
        // Initialize all achievements
        AddAchievement(new Achievement
        {
            Id = "first_blood",
            Name = "First Blood",
            Description = "Win your first combat",
            Points = 10,
            IsProgress = false
        });
        
        AddAchievement(new Achievement
        {
            Id = "flawless_victory",
            Name = "Flawless Victory",
            Description = "Win a combat without taking damage",
            Points = 20,
            IsProgress = false
        });
        
        AddAchievement(new Achievement
        {
            Id = "combo_master",
            Name = "Combo Master",
            Description = "Achieve a 3-hit combo",
            Points = 15,
            IsProgress = false
        });
        
        AddAchievement(new Achievement
        {
            Id = "puzzle_solver",
            Name = "Puzzle Solver",
            Description = "Solve a puzzle on the first try",
            Points = 25,
            IsProgress = false
        });
        
        AddAchievement(new Achievement
        {
            Id = "collector",
            Name = "Collector",
            Description = "Collect 10 items",
            Points = 30,
            IsProgress = true,
            TargetValue = 10
        });
        
        AddAchievement(new Achievement
        {
            Id = "explorer",
            Name = "Explorer",
            Description = "Discover 5 locations",
            Points = 20,
            IsProgress = true,
            TargetValue = 5
        });
        
        AddAchievement(new Achievement
        {
            Id = "socializer",
            Name = "Socializer",
            Description = "Talk to 3 different characters",
            Points = 15,
            IsProgress = true,
            TargetValue = 3
        });
        
        AddAchievement(new Achievement
        {
            Id = "speedrunner",
            Name = "Speedrunner",
            Description = "Complete the game in under an hour",
            Points = 50,
            IsProgress = false,
            IsHidden = true
        });
        
        AddAchievement(new Achievement
        {
            Id = "completionist",
            Name = "Completionist",
            Description = "Unlock all achievements",
            Points = 100,
            IsProgress = false,
            IsHidden = true
        });
    }
    
    private void AddAchievement(Achievement achievement)
    {
        _achievements[achievement.Id] = achievement;
        
        if (achievement.IsProgress)
        {
            _progress[achievement.Id] = 0;
        }
    }
    
    public void TriggerAchievement(string achievementId)
    {
        if (!_achievements.ContainsKey(achievementId))
            return;
            
        if (_unlockedAchievements.Contains(achievementId))
            return;
        
        var achievement = _achievements[achievementId];
        
        if (achievement.IsProgress)
        {
            UpdateProgress(achievementId, achievement.TargetValue);
        }
        else
        {
            UnlockAchievement(achievement);
        }
    }
    
    public void UpdateProgress(string achievementId, float amount = 1)
    {
        if (!_achievements.ContainsKey(achievementId))
            return;
            
        if (_unlockedAchievements.Contains(achievementId))
            return;
        
        var achievement = _achievements[achievementId];
        
        if (!achievement.IsProgress)
        {
            TriggerAchievement(achievementId);
            return;
        }
        
        _progress[achievementId] += amount;
        
        if (_progress[achievementId] >= achievement.TargetValue)
        {
            UnlockAchievement(achievement);
        }
    }
    
    public void SetProgress(string achievementId, float value)
    {
        if (!_achievements.ContainsKey(achievementId))
            return;
            
        if (_unlockedAchievements.Contains(achievementId))
            return;
        
        var achievement = _achievements[achievementId];
        
        if (!achievement.IsProgress)
            return;
        
        _progress[achievementId] = value;
        
        if (_progress[achievementId] >= achievement.TargetValue)
        {
            UnlockAchievement(achievement);
        }
    }
    
    private void UnlockAchievement(Achievement achievement)
    {
        _unlockedAchievements.Add(achievement.Id);
        GD.Print($"Achievement Unlocked: {achievement.Name} - {achievement.Description}");
        
        _onAchievementUnlocked?.Invoke(achievement);
        
        CheckCompletionist();
    }
    
    private void CheckCompletionist()
    {
        if (_unlockedAchievements.Contains("completionist"))
            return;
        
        int totalAchievements = _achievements.Count - 1; // Exclude completionist itself
        int unlockedCount = _unlockedAchievements.Count;
        
        if (unlockedCount >= totalAchievements)
        {
            TriggerAchievement("completionist");
        }
    }
    
    public void CheckLocationAchievements()
    {
        var discoveredCount = _gameState.GetDiscoveredLocations().Count;
        SetProgress("explorer", discoveredCount);
    }
    
    public void CheckCombatAchievements(CombatResult result)
    {
        if (result.Victory)
        {
            if (!_unlockedAchievements.Contains("first_blood"))
            {
                TriggerAchievement("first_blood");
            }
            
            if (result.DamageTaken == 0)
            {
                TriggerAchievement("flawless_victory");
            }
            
            if (result.ComboCount >= 3)
            {
                TriggerAchievement("combo_master");
            }
        }
    }
    
    public void CheckPuzzleAchievements(bool firstTry)
    {
        if (firstTry)
        {
            TriggerAchievement("puzzle_solver");
        }
    }
    
    public void CheckItemAchievements()
    {
        var itemCount = _gameState.GetTotalItemsCollected();
        SetProgress("collector", itemCount);
    }
    
    public void CheckExamineAchievements()
    {
        // Implementation for examine-based achievements
    }
    
    public void CheckConversationAchievements()
    {
        var talkedToCount = _gameState.GetTalkedToCharacters().Count;
        SetProgress("socializer", talkedToCount);
    }
    
    public void CheckSaveAchievements()
    {
        // Implementation for save-based achievements
    }
    
    public void CheckTimeAchievements(float totalPlayTime)
    {
        if (_gameState.IsGameCompleted() && totalPlayTime < 3600) // 1 hour
        {
            TriggerAchievement("speedrunner");
        }
    }
    
    // Add missing methods
    public Dictionary<string, float> GetProgress()
    {
        var allProgress = new Dictionary<string, float>();
        
        foreach (var achievement in _achievements.Values)
        {
            if (achievement.IsProgress)
            {
                allProgress[achievement.Id] = _progress.GetValueOrDefault(achievement.Id, 0f);
            }
            else
            {
                allProgress[achievement.Id] = _unlockedAchievements.Contains(achievement.Id) ? 1f : 0f;
            }
        }
        
        return allProgress;
    }
    
    public void LoadProgress(Dictionary<string, float> progress)
    {
        _progress.Clear();
        _unlockedAchievements.Clear();
        
        foreach (var kvp in progress)
        {
            if (_achievements.TryGetValue(kvp.Key, out var achievement))
            {
                if (achievement.IsProgress)
                {
                    _progress[kvp.Key] = kvp.Value;
                    
                    if (kvp.Value >= achievement.TargetValue)
                    {
                        _unlockedAchievements.Add(kvp.Key);
                    }
                }
                else
                {
                    if (kvp.Value >= 1f)
                    {
                        _unlockedAchievements.Add(kvp.Key);
                    }
                }
            }
        }
    }
}

public class Achievement
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; } = "";
    public int Points { get; set; }
    public bool IsHidden { get; set; } = false;
    public bool IsProgress { get; set; } = false;
    public float TargetValue { get; set; } = 1f;
}

public class AchievementSaveData
{
    public List<string> UnlockedAchievements { get; set; }
    public Dictionary<string, float> Progress { get; set; }
}

public class CombatResult
{
    public bool Victory { get; set; }
    public int DamageTaken { get; set; }
    public int DamageDealt { get; set; }
    public int ComboCount { get; set; }
    public float Duration { get; set; }
}