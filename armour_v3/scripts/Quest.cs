using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Represents a quest/mission
public class Quest
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; }
    public bool IsCompleted { get; set; }
    public List<QuestObjective> Objectives { get; set; } = new List<QuestObjective>();
    public List<QuestTrigger> Triggers { get; set; } = new List<QuestTrigger>();
    public Action<GameState> OnCompleted { get; set; }
    
    public void CheckObjectives()
    {
        if (IsCompleted) return;
        
        if (Objectives.All(o => o.IsCompleted))
        {
            IsCompleted = true;
            OnCompleted?.Invoke(null);
        }
    }
    
    public void CheckLocationTrigger(string locationId)
    {
        foreach (var trigger in Triggers)
        {
            if (trigger.Type == TriggerType.Enter && trigger.TargetId == locationId)
            {
                foreach (var objectiveId in trigger.ObjectiveIds)
                {
                    var objective = Objectives.FirstOrDefault(o => o.Id == objectiveId);
                    if (objective != null)
                    {
                        objective.IsCompleted = true;
                    }
                }
                
                CheckObjectives();
            }
        }
    }
    
    public void CheckItemTrigger(string itemId, TriggerType triggerType, string targetId = null)
    {
        foreach (var trigger in Triggers)
        {
            bool match = trigger.Type == triggerType && trigger.TargetId == itemId;
            
            // For UseOn triggers, also check the target
            if (triggerType == TriggerType.UseOn && match && !string.IsNullOrEmpty(targetId))
            {
                match = trigger.SecondaryTargetId == targetId;
            }
            
            if (match)
            {
                foreach (var objectiveId in trigger.ObjectiveIds)
                {
                    var objective = Objectives.FirstOrDefault(o => o.Id == objectiveId);
                    if (objective != null)
                    {
                        objective.IsCompleted = true;
                    }
                }
                
                CheckObjectives();
            }
        }
    }
}

// Represents a quest objective
public class QuestObjective
{
    public string Id { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; }
}

// Represents a trigger that can complete quest objectives
public class QuestTrigger
{
    public TriggerType Type { get; set; }
    public string TargetId { get; set; }
    public string SecondaryTargetId { get; set; }
    public List<string> ObjectiveIds { get; set; } = new List<string>();
}