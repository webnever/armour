using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Represents an item that can be picked up or interacted with
public class Item
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool CanTake { get; set; } = true;
    public bool CanDrop { get; set; } = true;
    public float Weight { get; set; } = 0.0f;
    public string Category { get; set; } = "Miscellaneous";
    public ItemType ItemType { get; set; } = ItemType.Miscellaneous;
    public int UseValue { get; set; } = 0;
    public bool IsStackable { get; set; } = false;
    public int StackCount { get; set; } = 1;
    
    // Additional properties for enhanced functionality
    public Dictionary<string, Func<string>> UseTargets { get; set; } = new Dictionary<string, Func<string>>();
    public Func<string> DefaultUseAction { get; set; }
    public List<string> Tags { get; set; } = new List<string>();
    
    // Location transition properties
    public Dictionary<string, ItemTransition> LocationTransitions { get; set; } = new Dictionary<string, ItemTransition>();
    public ItemTransition DefaultTransition { get; set; }
    
    // Create a copy of this item
    public virtual Item Clone()
    {
        return new Item
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            CanTake = this.CanTake,
            CanDrop = this.CanDrop,
            Weight = this.Weight,
            Category = this.Category,
            ItemType = this.ItemType,
            UseValue = this.UseValue,
            IsStackable = this.IsStackable,
            StackCount = this.StackCount,
            UseTargets = new Dictionary<string, Func<string>>(this.UseTargets),
            DefaultUseAction = this.DefaultUseAction,
            Tags = new List<string>(this.Tags),
            LocationTransitions = new Dictionary<string, ItemTransition>(this.LocationTransitions),
            DefaultTransition = this.DefaultTransition
        };
    }
}

// Container class extending Item
public class Container : Item
{
    public List<Item> Items { get; set; } = new List<Item>();
    public bool IsOpen { get; set; } = false;
    public float Capacity { get; set; } = 20.0f; // Maximum weight capacity

    public Container()
    {
        ItemType = ItemType.Container;
        CanTake = false; // Most containers aren't portable by default
    }

    public float GetCurrentWeight()
    {
        return Items.Sum(item => item.Weight);
    }

    public bool CanAddItem(Item item)
    {
        return GetCurrentWeight() + item.Weight <= Capacity;
    }

    public string GetContentsDescription()
    {
        if (!IsOpen)
        {
            return $"The {Name} is closed.";
        }

        if (Items.Count == 0)
        {
            return $"The {Name} is empty.";
        }

        string description = $"The {Name} contains:";
        foreach (var item in Items)
        {
            description += $"\n- {item.Name}";
        }

        return description;
    }
    
    public override Item Clone()
    {
        // Create a new container
        var clone = new Container
        {
            Id = this.Id,
            Name = this.Name,
            Description = this.Description,
            CanTake = this.CanTake,
            CanDrop = this.CanDrop,
            Weight = this.Weight,
            Category = this.Category,
            ItemType = this.ItemType,
            UseValue = this.UseValue,
            IsStackable = this.IsStackable,
            StackCount = this.StackCount,
            DefaultUseAction = this.DefaultUseAction,
            Tags = new List<string>(this.Tags),
            IsOpen = this.IsOpen,
            Capacity = this.Capacity
        };
        
        // Copy UseTargets if any
        if (this.UseTargets != null)
        {
            foreach (var target in this.UseTargets)
            {
                clone.UseTargets[target.Key] = target.Value;
            }
        }
        
        // Clone items inside the container
        foreach (var item in this.Items)
        {
            clone.Items.Add(item.Clone());
        }
        
        return clone;
    }
}

// New class for handling item-triggered location transitions
public class ItemTransition
{
    public string TargetLocationId { get; set; }
    public string TransitionMessage { get; set; } = "";
    public bool ConsumeItem { get; set; } = false;
    public string RequiredLocationId { get; set; } = ""; // If specified, only works in this location
    public List<string> RequiredLocationIds { get; set; } = new List<string>(); // If specified, only works in these locations
    public Dictionary<string, bool> RequiredFlags { get; set; } = new Dictionary<string, bool>();
    
    public bool CanTransition(string currentLocationId, Dictionary<string, bool> gameFlags)
    {
        // Check if we're in the required location (single location check for backwards compatibility)
        if (!string.IsNullOrEmpty(RequiredLocationId) && RequiredLocationId != currentLocationId)
        {
            return false;
        }
        
        // Check if we're in one of the required locations (multiple locations)
        if (RequiredLocationIds.Count > 0 && !RequiredLocationIds.Contains(currentLocationId))
        {
            return false;
        }
        
        // Check required flags
        foreach (var flag in RequiredFlags)
        {
            if (!gameFlags.ContainsKey(flag.Key) || gameFlags[flag.Key] != flag.Value)
            {
                return false;
            }
        }
        
        return true;
    }
}