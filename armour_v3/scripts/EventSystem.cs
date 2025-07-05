using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Manages event triggers
public class EventSystem
{
    private GameState _gameState;
    private List<GameEvent> _events = new List<GameEvent>();
    private ProceduralRegionGenerator _regionGenerator;
    
    public EventSystem(GameState gameState)
    {
        _gameState = gameState;
        _regionGenerator = new ProceduralRegionGenerator(gameState);
    }
    
    public void Update(float delta)
    {
        // Update and process all events
        for (int i = _events.Count - 1; i >= 0; i--)
        {
            var gameEvent = _events[i];
            
            // Update event timer
            gameEvent.TimeRemaining -= delta;
            
            // Check if event should trigger
            if (gameEvent.TimeRemaining <= 0)
            {
                // Execute the event
                gameEvent.Action();
                
                // Handle repeat behavior
                if (gameEvent.RepeatInterval > 0)
                {
                    // Reset timer for repeating events
                    gameEvent.TimeRemaining = gameEvent.RepeatInterval;
                }
                else
                {
                    // Remove one-time events
                    _events.RemoveAt(i);
                }
            }
        }
    }
    
    public void AddEvent(string name, float delay, Action action, float repeatInterval = 0)
    {
        _events.Add(new GameEvent
        {
            Name = name,
            TimeRemaining = delay,
            Action = action,
            RepeatInterval = repeatInterval
        });
    }
    
    public void RemoveEvent(string name)
    {
        _events.RemoveAll(e => e.Name == name);
    }
    
    public void CreateWanderingNPCEvent(string characterId, List<string> locations, float intervalSeconds)
    {
        if (locations.Count < 2)
        {
            return; // Need at least 2 locations to wander between
        }
        
        AddEvent($"wandering_{characterId}", intervalSeconds, () => {
            // Get a random location from the list, different from the current one
            Character character = null;
            string currentLocationId = null;
            
            // Find where the character currently is
            foreach (var location in locations)
            {
                var locationObj = _gameState.GetLocationById(location);
                if (locationObj == null) continue;
                
                var foundCharacter = locationObj.Characters.FirstOrDefault(c => c.Id == characterId);
                if (foundCharacter != null)
                {
                    character = foundCharacter;
                    currentLocationId = location;
                    break;
                }
            }
            
            if (character == null)
            {
                // Character not found in any of the specified locations
                // Place them in the first valid location
                foreach (var location in locations)
                {
                    var locationObj = _gameState.GetLocationById(location);
                    if (locationObj != null)
                    {
                        currentLocationId = location;
                        break;
                    }
                }
            }
            
            // If we found a current location, move to a different one
            if (currentLocationId != null)
            {
                // Get possible destinations (excluding current location)
                var possibleDestinations = locations
                    .Where(l => l != currentLocationId)
                    .ToList();
                
                if (possibleDestinations.Count > 0)
                {
                    // Pick a random destination
                    Random random = new Random();
                    string destination = possibleDestinations[random.Next(possibleDestinations.Count)];
                    
                    // Move the character
                    _gameState.MoveCharacter(characterId, destination);
                }
            }
        }, intervalSeconds);
    }
    
    public void ScheduleDelayedMessage(string message, float delaySeconds)
    {
        AddEvent("delayed_message", delaySeconds, () => {
            _gameState.AddMessage(message);
        });
    }
    
    public void ScheduleLocationEvent(string locationId, string eventMessage, float delaySeconds, Action<GameState> action = null)
    {
        AddEvent($"location_event_{locationId}", delaySeconds, () => {
            // Only trigger if player is in this location
            if (_gameState.GetCurrentLocation().Id == locationId)
            {
                _gameState.AddMessage(eventMessage);
                
                if (action != null)
                {
                    action(_gameState);
                }
            }
            else
            {
                // Reschedule if player isn't here yet
                ScheduleLocationEvent(locationId, eventMessage, 60, action);
            }
        });
    }
    
    public void TriggerDesertLabyrinthGeneration()
    {
        AddEvent("generate_desert_labyrinth", 0.1f, () => {
            _regionGenerator.GenerateDesertLabyrinth();
            _gameState.AddMessage("The desert shifts around you as ancient pathways reveal themselves...");
        });
    }
}

// Class to represent a scheduled game event
public class GameEvent
{
    public string Name { get; set; }
    public float TimeRemaining { get; set; }
    public Action Action { get; set; }
    public float RepeatInterval { get; set; }
}