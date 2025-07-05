using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Manages overall game state
public class GameState
{
    // Core properties
    private Player _player;
    private Dictionary<string, Location> _locations;
    private Location _currentLocation;
    private Dictionary<string, Character> _characters;
    private List<Quest> _quests;
    private Character _currentConversationPartner;
    private GameResourceLoader _resourceLoader;

    // Extension systems
    private SaveSystem _saveSystem;
    private EventSystem _eventSystem;
    private InventorySystem _inventorySystem;
    private CombatSystem _combatSystem;
    private CompanionSystem _companionSystem;
    private ProceduralRegionGenerator _proceduralGenerator;

    // Game state properties
    private bool _gameOver = false;
    private List<string> _pendingMessages = new List<string>();

    // For timed events
    private float _timeSinceLastUpdate = 0;
    private const float UPDATE_INTERVAL = 1.0f; // Update game state every second

    // Add these private fields at the top of the class
    private Dictionary<string, bool> _gameFlags = new Dictionary<string, bool>();
    private List<string> _completedQuests = new List<string>();
    private float _totalPlayTime = 0f;
    private int _saveCount = 0;
    private Dictionary<string, string> _worldEvents = new Dictionary<string, string>();
    private Dictionary<string, NPCState> _npcStates = new Dictionary<string, NPCState>();

    // NEW: Timed event system
    private bool _isTimedEventActive = false;
    private List<TimedEventStep> _currentTimedEventSteps = new List<TimedEventStep>();
    private int _currentTimedEventIndex = 0;
    private float _timedEventTimer = 0f;
    private AdvancedCLIEmulator _cliEmulator; // Reference to CLI for displaying text

    // Remove HIJACK sequence fields
    private AchievementSystem _achievementSystem;
    private bool _desertLabyrinthGenerated = false;
    private MediaManager _mediaManager; // NEW: Reference to MediaManager

    public GameState(GameResourceLoader resourceLoader)
    {
        _resourceLoader = resourceLoader;
        Initialize();

        // Initialize all extension systems
        _inventorySystem = new InventorySystem(_player);
        _combatSystem = new CombatSystem(this, _player);
        _eventSystem = new EventSystem(this);
        _saveSystem = new ImprovedSaveSystem();
        _companionSystem = new CompanionSystem(this);
        _proceduralGenerator = new ProceduralRegionGenerator(this);

        // Set up events
        InitializeEvents();
    }

    private void Initialize()
    {
        // Load game data
        _locations = _resourceLoader.LoadLocations();
        _characters = _resourceLoader.LoadAllCharacters();
        _quests = _resourceLoader.LoadAllQuests();

        // Set up player with starting inventory
        _player = new Player();
        foreach (var item in _resourceLoader.LoadStartingInventory())
        {
            _player.Inventory.Add(item);
        }

        // Set starting location
        string startingLocationId = _resourceLoader.GetStartingLocationId();
        if (_locations.TryGetValue(startingLocationId, out var startLocation))
        {
            _currentLocation = startLocation;
        }
        else
        {
            // Fallback to first location if starting location not found
            _currentLocation = _locations.FirstOrDefault().Value;
        }

        // Mark starting location as discovered
        if (_currentLocation != null)
        {
            _currentLocation.IsDiscovered = true;
        }

        // Add characters to locations
        SetupInitialGameState();
    }

    private void SetupInitialGameState()
    {
        // Add characters to their starting locations
        foreach (var character in _characters.Values)
        {
            // For simplicity, we'll add the wizard to the library location
            if (character.Id == "old_wizard" && _locations.ContainsKey("library"))
            {
                _locations["library"].Characters.Add(character);
            }
        }

        // Set up special items
        GameSetup.SetupSampleGame(_locations, _characters, _quests);
    }

    // Add getters for the extensions
    public InventorySystem GetInventorySystem() => _inventorySystem;
    public CombatSystem GetCombatSystem() => _combatSystem;
    public EventSystem GetEventSystem() => _eventSystem;
    public SaveSystem GetSaveSystem() => _saveSystem;
    public CompanionSystem GetCompanionSystem() => _companionSystem;

    public void Update(float delta)
    {
        // Update time-based systems
        _eventSystem.Update(delta);
        
        // Update timed event system
        UpdateTimedEvent(delta);
    }

    // Method to add messages to be displayed to the player
    public void AddMessage(string message)
    {
        _pendingMessages.Add(message);
    }

    // Method to get and clear pending messages
    public List<string> GetAndClearPendingMessages()
    {
        var messages = new List<string>(_pendingMessages);
        _pendingMessages.Clear();
        return messages;
    }

    public Dictionary<string, Location> GetLocations()
    {
        return _locations;
    }

    public Location GetLocationById(string locationId)
    {
        if (_locations.TryGetValue(locationId, out Location location))
        {
            return location;
        }
        return null;
    }

    public Location GetCurrentLocation()
    {
        return _currentLocation;
    }

    public void SetCurrentLocation(Location location)
    {
        _currentLocation = location;
    }
    
    // NEW: Overload to set location by ID
    public void SetCurrentLocation(string locationId)
    {
        if (_locations.TryGetValue(locationId, out Location destination))
        {
            // Generate desert labyrinth procedurally when first entering
            if (destination.Id == "desert_labyrinth" && !_desertLabyrinthGenerated)
            {
                _proceduralGenerator.GenerateDesertLabyrinth();
                _desertLabyrinthGenerated = true;
                GD.Print("Desert labyrinth procedurally generated!");
            }
            
            _currentLocation = destination;
            
            // Set location as discovered
            _currentLocation.IsDiscovered = true;

            // Check for any quests that trigger on location entry
            CheckLocationTriggers();
        }
    }

    public Player GetPlayer()
    {
        return _player;
    }

    public void AddLocation(string locationId, Location location)
    {
        _locations[locationId] = location;
    }

    // Add these public methods
    public float GetTotalPlayTime() => _totalPlayTime;
    public void SetTotalPlayTime(float playTime) => _totalPlayTime = playTime;

    public int GetSaveCount() => _saveCount;
    public void SetSaveCount(int saveCount) => _saveCount = saveCount;

    public Dictionary<string, string> GetWorldEvents() => _worldEvents;
    public void SetWorldEvents(Dictionary<string, string> worldEvents) => _worldEvents = worldEvents ?? new Dictionary<string, string>();

    public Dictionary<string, NPCState> GetNPCStates() => _npcStates;
    public void SetNPCStates(Dictionary<string, NPCState> npcStates) => _npcStates = npcStates ?? new Dictionary<string, NPCState>();

    public Dictionary<string, bool> GetGameFlags() => _gameFlags;
    public void SetGameFlags(Dictionary<string, bool> gameFlags) => _gameFlags = gameFlags ?? new Dictionary<string, bool>();

    public AchievementSystem GetAchievementSystem() => _achievementSystem;

    public int GetTotalItemsCollected()
    {
        return _player.Inventory.Count;
    }

    public List<string> GetTalkedToCharacters()
    {
        return new List<string>(); // Placeholder
    }

    public bool IsGameCompleted()
    {
        return false; // Placeholder
    }

    public void PlayerDefeated()
    {
        // Handle player defeat - reset health, move to starting location, etc.
        _player.Health = _player.MaxHealth / 2; // Restore some health

        // Move to starting location
        string startingLocationId = _resourceLoader.GetStartingLocationId();
        if (_locations.TryGetValue(startingLocationId, out var startLocation))
        {
            _currentLocation = startLocation;
        }

        AddMessage("You awaken, dazed and injured. Someone must have dragged you to safety.");
    }

    private void InitializeEvents()
    {
        // Initialize event handlers

        // Add NPC movement events
        var wanderingLocations = new List<string>();

        // Collect location IDs that should be part of the wandering path
        foreach (var location in _locations.Values)
        {
            if (location.Id != "treasure_room" && location.Id != "starting_room")  // Exclude some locations
            {
                wanderingLocations.Add(location.Id);
            }
        }

        // Set up wandering for a character if there are enough locations
        if (wanderingLocations.Count >= 2 && _characters.ContainsKey("old_wizard"))
        {
            _eventSystem.CreateWanderingNPCEvent("old_wizard", wanderingLocations, 120);  // 2 minutes
        }
    }

    public string GetCurrentLocationDescription()
    {
        if (_currentLocation == null)
        {
            return "You are nowhere. This is a bug.";
        }

        // Check if this is a procedural desert location and get dynamic description
        string description;
        if (_proceduralGenerator != null && _proceduralGenerator.IsProceduralDesertLocation(_currentLocation.Id))
        {
            description = $"{_currentLocation.Name}\n\n{_proceduralGenerator.GetDynamicLocationDescription(_currentLocation.Id)}\n";
        }
        else
        {
            description = $"{_currentLocation.Name}\n\n{_currentLocation.Description}\n";
        }

        // List exits with "Go" header (show user-friendly names)
        List<string> exits = _currentLocation.Exits.Keys.ToList();
        if (exits.Count > 0)
        {
            description += "\nMove";
            foreach (var exit in exits)
            {
                // Replace underscores with spaces for display
                string friendlyExitName = exit.Replace("_", " ");
                description += $"\n- {friendlyExitName}";
            }
        }

        // List items with "Items" header
        if (_currentLocation.Items.Count > 0)
        {
            description += "\n\nItems";
            foreach (var item in _currentLocation.Items)
            {
                description += $"\n- {item.Name}";
            }
        }

        // List characters with "Talk to" header
        if (_currentLocation.Characters.Count > 0)
        {
            description += "\n\nTalk to";
            foreach (var character in _currentLocation.Characters)
            {
                description += $"\n- {character.Name}";
            }
        }

        // List enemies (keeping existing format for now since not specified)
        if (_currentLocation.Enemies != null && _currentLocation.Enemies.Count > 0)
        {
            description += "\n\nEnemies";
            foreach (var enemy in _currentLocation.Enemies)
            {
                description += $"\n- {enemy.Name}";
            }
        }

        // List features with "Examine" header
        if (_currentLocation.Features.Count > 0)
        {
            description += "\n\nExamine";
            foreach (var feature in _currentLocation.Features)
            {
                description += $"\n- {feature.Key}";
            }
        }

        // NEW: Only show media notification for locations with images or manual scene paths (not HIJACK sequence)
        if (_currentLocation.HasImage || (_currentLocation.HasScene && _currentLocation.Id != "HIJACK_inside_closer"))
        {
            string mediaType = _currentLocation.HasScene ? "scene" : "image";
            description += $"\n\n[This location has a {mediaType}, type 'view']";
        }

        return description;
    }

    public string ExamineObject(string target)
    {
        // Check if target is in location's items
        Item item = _currentLocation.Items.FirstOrDefault(i =>
            i.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (item != null)
        {
            return item.Description;
        }

        // Check if target is in inventory
        item = _player.Inventory.FirstOrDefault(i =>
            i.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (item != null)
        {
            return item.Description;
        }

        // Check if target is a character
        Character character = _currentLocation.Characters.FirstOrDefault(c =>
            c.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
            c.Id.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (character != null)
        {
            return character.Description;
        }

        // Check if target is an enemy
        if (_currentLocation.Enemies != null)
        {
            Enemy enemy = _currentLocation.Enemies.FirstOrDefault(e =>
                e.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                e.Id.Equals(target, StringComparison.OrdinalIgnoreCase));

            if (enemy != null)
            {
                return enemy.Description;
            }
        }

        // Check if target is a feature of the location
        if (_currentLocation.Features.TryGetValue(target.ToLower(), out string description))
        {
            return description;
        }

        return $"You don't see any '{target}' here.";
    }

    public bool MovePlayer(string direction)
    {
        // Normalize direction input
        direction = direction.ToLower().Trim();

        // Handle common direction aliases
        var directionAliases = new Dictionary<string, string>
        {
            { "n", "north" },
            { "s", "south" },
            { "e", "east" },
            { "w", "west" },
            { "u", "up" },
            { "d", "down" },
            { "ne", "northeast" },
            { "nw", "northwest" },
            { "se", "southeast" },
            { "sw", "southwest" }
        };

        if (directionAliases.ContainsKey(direction))
        {
            direction = directionAliases[direction];
        }

        // Try to find exact match first
        string destinationId = null;
        if (_currentLocation.Exits.TryGetValue(direction, out destinationId))
        {
            // Found exact match, proceed with movement
        }
        else
        {
            // If no exact match, try to find a match by converting spaces to underscores
            string directionWithUnderscores = direction.Replace(" ", "_");
            if (_currentLocation.Exits.TryGetValue(directionWithUnderscores, out destinationId))
            {
                direction = directionWithUnderscores;
            }
            else
            {
                // Try to find a match by checking if any exit key matches when spaces are converted to underscores
                var matchingExit = _currentLocation.Exits.FirstOrDefault(exit => 
                    exit.Key.Replace("_", " ").Equals(direction, StringComparison.OrdinalIgnoreCase) ||
                    exit.Key.Equals(direction, StringComparison.OrdinalIgnoreCase));
                
                if (!string.IsNullOrEmpty(matchingExit.Key))
                {
                    direction = matchingExit.Key;
                    destinationId = matchingExit.Value;
                }
            }
        }

        if (!string.IsNullOrEmpty(destinationId) && _locations.TryGetValue(destinationId, out Location destination))
        {
            // Check if exit is locked
            if (_currentLocation.LockedExits.TryGetValue(direction, out var lockInfo))
            {
                if (lockInfo.IsLocked)
                {
                    // Check if player has the key
                    if (_player.HasItem(lockInfo.KeyItem))
                    {
                        _currentLocation.LockedExits[direction].IsLocked = false;
                        AddMessage($"You unlock the {direction} exit with the {lockInfo.KeyItem}.");
                    }
                    else
                    {
                        AddMessage(lockInfo.LockedMessage);
                        return false;
                    }
                }
            }

            // Use SetCurrentLocation with string ID to handle procedural generation
            SetCurrentLocation(destinationId);
            return true;
        }

        // Return false - let CommandProcessor handle the error message
        return false;
    }

    public void MoveCharacter(string characterId, string locationId)
    {
        // Find the character
        Character character = null;
        Location currentLocation = null;

        foreach (var location in _locations.Values)
        {
            var foundCharacter = location.Characters.FirstOrDefault(c => c.Id == characterId);
            if (foundCharacter != null)
            {
                character = foundCharacter;
                currentLocation = location;
                break;
            }
        }

        if (character == null || currentLocation == null || !_locations.ContainsKey(locationId))
        {
            return;
        }

        // Move the character to the new location
        currentLocation.Characters.Remove(character);
        _locations[locationId].Characters.Add(character);

        // Notify player if they're in the relevant locations
        if (_currentLocation == currentLocation)
        {
            // Add a message that the character has left
            AddMessage($"{character.Name} leaves the area.");
        }
        else if (_currentLocation.Id == locationId)
        {
            // Add a message that a character has entered
            AddMessage($"{character.Name} enters the area.");
        }
    }

    public string TakeItem(string itemName)
    {
        Item foundItem = _currentLocation.Items.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (foundItem == null)
        {
            // Better error message with suggestions
            if (_currentLocation.Items.Count > 0)
            {
                var availableItems = string.Join(", ", _currentLocation.Items.Where(i => i.CanTake).Select(i => $"'{i.Name}'"));
                if (!string.IsNullOrEmpty(availableItems))
                {
                    return $"There's no '{itemName}' here to take. Available items: {availableItems}";
                }
            }
            return $"There's no '{itemName}' here to take.";
        }

        if (!foundItem.CanTake)
        {
            return $"You can't take the {foundItem.Name}.";
        }

        // Check weight limit if inventory system is available
        if (_inventorySystem != null && !_inventorySystem.CanAddItem(foundItem))
        {
            return $"The {foundItem.Name} is too heavy to carry with everything else you have.";
        }

        _currentLocation.Items.Remove(foundItem);
        _player.Inventory.Add(foundItem);

        return $"You take the {foundItem.Name}.";
    }

    public string DropItem(string itemName)
    {
        Item item = _player.Inventory.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            return $"You don't have a '{itemName}' in your inventory.";
        }

        if (!item.CanDrop)
        {
            return $"You can't drop the {item.Name}.";
        }

        _player.Inventory.Remove(item);
        _currentLocation.Items.Add(item);

        return $"You drop the {item.Name}.";
    }

    public string GetInventoryDescription()
    {
        if (_player.Inventory.Count == 0)
        {
            return "Your inventory is empty.";
        }

        string inventory = "Inventory:\n";
        
        for (int i = 0; i < _player.Inventory.Count; i++)
        {
            Item item = _player.Inventory[i];
            inventory += $"- {item.Name}";
            
            if (item.IsStackable && item.StackCount > 1)
            {
                inventory += $" (x{item.StackCount})";
            }
            
            inventory += "\n";
        }

        // Show equipped items
        if (_player.EquippedWeapon != null || _player.EquippedArmor != null)
        {
            inventory += "\nEquipped:\n";
            if (_player.EquippedWeapon != null)
            {
                inventory += $"- Weapon: {_player.EquippedWeapon.Name}\n";
            }
            if (_player.EquippedArmor != null)
            {
                inventory += $"- Armor: {_player.EquippedArmor.Name}\n";
            }
        }

        return inventory.TrimEnd();
    }
    
    public string UseItem(string itemName, string target)
    {
        // First, check if the player has the item
        Item item = _player.Inventory.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
            i.Id.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            return $"You don't have a '{itemName}' to use.";
        }

        // If we're in combat and it's a healing item, use it in combat
        if (_combatSystem.IsInCombat && item.ItemType == ItemType.Healing)
        {
            return _combatSystem.UseItem(item);
        }

        // If no target specified, check if item has a default transition first
        if (string.IsNullOrEmpty(target))
        {
            // Check for default transition first
            string transitionResult = ProcessDefaultItemTransition(item);
            if (transitionResult != null)
            {
                return transitionResult;
            }
            
            if (item.DefaultUseAction != null)
            {
                string result = item.DefaultUseAction();
                CheckItemTriggers(item, TriggerType.Use);
                return result;
            }
            else
            {
                return $"How do you want to use the {item.Name}?";
            }
        }

        // Check usable targets in the current location
        foreach (var locationItem in _currentLocation.Items)
        {
            if (locationItem.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
                locationItem.Id.Equals(target, StringComparison.OrdinalIgnoreCase))
            {
                // Check if this item can be used with the target
                if (item.UseTargets.TryGetValue(locationItem.Id, out var useAction))
                {
                    string result = useAction();
                    CheckItemTriggers(item, TriggerType.UseOn, locationItem);
                    return result;
                }
                else
                {
                    return $"You can't use the {item.Name} with the {locationItem.Name}.";
                }
            }
        }

        // Check if target is a character
        Character character = _currentLocation.Characters.FirstOrDefault(c =>
            c.Name.Equals(target, StringComparison.OrdinalIgnoreCase) ||
            c.Id.Equals(target, StringComparison.OrdinalIgnoreCase));

        if (character != null)
        {
            if (item.UseTargets.TryGetValue(character.Id, out var useAction))
            {
                string result = useAction();
                CheckItemTriggers(item, TriggerType.UseOn);
                return result;
            }
            else
            {
                return $"You can't use the {item.Name} with {character.Name}.";
            }
        }

        // Check if target is a feature of the location
        if (_currentLocation.Features.ContainsKey(target.ToLower()))
        {
            if (item.UseTargets.TryGetValue(target.ToLower(), out var useAction))
            {
                string result = useAction();
                CheckItemTriggers(item, TriggerType.UseOn);
                return result;
            }
        }
        
        // Check for location transitions triggered by using item on target
        if (item.LocationTransitions.TryGetValue(target.ToLower(), out var transition))
        {
            if (transition.CanTransition(_currentLocation.Id, _gameFlags))
            {
                return ProcessItemTransition(item, transition);
            }
            else
            {
                return $"You can't use the {item.Name} here right now.";
            }
        }

        return $"You don't see a '{target}' to use that on.";
    }
    
    private string ProcessItemTransition(Item item, ItemTransition transition)
    {
        // Check if target location exists
        var targetLocation = GetLocationById(transition.TargetLocationId);
        if (targetLocation == null)
        {
            return $"The {item.Name} doesn't seem to work here.";
        }
        
        // Consume item if required
        if (transition.ConsumeItem)
        {
            _player.Inventory.Remove(item);
        }
        
        // Move player to new location
        _currentLocation = targetLocation;
        _currentLocation.IsDiscovered = true;
        
        // Check for location triggers in new location
        CheckLocationTriggers();
        
        // Return transition message
        string message = !string.IsNullOrEmpty(transition.TransitionMessage) 
            ? transition.TransitionMessage 
            : $"Using the {item.Name}, you find yourself in a new location.";
            
        return message;
    }
    
    private string ProcessDefaultItemTransition(Item item)
    {
        if (item.DefaultTransition == null)
        {
            return null;
        }
        
        // Check if transition can be performed in current location
        if (!item.DefaultTransition.CanTransition(_currentLocation.Id, _gameFlags))
        {
            return null; // Location or other requirements not met, don't process transition
        }
        
        // Check if target location exists
        var targetLocation = GetLocationById(item.DefaultTransition.TargetLocationId);
        if (targetLocation == null)
        {
            return null;
        }
        
        // Process the transition since all requirements are met
        return ProcessItemTransition(item, item.DefaultTransition);
    }

    public string StartConversation(string characterName)
    {
        Character character = _currentLocation.Characters.FirstOrDefault(c =>
            c.Name.Equals(characterName, StringComparison.OrdinalIgnoreCase) ||
            c.Id.Equals(characterName, StringComparison.OrdinalIgnoreCase));

        if (character == null)
        {
            // Better error with available characters
            if (_currentLocation.Characters.Count > 0)
            {
                var availableCharacters = string.Join(", ", _currentLocation.Characters.Select(c => $"'{c.Name}'"));
                return $"There's no '{characterName}' here to talk to. Available: {availableCharacters}";
            }
            return $"There's no '{characterName}' here to talk to.";
        }

        _currentConversationPartner = character;
        string greeting = character.StartConversation();

        // Add helpful hint about saying things
        return greeting + "\n\n(You can 'say [text]' to respond, or 'look' to end the conversation)";
    }

    public string Say(string text)
    {
        if (_currentConversationPartner == null)
        {
            return "You're not talking to anyone.";
        }

        return _currentConversationPartner.RespondTo(text);
    }

    public string GetQuestLog()
    {
        List<Quest> activeQuests = _quests.Where(q => q.IsActive && !q.IsCompleted).ToList();

        if (activeQuests.Count == 0)
        {
            return "You don't have any active quests.";
        }

        string questLog = "Active Quests:";

        foreach (var quest in activeQuests)
        {
            questLog += $"\n\n{quest.Name}";
            questLog += $"\n{quest.Description}";

            questLog += "\nObjectives:";
            foreach (var objective in quest.Objectives)
            {
                string status = objective.IsCompleted ? "[✓]" : "[ ]";
                questLog += $"\n{status} {objective.Description}";
            }
        }

        return questLog;
    }

    public string GetPlayerStatus()
    {
        string status = "Player Status:";
        status += $"\nHealth: {_player.Health}/{_player.MaxHealth}";
        status += $"\nSatoshi: ${_player.Satoshi}"; // Added $ symbol for clarity

        if (_player.Level > 1)
        {
            status += $"\nLevel: {_player.Level}";
            status += $"\nExperience: {_player.ExperiencePoints}";
            status += $"\nAttack Power: {_player.AttackPower}";
            status += $"\nDefense: {_player.Defense}";
        }

        if (_player.EquippedWeapon != null)
        {
            status += $"\nWeapon: {_player.EquippedWeapon.Name}";
        }

        if (_player.EquippedArmor != null)
        {
            status += $"\nArmor: {_player.EquippedArmor.Name}";
        }

        return status;
    }

    // Save and load game methods
    public void SaveGame(string slotName = "quicksave")
    {
        _saveSystem.SaveGame(this, slotName);
        AddMessage($"Game saved to slot '{slotName}'.");
    }

    public bool LoadGame(string slotName)
    {
        bool success = _saveSystem.LoadGame(slotName, this);

        if (success)
        {
            AddMessage($"Game loaded from slot '{slotName}'.");
        }

        return success;
    }

    private void CheckLocationTriggers()
    {
        // Check for special location triggers
        switch (_currentLocation.Id)
        {
            case "hijack_temple_arm":
                TriggerHijackIntroScene();
                break;
            // Add more location triggers here as needed
        }

        foreach (var quest in _quests)
        {
            // Auto-activate quests when entering relevant locations
            if (!quest.IsActive && !quest.IsCompleted)
            {
                foreach (var trigger in quest.Triggers)
                {
                    if (trigger.Type == TriggerType.Enter && trigger.TargetId == _currentLocation.Id)
                    {
                        quest.IsActive = true;
                        AddMessage($"New quest activated: {quest.Name}");
                        break;
                    }
                }
            }

            if (quest.IsActive && !quest.IsCompleted)
            {
                quest.CheckLocationTrigger(_currentLocation.Id);
            }
        }
    }

    // NEW: Trigger HIJACK intro scene using MediaManager
    private void TriggerHijackIntroScene()
    {
        if (_mediaManager != null)
        {
            // Trigger scene directly using MediaManager's DisplayScene method
            // Pass the scene path directly instead of using location data
            _mediaManager.DisplaySceneDirect("res://scenes/hijack/hijackIntro.tscn");
        }
        else
        {
            GD.PrintErr("GameState: MediaManager not available for HIJACK intro");
            // Fallback without scene
            AddMessage("The prosthetic arm activates! Pain shoots through your body!");
            SetCurrentLocation("hijack_temple_writhing");
        }
    }

    private void CheckItemTriggers(Item item, TriggerType triggerType, Item targetItem = null)
    {
        string targetId = targetItem?.Id;
        foreach (var quest in _quests)
        {
            if (quest.IsActive && !quest.IsCompleted)
            {
                quest.CheckItemTrigger(item.Id, triggerType, targetId);
            }
        }
    }

    // NEW: Method to set MediaManager reference
    public void SetMediaManager(MediaManager mediaManager)
    {
        _mediaManager = mediaManager;
        if (_mediaManager != null)
        {
            _mediaManager.MediaViewerClosed += OnMediaViewerClosed;
        }
    }

    // NEW: Handle media viewer closed event
    private void OnMediaViewerClosed()
    {
        // Check if we need to move to a specific location after scene closes
        if (_currentLocation?.Id == "hijack_temple_arm")
        {
            SetCurrentLocation("hijack_temple_writhing");
            // Add the location description message
            AddMessage(GetCurrentLocationDescription());
        }
    }

    // NEW: Timed event system
    private void UpdateTimedEvent(float delta)
    {
        if (!_isTimedEventActive || _currentTimedEventSteps.Count == 0)
            return;

        _timedEventTimer += delta;

        // Check if it's time for the next step
        if (_currentTimedEventIndex < _currentTimedEventSteps.Count)
        {
            var currentStep = _currentTimedEventSteps[_currentTimedEventIndex];
            
            if (_timedEventTimer >= currentStep.DelaySeconds)
            {
                // Execute the current step
                if (_cliEmulator != null)
                {
                    if (!string.IsNullOrEmpty(currentStep.Message))
                    {
                        _cliEmulator.DisplayOutput(currentStep.Message);
                    }
                }
                
                _currentTimedEventIndex++;
                _timedEventTimer = 0f; // Reset timer for next step
                
                // Check if we've completed all steps
                if (_currentTimedEventIndex >= _currentTimedEventSteps.Count)
                {
                    _isTimedEventActive = false;
                }
            }
        }
    }

    // Add missing methods
    public bool HasGameFlag(string flagName)
    {
        return _gameFlags.ContainsKey(flagName) && _gameFlags[flagName];
    }

    public void SetGameFlag(string flagName, bool value)
    {
        _gameFlags[flagName] = value;
    }

    public List<string> GetCompletedQuestIds()
    {
        return _quests
            .Where(q => q.IsCompleted)
            .Select(q => q.Id)
            .ToList();
    }

    public void SetCompletedQuests(List<string> questIds)
    {
        foreach (var questId in questIds)
        {
            var quest = _quests.FirstOrDefault(q => q.Id == questId);
            if (quest != null)
            {
                quest.IsCompleted = true;
                // Also complete all objectives
                foreach (var objective in quest.Objectives)
                {
                    objective.IsCompleted = true;
                }
            }
        }
    }

    public List<string> GetActiveQuestIds()
    {
        return _quests
            .Where(q => q.IsActive && !q.IsCompleted)
            .Select(q => q.Id)
            .ToList();
    }

    public Dictionary<string, int> GetQuestProgress()
    {
        var progress = new Dictionary<string, int>();
        foreach (var quest in _quests.Where(q => q.IsActive))
        {
            int completedObjectives = quest.Objectives.Count(o => o.IsCompleted);
            progress[quest.Id] = completedObjectives;
        }
        return progress;
    }

    public bool CheckSpecialCondition(string condition)
    {
        switch (condition.ToLower())
        {
            case "show mercy to all shadow entities":
                return HasGameFlag("merciful_to_shadows");
            default:
                return false;
        }
    }

    public void AddQuest(Quest quest)
    {
        if (!_quests.Any(q => q.Id == quest.Id))
        {
            _quests.Add(quest);
        }
    }

    public List<Location> GetDiscoveredLocations()
    {
        return _locations.Values.Where(l => l.IsDiscovered).ToList();
    }

    public List<string> GetDiscoveredLocationIds()
    {
        return _locations.Values
            .Where(l => l.IsDiscovered)
            .Select(l => l.Id)
            .ToList();
    }

    public void SetDiscoveredLocations(List<string> locationIds)
    {
        foreach (var locationId in locationIds)
        {
            if (_locations.TryGetValue(locationId, out var location))
            {
                location.IsDiscovered = true;
            }
        }
    }

    // NEW: Set CLI emulator reference
    public void SetCLIEmulator(AdvancedCLIEmulator cliEmulator)
    {
        _cliEmulator = cliEmulator;
    }

    // NEW: Add TimedEventStep class
    public class TimedEventStep
    {
        public float DelaySeconds { get; set; }
        public string Message { get; set; }
        public string ScenePath { get; set; }
        public string Action { get; set; }
    }
}