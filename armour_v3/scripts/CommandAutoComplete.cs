using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class CommandAutoComplete
{
    private GameState _gameState;
    private List<string> _commandHistory;
    private readonly int _maxHistorySize = 100;
    
    // All available commands - UPDATED: Removed minimap and puzzle references
    private readonly List<string> _baseCommands = new List<string>
    {
        "look", "l", "go", "examine", "move", "take", "drop", "inventory", "i", "inv", "use",
        "talk", "say", "attack", "flee", "save", "load", "saves", "help",
        "status", "quest", "stats", "equip", "unequip", "open", "close", "put", "take_from",
        "view", "clear", "exit",
        // Enhanced combat commands
        "stance", "heavy", "defend", "heal", "dodge", "taunt", "skill", "combo", "status_effect",
        // Secret command
        "crash"
    };

    public CommandAutoComplete(GameState gameState)
    {
        _gameState = gameState;
        _commandHistory = new List<string>();
    }

    public List<string> GetSuggestions(string partial)
    {
        if (string.IsNullOrWhiteSpace(partial))
            return new List<string>();

        partial = partial.ToLower().Trim();
        var suggestions = new List<string>();
        
        // Split input to check for multi-word commands
        var words = partial.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        if (words.Length == 0)
            return suggestions;

        // If only one word, suggest commands
        if (words.Length == 1)
        {
            suggestions.AddRange(GetCommandSuggestions(words[0]));
        }
        else
        {
            // Multi-word input - suggest based on command context
            string command = words[0];
            string argument = string.Join(" ", words.Skip(1));
            
            suggestions.AddRange(GetContextualSuggestions(command, argument));
        }
        
        // Remove duplicates and sort by relevance
        return suggestions.Distinct()
            .OrderBy(s => !s.StartsWith(partial)) // Exact prefix matches first
            .ThenBy(s => s.Length) // Shorter suggestions first
            .Take(10) // Limit suggestions
            .ToList();
    }

    private List<string> GetCommandSuggestions(string partial)
    {
        var suggestions = _baseCommands
            .Where(cmd => cmd.StartsWith(partial))
            .ToList();

        // NEW: Add custom location commands
        var currentLocation = _gameState.GetCurrentLocation();
        if (currentLocation?.CustomCommands != null)
        {
            suggestions.AddRange(currentLocation.CustomCommands.Keys
                .Where(cmd => cmd.StartsWith(partial)));
        }

        return suggestions;
    }

    private List<string> GetContextualSuggestions(string command, string partial)
    {
        var suggestions = new List<string>();

        switch (command.ToLower())
        {
            case "move":
                suggestions.AddRange(GetExitSuggestions(partial));
                break;
                
            case "take":
            case "get":
                // Check if it's "take from" pattern
                if (partial.Contains("from"))
                {
                    var parts = partial.Split(new[] { "from" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        suggestions.AddRange(GetContainerSuggestions(parts[1].Trim()));
                    }
                }
                else
                {
                    suggestions.AddRange(GetItemSuggestions(partial, false));
                }
                break;
                
            case "drop":
                suggestions.AddRange(GetInventoryItemSuggestions(partial));
                break;
                
            case "examine":
            case "look":
                suggestions.AddRange(GetExaminableSuggestions(partial));
                break;
                
            case "talk":
                suggestions.AddRange(GetCharacterSuggestions(partial));
                break;
                
            case "attack":
                suggestions.AddRange(GetEnemySuggestions(partial));
                break;
                
            case "use":
                suggestions.AddRange(GetUsableItemSuggestions(partial));
                break;
                
            case "equip":
                suggestions.AddRange(GetEquippableItemSuggestions(partial));
                break;
                
            case "unequip":
                suggestions.AddRange(new[] { "weapon", "armor" }.Where(s => s.StartsWith(partial)));
                break;
                
            case "open":
            case "close":
                suggestions.AddRange(GetContainerSuggestions(partial));
                break;
                
            case "put":
                // Handle "put [item] in [container]" pattern
                if (partial.Contains("in"))
                {
                    var parts = partial.Split(new[] { "in" }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length == 2)
                    {
                        suggestions.AddRange(GetContainerSuggestions(parts[1].Trim()));
                    }
                }
                else
                {
                    suggestions.AddRange(GetInventoryItemSuggestions(partial));
                }
                break;
                
            case "stance":
                suggestions.AddRange(new[] { "balanced", "aggressive", "defensive" }.Where(s => s.StartsWith(partial)));
                break;
                
            case "skill":
                suggestions.AddRange(new[] { "heavy", "defend", "heal", "dodge", "taunt" }.Where(s => s.StartsWith(partial)));
                break;
        }
        
        return suggestions;
    }

    private List<string> GetExitSuggestions(string partial)
    {
        var location = _gameState.GetCurrentLocation();
        if (location?.Exits == null) return new List<string>();
        
        return location.Exits.Keys
            .Where(exit => exit.StartsWith(partial))
            .ToList(); // FIXED: Return just the direction, not "go direction"
    }

    private List<string> GetItemSuggestions(string partial, bool fromInventory)
    {
        var items = fromInventory 
            ? _gameState.GetPlayer().Inventory 
            : _gameState.GetCurrentLocation()?.Items ?? new List<Item>();
            
        return items
            .Where(item => item.Name.ToLower().Contains(partial.ToLower()))
            .Select(item => item.Name.ToLower()) // FIXED: Return just the item name
            .ToList();
    }

    private List<string> GetInventoryItemSuggestions(string partial)
    {
        return _gameState.GetPlayer().Inventory
            .Where(item => item.Name.ToLower().Contains(partial.ToLower()))
            .Select(item => item.Name.ToLower()) // FIXED: Return just the item name
            .ToList();
    }

    private List<string> GetExaminableSuggestions(string partial)
    {
        var suggestions = new List<string>();
        var location = _gameState.GetCurrentLocation();
        
        if (location == null) return suggestions;
        
        // Items
        if (location.Items != null)
        {
            suggestions.AddRange(location.Items
                .Where(item => item.Name.ToLower().Contains(partial.ToLower()))
                .Select(item => item.Name.ToLower())); // FIXED: Return just the name
        }
            
        // Features
        if (location.Features != null)
        {
            suggestions.AddRange(location.Features.Keys
                .Where(feature => feature.Contains(partial.ToLower())));
        }
            
        // Characters
        if (location.Characters != null)
        {
            suggestions.AddRange(location.Characters
                .Where(ch => ch.Name.ToLower().Contains(partial.ToLower()))
                .Select(ch => ch.Name.ToLower()));
        }
            
        return suggestions;
    }

    private List<string> GetCharacterSuggestions(string partial)
    {
        var location = _gameState.GetCurrentLocation();
        if (location?.Characters == null) return new List<string>();
        
        return location.Characters
            .Where(ch => ch.Name.ToLower().Contains(partial.ToLower()))
            .Select(ch => ch.Name.ToLower()) // FIXED: Return just the name
            .ToList();
    }

    private List<string> GetEnemySuggestions(string partial)
    {
        var location = _gameState.GetCurrentLocation();
        var enemies = location?.Enemies ?? new List<Enemy>();
        return enemies
            .Where(enemy => enemy.Name.ToLower().Contains(partial.ToLower()))
            .Select(enemy => enemy.Name.ToLower()) // FIXED: Return just the name
            .ToList();
    }

    private List<string> GetUsableItemSuggestions(string partial)
    {
        return _gameState.GetPlayer().Inventory
            .Where(item => item.Name.ToLower().Contains(partial.ToLower()))
            .Select(item => item.Name.ToLower()) // FIXED: Return just the name
            .ToList();
    }

    // NEW: Add missing suggestion methods
    private List<string> GetEquippableItemSuggestions(string partial)
    {
        return _gameState.GetPlayer().Inventory
            .Where(item => (item.ItemType == ItemType.Weapon || item.ItemType == ItemType.Armor) && 
                          item.Name.ToLower().Contains(partial.ToLower()))
            .Select(item => item.Name.ToLower())
            .ToList();
    }

    private List<string> GetContainerSuggestions(string partial)
    {
        var suggestions = new List<string>();
        var location = _gameState.GetCurrentLocation();
        
        if (location?.Items != null)
        {
            suggestions.AddRange(location.Items
                .Where(item => item is Container && item.Name.ToLower().Contains(partial.ToLower()))
                .Select(item => item.Name.ToLower()));
        }
        
        // Also check inventory for containers
        suggestions.AddRange(_gameState.GetPlayer().Inventory
            .Where(item => item is Container && item.Name.ToLower().Contains(partial.ToLower()))
            .Select(item => item.Name.ToLower()));
        
        return suggestions;
    }

    // History methods for arrow key navigation only
    public void AddToHistory(string command)
    {
        if (string.IsNullOrWhiteSpace(command))
            return;
            
        // Remove duplicates
        _commandHistory.Remove(command);
        
        // Add to end
        _commandHistory.Add(command);
        
        // Limit history size
        if (_commandHistory.Count > _maxHistorySize)
        {
            _commandHistory.RemoveAt(0);
        }
    }

    public List<string> GetFullHistory()
    {
        return new List<string>(_commandHistory);
    }
}