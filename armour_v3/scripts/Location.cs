using Godot;
using System;
using System.Collections.Generic;

// Represents a location in the game
public class Location
{
    // Basic properties
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string Region { get; set; } = "General";
    
    // Navigation
    public Dictionary<string, string> Exits { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, LockInfo> LockedExits { get; set; } = new Dictionary<string, LockInfo>();
    
    // Environment
    public Dictionary<string, string> Features { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> StateBasedFeatures { get; set; } = new Dictionary<string, string>();
    public Dictionary<string, string> TimeBasedDescriptions { get; set; } = new Dictionary<string, string>();
    
    // Contents
    public List<Item> Items { get; set; } = new List<Item>();
    public List<Character> Characters { get; set; } = new List<Character>();
    public List<Enemy> Enemies { get; set; } = new List<Enemy>();
    
    // Discovery and media
    public bool IsDiscovered { get; set; } = false;
    public string ImagePath { get; set; } = "";
    public string ScenePath { get; set; } = "";
    public string ImageDisplayMode { get; set; } = "default"; // "ascii", "regular", or "default"
    
    // Enhanced properties
    public bool HasImage => !string.IsNullOrEmpty(ImagePath);
    public bool HasScene => !string.IsNullOrEmpty(ScenePath);
    
    // Custom commands
    public Dictionary<string, string> CustomCommands { get; set; } = new Dictionary<string, string>();
    
    // Methods for content management
    public void AddItem(Item item)
    {
        if (item != null && !Items.Contains(item))
        {
            Items.Add(item);
        }
    }
    
    public bool RemoveItem(Item item)
    {
        return Items.Remove(item);
    }
    
    public Item GetItemById(string itemId)
    {
        return Items.Find(item => item.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase));
    }
    
    public void AddCharacter(Character character)
    {
        if (character != null && !Characters.Contains(character))
        {
            Characters.Add(character);
        }
    }
    
    public Character GetCharacterById(string characterId)
    {
        return Characters.Find(character => character.Id.Equals(characterId, StringComparison.OrdinalIgnoreCase));
    }
    
    // Get description based on current time or state
    public string GetCurrentDescription(string timeOfDay = "", string gameState = "")
    {
        // Check for time-based description first
        if (!string.IsNullOrEmpty(timeOfDay) && TimeBasedDescriptions.ContainsKey(timeOfDay.ToLower()))
        {
            return TimeBasedDescriptions[timeOfDay.ToLower()];
        }
        
        // Check for state-based description
        if (!string.IsNullOrEmpty(gameState) && StateBasedFeatures.ContainsKey(gameState))
        {
            return StateBasedFeatures[gameState];
        }
        
        // Return default description
        return Description;
    }
    
    // Check if an exit exists and is unlocked
    public bool CanExitTo(string direction)
    {
        string lowerDirection = direction.ToLower();
        
        // Check if exit exists
        if (!Exits.ContainsKey(lowerDirection))
        {
            return false;
        }
        
        // Check if exit is locked
        if (LockedExits.ContainsKey(lowerDirection))
        {
            return !LockedExits[lowerDirection].IsLocked;
        }
        
        return true;
    }
    
    // Get the destination location ID for a direction
    public string GetExitDestination(string direction)
    {
        string lowerDirection = direction.ToLower();
        return Exits.ContainsKey(lowerDirection) ? Exits[lowerDirection] : null;
    }
    
    // Get lock information for an exit
    public LockInfo GetLockInfo(string direction)
    {
        string lowerDirection = direction.ToLower();
        return LockedExits.ContainsKey(lowerDirection) ? LockedExits[lowerDirection] : null;
    }
}

// Helper class for locked exit information
public class LockInfo
{
    public bool IsLocked { get; set; } = true;
    public string KeyItem { get; set; } = "";
    public string LockedMessage { get; set; } = "This exit is locked.";
}