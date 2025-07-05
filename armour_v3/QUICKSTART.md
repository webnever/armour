## Core Components

### AdvancedCLIEmulator

The `AdvancedCLIEmulator` class manages the user interface, input/output, and rendering:

- Handles user input and keyboard events
- Maintains command history
- Manages scrolling and display
- Processes commands via the CommandProcessor
- Displays game output with styling

Key methods:
- `AddLine(string text, Color? color)` - Adds a line of text to the display
- `DisplayOutput(string text, Color? color)` - Displays text with appropriate styling
- `ForceScrollToBottom()` - Ensures the newest content is visible
- `HandleCommand(string text)` - Passes commands to the CommandProcessor
- `DisplayCommand(string text)` - Shows the command entered by the player

### CommandProcessor

The `CommandProcessor` interprets user commands and routes them to the appropriate game systems:

- Parses input text into commands and arguments
- Maps commands to handler methods
- Executes game actions based on commands
- Returns text responses to display

Key methods:
- `ProcessCommand(string input)` - Parses and executes a command
- `HandleHelp(string[] args)` - Provides help information
- Various command handlers (HandleLook, HandleTake, etc.)

The CommandProcessor supports a comprehensive set of commands:

```
Basic commands: help, look, examine, go, move, take, drop, inventory, use, talk, say, quest, status
Extended system commands: save, load, saves
Combat commands: attack, flee, stats
Inventory commands: equip, unequip, open, close, put, take_from
Puzzle commands: solve, hint
System commands: clear, exit
```

### GameState

The `GameState` class serves as the central manager for game data and the core of the framework:

```csharp
public class GameState
{
	// Core properties
	private Player _player;
	private Dictionary<string, Location> _locations;
	private Location _currentLocation;
	private Dictionary<string, Character> _characters;
	private List<Quest> _quests;
	private Character _currentConversationPartner;
	private ResourceLoader _resourceLoader;
	
	// Extension systems
	private SaveSystem _saveSystem;
	private EventSystem _eventSystem;
	private InventorySystem _inventorySystem;
	private CombatSystem _combatSystem;
	private PuzzleSystem _puzzleSystem;
	private ProceduralGenerator _proceduralGenerator;
	
	// Game state properties
	private bool _gameOver = false;
	private string _timeOfDay = "morning";
	private List<string> _pendingMessages = new List<string>();
	
	// Methods for game state management
	// ...
}
```

Key responsibilities:
- Track game progress and player state
- Maintain the game world state
- Handle player actions (movement, interactions, etc.)
- Coordinate between subsystems
- Manage pending messages queue

Game action methods:
- `MovePlayer(string direction)` - Move the player between locations
- `ExamineObject(string target)` - Get description of objects, items or characters
- `TakeItem(string itemName)` - Pick up items from the current location
- `DropItem(string itemName)` - Drop inventory items in the current location
- `UseItem(string itemName, string target)` - Use items, potentially on targets
- `StartConversation(string characterName)` - Begin dialogue with NPCs
- `Say(string text)` - Continue dialogue with response text

### EventSystem

The `EventSystem` manages time-based events and triggers:

```csharp
public class EventSystem
{
	private GameState _gameState;
	private List<GameEvent> _events = new List<GameEvent>();
	private float _gameTime = 0; // Time in seconds
	private int _currentDay = 1;
	
	// Time of day constants
	private const float MORNING_START = 0;
	private const float AFTERNOON_START = 21600; // 6 hours
	private const float EVENING_START = 43200; // 12 hours  
	private const float NIGHT_START = 64800; // 18 hours
	private const float DAY_LENGTH = 86400; // 24 hours in seconds
	
	// Time acceleration (1.0 = real time, higher = faster)
	private float _timeScale = 60.0f; // 1 minute real time = 1 hour game time
	
	// Methods for scheduling and managing events
	// ...
}
```

Key features:
- Maintains game time and day/night cycle
- Schedules and executes timed events
- Manages weather changes and NPC movements
- Provides time-of-day descriptions

Key methods:
- `AddEvent(string name, float delay, Action action, float repeatInterval)` - Schedules a new event
- `CreateWanderingNPCEvent(string characterId, List<string> locations, float intervalSeconds)` - Sets up NPC movement
- `ScheduleLocationEvent(string locationId, string eventMessage, float delaySeconds, Action<GameState> action)` - Creates location-specific events
- `CreateWeatherEvent(float intervalSeconds)` - Implements changing weather conditions

### CombatSystem

The `CombatSystem` handles all combat-related functionality:

```csharp
public class CombatSystem
{
	private GameState _gameState;
	private Player _player;
	private Enemy _currentEnemy;
	private bool _isInCombat;
	private Random _random = new Random();
	
	// Combat settings
	private const float PlayerFleeChance = 0.5f;
	private const float EnemyFleeThreshold = 0.2f; // Enemy might flee when below 20% health
	
	// Methods for combat
	// ...
}
```

Key features:
- Manages turn-based combat encounters
- Handles damage calculation with randomization
- Implements special attacks and combat items
- Rewards players with experience, gold, and loot

Key methods:
- `StartCombat(Enemy enemy)` - Begins a combat encounter
- `Attack()` - Handles player attacking the enemy
- `Flee()` - Attempt to escape from combat
- `UseItem(Item item)` - Use items during combat

### InventorySystem

The `InventorySystem` manages the player's inventory and item interactions:

```csharp
public class InventorySystem
{
	private Player _player;
	private float _maxCarryWeight = 50.0f; // Default max carry weight
	
	// Methods for inventory management
	// ...
}
```

Key features:
- Tracks inventory weight and limits
- Organizes items by category
- Handles transferring items between containers
- Provides detailed item descriptions

Key methods:
- `GetInventoryDescription()` - Gets formatted inventory listing
- `CanAddItem(Item item)` - Checks if player can carry additional items
- `TransferItem(Item item, Container container, bool toContainer)` - Moves items between inventory and containers
- `GetItemDetailsDescription(Item item)` - Gets detailed item information

### SaveSystem

The `SaveSystem` handles game state persistence:

```csharp
public class SaveSystem
{
	private string _savePath = "user://saves/";
	private const string EXTENSION = ".save";
	
	// Methods for saving/loading
	// ...
}
```

Key features:
- Saves game state to JSON files
- Loads saved games
- Manages save slots
- Handles error conditions gracefully

Key methods:
- `SaveGame(GameState gameState, string slotName)` - Creates a save file
- `LoadGame(string slotName, GameState gameState)` - Restores game from save
- `GetSaveSlots()` - Lists available save files
- `DeleteSaveSlot(string slotName)` - Removes a saved game

### PuzzleSystem

The `PuzzleSystem` manages puzzles and their solutions:

```csharp
public class PuzzleSystem
{
	private GameState _gameState;
	private Dictionary<string, PuzzleStatus> _puzzleStatuses = new Dictionary<string, PuzzleStatus>();
	
	// Methods for puzzle solving
	// ...
}
```

Key features:
- Tracks puzzle attempt counts
- Provides progressive hints
- Supports multiple solution types
- Triggers game state changes when puzzles are solved

Key methods:
- `SolvePuzzle(string puzzleId, string solution)` - Attempts to solve a puzzle
- `GetHint(string puzzleId)` - Provides incremental hints for puzzles
- `GetSolvedPuzzleIds()` - Lists all solved puzzles
- `SetSolvedPuzzles(List<string> puzzleIds)` - Sets puzzle completion state

The system supports multiple solution types:
- `ExactMatchSolution` - Direct text matching
- `RegexSolution` - Pattern-based matching
- `ItemCombinationSolution` - Using items together
- `StateSolution` - State-based solutions

### ResourceLoader

The `ResourceLoader` handles loading game data:

```csharp
public class ResourceLoader
{
	private string _configPath;
	
	// Methods for loading game data
	// ...
}
```

Key responsibilities:
- Loads location data
- Creates characters and dialogue
- Sets up inventory items
- Configures quests and puzzles

Key methods:
- `LoadLocations()` - Creates the game world map
- `LoadCharacters()` - Sets up NPCs and their dialogue
- `LoadStartingInventory()` - Configures player's initial items
- `LoadQuests()` - Creates quest data structures

### Entity Classes

The framework includes several essential entity classes that represent game objects:

#### Location

```csharp
public class Location
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public Dictionary<string, string> Exits { get; set; } = new Dictionary<string, string>();
	public Dictionary<string, LockInfo> LockedExits { get; set; } = new Dictionary<string, LockInfo>();
	public List<Item> Items { get; set; } = new List<Item>();
	public List<Character> Characters { get; set; } = new List<Character>();
	public Dictionary<string, string> Features { get; set; } = new Dictionary<string, string>();
	
	// Extension properties
	public Dictionary<string, string> TimeBasedDescriptions { get; set; } = new Dictionary<string, string>();
	public List<Puzzle> Puzzles { get; set; } = new List<Puzzle>();
	public List<Enemy> Enemies { get; set; } = new List<Enemy>();
	public string Region { get; set; } = "General"; // For grouping locations
	public bool IsDiscovered { get; set; } = false;
	public Dictionary<string, string> StateBasedFeatures { get; set; } = new Dictionary<string, string>();
}
```

#### Item and Container

```csharp
public class Item
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public bool CanTake { get; set; } = true;
	public Func<string> DefaultUseAction { get; set; }
	public Dictionary<string, Func<string>> UseTargets { get; set; } = new Dictionary<string, Func<string>>();
	
	// Extension properties
	public float Weight { get; set; } = 1.0f;
	public string Category { get; set; } = "Miscellaneous";
	public ItemType ItemType { get; set; } = ItemType.Miscellaneous;
	public int UseValue { get; set; } = 0;
	public bool IsStackable { get; set; } = false;
	public int StackCount { get; set; } = 1;
	public Dictionary<string, string> StateBasedDescriptions { get; set; } = new Dictionary<string, string>();
}

public class Container : Item
{
	public List<Item> Items { get; set; } = new List<Item>();
	public float Capacity { get; set; } = 20.0f; // Maximum weight capacity
	public bool IsOpen { get; set; } = false;
	
	// Container-specific methods
	// ...
}
```

#### Character and Dialogue

```csharp
public class Character
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public Dictionary<string, DialogueNode> Dialogue { get; set; } = new Dictionary<string, DialogueNode>();
	public string CurrentDialogueId { get; set; } = "greeting";
	
	public string StartConversation()
	{
		// Dialogue handling...
	}
	
	public string RespondTo(string playerText)
	{
		// Response handling with regex pattern matching...
	}
}

public class DialogueNode
{
	public string Id { get; set; }
	public string Text { get; set; }
	public List<DialogueResponse> Responses { get; set; } = new List<DialogueResponse>();
	public string DefaultResponseId { get; set; }
}

public class DialogueResponse
{
	public string Pattern { get; set; }
	public string NextDialogueId { get; set; }
	public Action Action { get; set; }
}
```

#### Player

```csharp
public class Player
{
	// Basic stats
	public int Health { get; set; } = 100;
	public int MaxHealth { get; set; } = 100;
	public int Level { get; set; } = 1;
	public int ExperiencePoints { get; set; } = 0;
	public int Gold { get; set; } = 0;
	
	// Character stats
	public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>
	{
		{ "Strength", 10 },
		{ "Dexterity", 10 },
		{ "Intelligence", 10 }
	};
	
	// Inventory
	public List<Item> Inventory { get; set; } = new List<Item>();
	public Item EquippedWeapon { get; set; }
	public Item EquippedArmor { get; set; }
	
	// Derived stats
	public int AttackPower { get { /* Calculate attack */ } }
	public int Defense { get { /* Calculate defense */ } }
	
	// Equipment methods
	public void EquipItem(Item item) { /* Equipment logic */ }
	public void Unequip(ItemType itemType) { /* Unequip logic */ }
	
	// Utility methods
	public bool HasItem(string itemId) { /* Check inventory */ }
	public void GainExperience(int amount) { /* Experience logic */ }
}
```

#### Enemy

```csharp
public class Enemy
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	
	// Combat stats
	public int Health { get; set; }
	public int MaxHealth { get; set; }
	public int AttackPower { get; set; }
	public int Defense { get; set; }
	
	// Rewards
	public int ExperienceReward { get; set; }
	public int GoldReward { get; set; }
	
	// Extended properties
	public List<LootEntry> PossibleLoot { get; set; } = new List<LootEntry>();
	public Dictionary<string, Action<GameState>> SpecialAbilities { get; set; } = new Dictionary<string, Action<GameState>>();
	public Dictionary<string, string> Dialog { get; set; } = new Dictionary<string, string>();
	public bool CanFlee { get; set; } = true;
	public bool IsAggressive { get; set; } = true;
	public string DeathMessage { get; set; }
	public List<string> Tags { get; set; } = new List<string>();
	
	// Enemy methods
	public Enemy Clone() { /* Cloning logic */ }
	public virtual (string description, int damage) SpecialAttack() { /* Special attack logic */ }
	public virtual bool ShouldUseSpecialAttack(int turnNumber, float healthPercentage) { /* Decision logic */ }
	public string GetResponse(string playerAction) { /* Response logic */ }
}
```

#### Quest

```csharp
public class Quest
{
	public string Id { get; set; }
	public string Name { get; set; }
	public string Description { get; set; }
	public bool IsActive { get; set; }
	public bool IsCompleted { get; set; }
	public List<QuestObjective> Objectives { get; set; } = new List<QuestObjective>();
	public List<QuestTrigger> Triggers { get; set; } = new List<QuestTrigger>();
	
	// Quest methods
	public void CheckLocationTrigger(string locationId) { /* Location trigger logic */ }
	public void CheckItemTrigger(string itemId, TriggerType triggerType, string targetId = null) { /* Item trigger logic */ }
}

public class QuestObjective
{
	public string Id { get; set; }
	public string Description { get; set; }
	public bool IsCompleted { get; set; }
}

public class QuestTrigger
{
	public TriggerType Type { get; set; }
	public string TargetId { get; set; }
	public string SecondaryTargetId { get; set; }
	public List<string> ObjectiveIds { get; set; } = new List<string>();
}
```

## Creating Your Game

### Setting Up the Project

1. Create a new Godot project
2. Add the framework scripts to your project
3. Create the necessary scenes:
   - Main scene
   - AddLine.tscn (for text display)
   - CommandPrompt.tscn (for command input)

### Implementing Game Data

You have two options for implementing game data:

#### Option 1: Direct Code Implementation

Directly create game data in code as shown in the `ResourceLoader` class:

```csharp
public Dictionary<string, Location> LoadLocations()
{
	var locations = new Dictionary<string, Location>();
	
	// Create locations
	var startingRoom = new Location
	{
		Id = "starting_room",
		Name = "Starting Room",
		Description = "A small, dimly lit room with stone walls...",
		// ...
	};
	
	// Set up connections
	startingRoom.Exits["north"] = "hallway";
	
	// Add locked doors
	hallway.LockedExits["north"] = new LockInfo 
	{ 
		IsLocked = true, 
		KeyItem = "rusty_key", 
		LockedMessage = "The door is locked tight..." 
	};
	
	// Add time-based descriptions
	library.TimeBasedDescriptions["morning"] = "Rays of morning sunlight filter through...";
	
	// Add locations to dictionary
	locations["starting_room"] = startingRoom;
	
	return locations;
}
```

#### Option 2: JSON Data Files

Create JSON files for game data (recommended for larger games):

```json
{
  "locations": [
	{
	  "id": "starting_room",
	  "name": "Starting Room",
	  "description": "A small, dimly lit room with stone walls...",
	  "exits": {
		"north": "hallway"
	  },
	  "lockedExits": {
		"east": {
		  "isLocked": true,
		  "keyItem": "rusty_key",
		  "lockedMessage": "The door is locked tight..."
		}
	  },
	  "timeBasedDescriptions": {
		"morning": "Morning light filters through the cracks...",
		"night": "The room is pitch black except for the torch..."
	  }
	},
	// More locations...
  ]
}
```

### Setting Up the Main Scene

1. Create a main scene with an instance of `AdvancedCLIEmulator`
2. Configure the paths for the required subscenes:

```csharp
// In AdvancedCLIEmulator
private Dictionary<string, string> _scenes = new Dictionary<string, string>
{
	{ "add_line", "res://scenes/add_line.tscn" },
	{ "prompt", "res://scenes/command_prompt.tscn" }
};
```

3. Initialize the game systems in your main script:

```csharp
public override void _Ready()
{
	// Initialize the framework
	_resourceLoader = new ResourceLoader("res://game_data/config.json");
	_gameState = new GameState(_resourceLoader);
	_commandProcessor = new CommandProcessor(_gameState);
	
	// Get UI components
	_cliEmulator = GetNode<AdvancedCLIEmulator>("CLIEmulator");
	
	// Display the welcome message
	_cliEmulator.DisplayOutput("Welcome to your adventure!");
}
```

### Creating Content

1. **Design your world map**: Plan the locations and connections
2. **Create interesting characters**: Write memorable dialogue and compelling stories
3. **Implement puzzles**: Design engaging puzzles with multiple solution approaches
4. **Balance combat**: Create enemies with appropriate difficulty progression
5. **Write atmospheric descriptions**: Use time-based descriptions for immersion

#### Example: Creating a Simple Quest

```csharp
var rescueQuest = new Quest
{
	Id = "rescue_villager",
	Name = "The Missing Villager",
	Description = "A villager has gone missing in the dark forest. Find them and bring them back safely.",
	IsActive = true,
	IsCompleted = false
};

// Add objectives
rescueQuest.Objectives.Add(new QuestObjective
{
	Id = "find_villager",
	Description = "Find the missing villager in the forest",
	IsCompleted = false
});

rescueQuest.Objectives.Add(new QuestObjective
{
	Id = "escort_to_safety",
	Description = "Escort the villager back to the village",
	IsCompleted = false
});

// Add triggers
rescueQuest.Triggers.Add(new QuestTrigger
{
	Type = TriggerType.Location,
	TargetId = "forest_clearing",
	ObjectiveIds = new List<string> { "find_villager" }
});

rescueQuest.Triggers.Add(new QuestTrigger
{
	Type = TriggerType.Location,
	TargetId = "village_square",
	ObjectiveIds = new List<string> { "escort_to_safety" }
});
```

## Data Formats

For production games, it's recommended to use JSON files to store your game data. Here are examples of recommended data formats:

### Location Data

```json
{
  "id": "forest_clearing",
  "name": "Forest Clearing",
  "description": "A small clearing in the dense forest. Sunlight filters through the canopy above.",
  "exits": {
	"north": "deep_forest",
	"south": "forest_path",
	"east": "river_bank"
  },
  "lockedExits": {
	"west": {
	  "isLocked": true,
	  "keyItem": "old_key",
	  "lockedMessage": "A tangled wall of thorns blocks the path. Perhaps there's a way through."
	}
  },
  "features": {
	"sunlight": "Beams of golden sunlight break through the leaves above.",
	"tree stump": "An old tree stump sits in the center of the clearing. It's covered in moss."
  },
  "timeBasedDescriptions": {
	"morning": "Morning dew clings to the grass as sunlight begins to filter through the trees.",
	"afternoon": "Bright sunlight fills the clearing, making it a warm and pleasant spot.",
	"evening": "Long shadows stretch across the clearing as the sun begins to set.",
	"night": "Moonlight bathes the clearing in a soft, silver glow. The forest around you is alive with nocturnal sounds."
  },
  "items": ["forest_herb", "strange_stone"],
  "enemies": ["forest_wolf"],
  "region": "Northern Forest"
}
```

### Item Data

```json
{
  "id": "healing_potion",
  "name": "Healing Potion",
  "description": "A small flask containing a red liquid that smells of herbs and honey.",
  "itemType": "Healing",
  "useValue": 20,
  "weight": 0.5,
  "category": "Potion",
  "canTake": true,
  "isStackable": true
}
```

### Character and Dialogue Data

```json
{
  "id": "village_elder",
  "name": "Elder Moira",
  "description": "An elderly woman with silver hair and knowing eyes. She wears simple clothing adorned with intricate embroidery.",
  "dialogue": {
	"greeting": {
	  "text": "Ah, a traveler! We don't get many visitors these days. What brings you to our humble village?",
	  "responses": [
		{
		  "pattern": "(help|assist|aid)",
		  "nextDialogueId": "offer_help"
		},
		{
		  "pattern": "(passing through|traveling|journey)",
		  "nextDialogueId": "just_passing"
		},
		{
		  "pattern": "(village|town|place)",
		  "nextDialogueId": "about_village"
		}
	  ],
	  "defaultResponseId": "default_response"
	},
	"offer_help": {
	  "text": "You wish to help? That's kind of you. As it happens, one of our villagers went missing in the forest to the north. We fear the worst...",
	  "responses": [
		{
		  "pattern": "(search|find|rescue|look)",
		  "nextDialogueId": "start_quest"
		},
		{
		  "pattern": "dangerous",
		  "nextDialogueId": "forest_dangers"
		}
	  ],
	  "defaultResponseId": "default_response"
	},
	"start_quest": {
	  "text": "You'll search for them? Thank the gods! The villager, a young man named Tomas, went to gather herbs in the northern forest yesterday and hasn't returned. Please, find him and bring him back safely."
	}
  }
}
```

## Best Practices

### Organization

1. **Modular design**: Split your game into logical modules
2. **Clean data separation**: Keep game content separate from game logic
3. **Clear naming conventions**: Use consistent naming for IDs and references

### Performance

1. **Limit event frequency**: Don't schedule events too frequently
2. **Optimize text rendering**: Limit the number of text lines kept in memory
3. **Use appropriate data structures**: Use dictionaries for quick lookups

### Content Design

1. **Progressive difficulty**: Gradually introduce new mechanics
2. **Environmental storytelling**: Use location descriptions to tell stories
3. **Meaningful choices**: Give players choices that# Text Adventure Framework - Developer's Guide

This guide will help you understand and use the Text Adventure Framework to develop your own text-based adventure games in Godot using C#.

## Table of Contents

1. [Introduction](#introduction)
2. [System Architecture](#system-architecture)
3. [Core Components](#core-components)
   - [AdvancedCLIEmulator](#advancedcliemulator)
   - [CommandProcessor](#commandprocessor)
   - [GameState](#gamestate)
   - [EventSystem](#eventsystem)
   - [CombatSystem](#combatsystem)
   - [Inventory
