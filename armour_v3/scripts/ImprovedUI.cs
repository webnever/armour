using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;

public class ImprovedUI
{
    private AdvancedCLIEmulator _cliEmulator;
    private bool _showMinimap = true;
    
    public ImprovedUI(AdvancedCLIEmulator cliEmulator)
    {
        _cliEmulator = cliEmulator;
        LoadUserPreferences();
    }
    
    public void ToggleMinimap()
    {
        _showMinimap = !_showMinimap;
    }
    
    public bool IsMinimapEnabled()
    {
        return _showMinimap;
    }
    
    public string GenerateMinimap(GameState gameState)
    {
        if (!_showMinimap)
            return "";
            
        var currentLocation = gameState.GetCurrentLocation();
        var map = new Dictionary<(int, int), string>();
        var visited = new HashSet<string>();
        
        // Build map using BFS
        BuildMapFromLocation(currentLocation, gameState, map, visited, 0, 0, 2);
        
        // Convert to string
        return RenderMinimap(map, 0, 0);
    }
    
    private void BuildMapFromLocation(Location location, GameState gameState, 
        Dictionary<(int, int), string> map, HashSet<string> visited, 
        int x, int y, int depth)
    {
        if (depth <= 0 || visited.Contains(location.Id))
            return;
            
        visited.Add(location.Id);
        map[(x, y)] = location.IsDiscovered ? "○" : "?";
        
        if (location == gameState.GetCurrentLocation())
            map[(x, y)] = "●";
            
        // Map exits to coordinates
        foreach (var exit in location.Exits)
        {
            var (dx, dy) = GetDirectionOffset(exit.Key);
            var nextLocation = gameState.GetLocationById(exit.Value);
            
            if (nextLocation != null)
            {
                BuildMapFromLocation(nextLocation, gameState, map, visited, 
                    x + dx, y + dy, depth - 1);
            }
        }
    }
    
    private (int, int) GetDirectionOffset(string direction)
    {
        return direction.ToLower() switch
        {
            "north" => (0, -1),
            "south" => (0, 1),
            "east" => (1, 0),
            "west" => (-1, 0),
            "up" => (0, -2),
            "down" => (0, 2),
            _ => (0, 0)
        };
    }
    
    private string RenderMinimap(Dictionary<(int, int), string> map, int centerX, int centerY)
    {
        if (map.Count == 0)
            return "No map available";
            
        int minX = map.Keys.Min(k => k.Item1);
        int maxX = map.Keys.Max(k => k.Item1);
        int minY = map.Keys.Min(k => k.Item2);
        int maxY = map.Keys.Max(k => k.Item2);
        
        string minimap = "┌─── Map ───┐\n";
        
        for (int y = minY; y <= maxY; y++)
        {
            minimap += "│ ";
            for (int x = minX; x <= maxX; x++)
            {
                if (map.TryGetValue((x, y), out string symbol))
                {
                    minimap += symbol + " ";
                }
                else
                {
                    minimap += "  ";
                }
            }
            minimap += "│\n";
        }
        
        minimap += "└───────────┘\n";
        minimap += "● You  ○ Visited  ? Unknown";
        
        return minimap;
    }
    
    private void LoadUserPreferences()
    {
        // Load saved preferences
        try
        {
            using var file = FileAccess.Open("user://ui_preferences.json", FileAccess.ModeFlags.Read);
            if (file != null)
            {
                string json = file.GetAsText();
                var prefs = JsonSerializer.Deserialize<UIPreferences>(json);
                
                _showMinimap = prefs.ShowMinimap;
            }
        }
        catch
        {
            // Use defaults
        }
    }
    
    public void SaveUserPreferences()
    {
        var prefs = new UIPreferences
        {
            ShowMinimap = _showMinimap
        };
        
        try
        {
            string json = JsonSerializer.Serialize(prefs);
            using var file = FileAccess.Open("user://ui_preferences.json", FileAccess.ModeFlags.Write);
            file?.StoreString(json);
        }
        catch
        {
            // Ignore save errors
        }
    }
}

public class UIPreferences
{
    public bool ShowMinimap { get; set; }
}