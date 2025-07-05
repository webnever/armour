using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class ProceduralRegionGenerator
{
    private GameState _gameState;
    private Random _random;
    private Vector2 _templePosition;
    private List<Vector2> _generatedPositions;
    private GameResourceLoader _resourceLoader;
    
    // Radar corruption settings
    private float _radarCorruptionScale = 0f; // Multiplier for corruption intensity
    
    // 7G Radiation reader settings
    private float _baseRadiationLevel = 2.5f; // Base radiation level (mSv/h)
    private float _radiationDistanceMultiplier = 1.8f; // How much radiation increases per unit distance
    private float _radiationNoiseScale = 0.3f; // Random noise factor for radiation readings
    private Vector2 _exitPosition = Vector2.Zero; // Center (0,0) is the exit point
    
    // Store position data for dynamic descriptions
    private Dictionary<string, Vector2> _locationPositions = new Dictionary<string, Vector2>();
    
    public ProceduralRegionGenerator(GameState gameState)
    {
        _gameState = gameState;
        _random = new Random();
        _generatedPositions = new List<Vector2>();
        // Get the resource loader from GameState to access enemy data
        _resourceLoader = gameState.GetType().GetField("_resourceLoader", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)
            ?.GetValue(gameState) as GameResourceLoader;
    }
    
    public void GenerateDesertLabyrinth()
    {
        // Generate all positions within radius 3 of center (0,0)
        var positions = new List<Vector2>();
        
        for (int x = -3; x <= 3; x++)
        {
            for (int y = -3; y <= 3; y++)
            {
                float distance = Mathf.Sqrt(x * x + y * y);
                if (distance <= 3.0f && !(x == 0 && y == 0)) // Exclude center (desert_labyrinth)
                {
                    positions.Add(new Vector2(x, y));
                }
            }
        }
        
        // Randomly select temple position (not at center)
        _templePosition = positions[_random.Next(positions.Count)];
        _generatedPositions = positions;
        
        // Generate locations
        foreach (var pos in positions)
        {
            GenerateLocationAtPosition(pos);
        }
        
        // Update desert_labyrinth exits to connect to adjacent positions
        UpdateCenterLocationExits();
    }
    
    private void GenerateLocationAtPosition(Vector2 position)
    {
        string locationId = GetLocationId(position);
        bool isTemple = position == _templePosition;
        
        if (isTemple)
        {
            // Don't create a new temple location - use existing "hijack_temple"
            // The temple already exists in the game data, we just need to connect to it
            return; // Skip creating this location since it already exists
        }
        else
        {
            // Store position for dynamic description generation
            _locationPositions[locationId] = position;
            
            // Create regular desert location with placeholder description
            var location = new Location
            {
                Id = locationId,
                Name = GetLocationName(position),
                Description = "Generating radar data...", // Placeholder - will be replaced dynamically
                Exits = GetLocationExits(position),
                Region = "Desert Labyrinth",
                IsDiscovered = false
            };
            
            // Add random Black Rats
            if (_random.NextDouble() < 0.4) // 40% chance
            {
                int ratCount = _random.Next(1, 3); // 1-2 rats
                for (int i = 0; i < ratCount; i++)
                {
                    // Get black_rat from resource loader and clone it
                    var allEnemies = _resourceLoader?.LoadAllEnemies();
                    if (allEnemies != null && allEnemies.TryGetValue("black_rat", out var blackRatTemplate))
                    {
                        var blackRat = blackRatTemplate.Clone();
                        location.Enemies.Add(blackRat);
                    }
                    else
                    {
                        // Fallback: create manually if loading fails
                        var blackRat = new Enemy
                        {
                            Id = "black_rat",
                            Name = "Black Rat",
                            Description = "A corrupted rodent with glitching fur and red digital eyes. It moves erratically, leaving pixelated trails.",
                            Health = 15,
                            MaxHealth = 15,
                            AttackPower = 5,
                            Defense = 2,
                            ExperienceReward = 10,
                            SatoshiReward = 0
                        };
                        location.Enemies.Add(blackRat);
                    }
                }
            }
            
            _gameState.AddLocation(location.Id, location);
        }
    }
    
    private string GetLocationId(Vector2 position)
    {
        return $"desert_proc_{(int)position.X}_{(int)position.Y}";
    }
    
    private string GetLocationName(Vector2 position)
    {
        string[] names = {
            "3rd Earth Desert", "3rd Earth Dunes", "3rd Earth Plains"
        };
        return names[_random.Next(names.Length)];
    }
    
    // New method to calculate dynamic radiation readings
    private float GetDynamicRadiationReading(Vector2 position)
    {
        // Create a new Random instance for radiation readings
        int locationSeed = ((int)position.X * 2000) + ((int)position.Y * 3); // Different from radar seed
        int timeSeed = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond / 100); // Changes every 100ms for more variation
        var radiationRandom = new Random(locationSeed + timeSeed);
        
        // Calculate distance from exit (center at 0,0)
        float distanceFromExit = (position - _exitPosition).Length();
        
        // Base radiation increases with distance
        float radiationLevel = _baseRadiationLevel + (distanceFromExit * _radiationDistanceMultiplier);
        
        // Add random noise
        float noiseRange = radiationLevel * _radiationNoiseScale;
        float noise = (float)(radiationRandom.NextDouble() * 2.0 - 1.0) * noiseRange; // -noiseRange to +noiseRange
        radiationLevel += noise;
        
        // Ensure minimum radiation level
        radiationLevel = Mathf.Max(0.1f, radiationLevel);
        
        return radiationLevel;
    }
    
    // New method to get radiation status description
    private string GetRadiationStatusDescription(float radiationLevel)
    {
        if (radiationLevel < 1.0f)
            return "SAFE - Minimal 7G interference detected";
        else if (radiationLevel < 3.0f)
            return "CAUTION - Moderate 7G radiation levels";
        else if (radiationLevel < 6.0f)
            return "WARNING - High 7G radiation detected";
        else if (radiationLevel < 10.0f)
            return "DANGER - Extreme 7G radiation levels";
        else
            return "CRITICAL - Lethal 7G radiation exposure";
    }
    
    // New method for dynamic descriptions that recalculate corruption each time
    private string GetDynamicLocationDescription(Vector2 position)
    {
        // Create a new Random instance with time + location coordinates as seed for true randomness
        // This ensures different readings for different locations AND changes over time
        int locationSeed = ((int)position.X * 1000) + (int)position.Y; // Convert position to unique int
        int timeSeed = (int)(DateTime.Now.Ticks / TimeSpan.TicksPerSecond); // Changes every second
        var dynamicRandom = new Random(locationSeed + timeSeed);
        
        // Calculate actual distance to temple
        float actualDistance = (position - _templePosition).Length();
        
        // Add corruption noise scaled by corruption factor - RECALCULATED WITH FRESH RANDOM
        float corruptionRange = 1.0f * _radarCorruptionScale;
        float corruptedDistance = actualDistance + (dynamicRandom.Next(-(int)corruptionRange, (int)corruptionRange + 1));
        corruptedDistance = Mathf.Max(0.1f, corruptedDistance); // Prevent negative
        
        // Get direction with some corruption (only cardinal directions) - RECALCULATED WITH FRESH RANDOM
        Vector2 directionToTemple = (_templePosition - position).Normalized();
        string[] directions = {"north", "east", "south", "west"};
        float angle = Mathf.Atan2(directionToTemple.Y, directionToTemple.X) * 180 / Mathf.Pi;
        
        // Add noise to direction scaled by corruption factor - RECALCULATED WITH FRESH RANDOM
        float directionCorruption = 22.5f * _radarCorruptionScale;
        angle += dynamicRandom.Next(-(int)directionCorruption, (int)directionCorruption + 1);
        
        // Normalize angle and get direction (90 degree increments for cardinal directions)
        angle = (angle + 360) % 360;
        int dirIndex = Mathf.RoundToInt(angle / 90) % 4;
        string direction = directions[dirIndex];
        
        // Get radiation reading
        float radiationLevel = GetDynamicRadiationReading(position);
        string radiationStatus = GetRadiationStatusDescription(radiationLevel);
        
        string[] descriptions = {
            $"Sand swirls all around you. Your scanner's corrupted readings suggest the temple is {corruptedDistance:F1} units {direction}, but the data flickers unreliably.\n\n7G RADIATION: {radiationLevel:F2} mSv/h - {radiationStatus}",
            $"The desert stretches endlessly. Coordinates indicate the temple lies approximately {corruptedDistance:F1} units {direction}, though static interferes with precision.\n\n7G RADIATION: {radiationLevel:F2} mSv/h - {radiationStatus}",
            $"Black sand shifts beneath your feet. Your proximity sensor reports the temple at roughly {corruptedDistance:F1} units {direction}, but the signal wavers with electromagnetic interference.\n\n7G RADIATION: {radiationLevel:F2} mSv/h - {radiationStatus}",
            $"The landscape pulses with glitching artifacts. Temple proximity: ~{corruptedDistance:F1} units {direction}. Corruption is detected in the navigation systems.\n\n7G RADIATION: {radiationLevel:F2} mSv/h - {radiationStatus}"
        };
        
        return descriptions[dynamicRandom.Next(descriptions.Length)];
    }

    private Dictionary<string, string> GetLocationExits(Vector2 position)
    {
        var exits = new Dictionary<string, string>();
        
        // Check only cardinal directions (no diagonals)
        var cardinalDirections = new[]
        {
            new { dx = 0, dy = 1, name = "north" },
            new { dx = 1, dy = 0, name = "east" },
            new { dx = 0, dy = -1, name = "south" },
            new { dx = -1, dy = 0, name = "west" }
        };
        
        foreach (var dir in cardinalDirections)
        {
            Vector2 adjacent = position + new Vector2(dir.dx, dir.dy);
            
            // Check if adjacent position exists in our generated positions or is center
            bool isCenter = adjacent.X == 0 && adjacent.Y == 0;
            bool isGenerated = _generatedPositions.Contains(adjacent);
            bool isTemple = adjacent == _templePosition;
            
            if (isCenter || isGenerated || isTemple)
            {
                string targetId = isCenter ? "desert_labyrinth" : 
                                isTemple ? "hijack_temple" : 
                                GetLocationId(adjacent);
                exits[dir.name] = targetId;
            }
        }
        
        return exits;
    }
    
    private void UpdateCenterLocationExits()
    {
        var centerLocation = _gameState.GetLocationById("desert_labyrinth");
        if (centerLocation != null)
        {
            // Calculate distance from center to temple for player guidance
            float distanceToTemple = _templePosition.Length();
            string directionToTemple = GetApproximateDirection(Vector2.Zero, _templePosition);
            
            centerLocation.Description = $"Pathways stretch in all directions, leading deeper into the desert. Your scanner picks up a signal approximately {distanceToTemple:F1} units {directionToTemple}.";
            centerLocation.Exits.Clear();
            
            // Add exits to adjacent generated positions (cardinal directions only)
            int exitCount = 0;
            var cardinalDirections = new[]
            {
                new { dx = 0, dy = 1, name = "north" },
                new { dx = 1, dy = 0, name = "east" },
                new { dx = 0, dy = -1, name = "south" },
                new { dx = -1, dy = 0, name = "west" }
            };
            
            foreach (var dir in cardinalDirections)
            {
                Vector2 adjacent = new Vector2(dir.dx, dir.dy);
                if (_generatedPositions.Contains(adjacent) || adjacent == _templePosition)
                {
                    string targetId = adjacent == _templePosition ? "hijack_temple" : GetLocationId(adjacent);
                    centerLocation.Exits[dir.name] = targetId;
                    exitCount++;
                }
            }
            
            // Debug output to verify exits are being created
            GD.Print($"Desert labyrinth center updated with {exitCount} exits");
            foreach (var exit in centerLocation.Exits)
            {
                GD.Print($"  {exit.Key} -> {exit.Value}");
            }
        }
        else
        {
            GD.PrintErr("Could not find desert_labyrinth location to update exits");
        }
    }
    
    private string GetApproximateDirection(Vector2 from, Vector2 to)
    {
        Vector2 direction = (to - from).Normalized();
        string[] directions = {"north", "east", "south", "west"}; // Removed diagonals
        float angle = Mathf.Atan2(direction.Y, direction.X) * 180 / Mathf.Pi;
        
        // Normalize angle and get direction (90 degree increments)
        angle = (angle + 360) % 360;
        int dirIndex = Mathf.RoundToInt(angle / 90) % 4;
        return directions[dirIndex];
    }
    
    private string GetDirectionName(int dx, int dy)
    {
        if (dx == 0 && dy == 1) return "north";
        if (dx == 1 && dy == 0) return "east";
        if (dx == 0 && dy == -1) return "south";
        if (dx == -1 && dy == 0) return "west";
        return "unknown";
    }
    
    // Public method to adjust radar corruption
    public void SetRadarCorruptionScale(float scale)
    {
        _radarCorruptionScale = Mathf.Max(0.0f, scale); // Prevent negative values
    }
    
    // Public method to get dynamic description for a location
    public string GetDynamicLocationDescription(string locationId)
    {
        if (!_locationPositions.TryGetValue(locationId, out Vector2 position))
        {
            return "Unknown location.";
        }
        
        return GetDynamicLocationDescription(position);
    }
    
    // Public method to get radiation reading for a location
    public float GetRadiationReading(string locationId)
    {
        if (!_locationPositions.TryGetValue(locationId, out Vector2 position))
        {
            return _baseRadiationLevel; // Return base level for unknown locations
        }
        
        return GetDynamicRadiationReading(position);
    }
    
    // Public method to adjust radiation parameters
    public void SetRadiationParameters(float baseLevel, float distanceMultiplier, float noiseScale)
    {
        _baseRadiationLevel = Mathf.Max(0.0f, baseLevel);
        _radiationDistanceMultiplier = Mathf.Max(0.0f, distanceMultiplier);
        _radiationNoiseScale = Mathf.Clamp(noiseScale, 0.0f, 1.0f);
    }
    
    // Public method to get radiation status for a location
    public string GetRadiationStatus(string locationId)
    {
        float radiation = GetRadiationReading(locationId);
        return GetRadiationStatusDescription(radiation);
    }
    
    // Check if a location ID is a procedurally generated desert location
    public bool IsProceduralDesertLocation(string locationId)
    {
        return _locationPositions.ContainsKey(locationId);
    }
}
