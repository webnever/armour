# JSON-Based Game Development Guide for Text Adventure Framework

This guide focuses on creating text adventure games using the JSON data format with the Text Adventure Framework. Using JSON for game content offers several advantages, including easier content creation, separation of game data from code, and the ability for non-programmers to contribute to game development.

## Table of Contents

1. [Introduction](#introduction)
2. [Setting Up a JSON-Based Project](#setting-up-a-json-based-project)
3. [JSON Data Structure](#json-data-structure)
4. [Creating Game Content](#creating-game-content)
   - [Locations](#locations)
   - [Items](#items)
   - [Characters and Dialogue](#characters-and-dialogue)
   - [Enemies](#enemies)
   - [Puzzles](#puzzles)
   - [Quests](#quests)
5. [Loading JSON Data](#loading-json-data)
6. [Advanced JSON Techniques](#advanced-json-techniques)
7. [Example Game](#example-game)
8. [Best Practices](#best-practices)
9. [Troubleshooting](#troubleshooting)

## Introduction

JSON (JavaScript Object Notation) is a lightweight data interchange format that's easy for humans to read and write and easy for machines to parse and generate. For text adventure game development, JSON provides an excellent way to separate game content from the game engine code.

With the Text Adventure Framework, you can define all game content—locations, items, characters, dialogue, quests, and puzzles—in JSON files. This allows game designers and writers to create content without needing to modify C# code.

## Setting Up a JSON-Based Project

### Project Structure

A well-organized JSON-based text adventure project typically has this folder structure:

```
project/
├── game_data/
│   ├── config.json
│   ├── locations/
│   │   ├── starting_room.json
│   │   ├── hallway.json
│   │   └── ...
│   ├── items/
│   │   ├── common_items.json
│   │   ├── weapons.json
│   │   └── ...
│   ├── characters/
│   │   ├── villagers.json
│   │   ├── enemies.json
│   │   └── ...
│   ├── quests/
│   │   ├── main_quests.json
│   │   ├── side_quests.json
│   │   └── ...
│   └── puzzles/
│       ├── riddles.json
│       ├── mechanical_puzzles.json
│       └── ...
└── scripts/
    ├── ResourceLoader.cs
    ├── GameState.cs
    └── ...
```

### Configuration File

The main configuration file (`config.json`) specifies global game settings and the starting location:

```json
{
  "gameTitle": "The Haunted Mansion",
  "version": "1.0",
  "author": "Your Name",
  "startingLocationId": "entrance_hall",
  "startingInventory": ["torch", "basic_dagger"],
  "timeScale": 60.0,
  "playerStartStats": {
    "health": 100,
    "maxHealth": 100,
    "gold": 25,
    "strength": 10,
    "dexterity": 10,
    "intelligence": 10
  }
}
```

## JSON Data Structure

### Basic Structure

Each JSON file should follow this basic structure:

```json
{
  "type": "locations",
  "data": [
    {
      "id": "unique_identifier",
      "name": "Human-Readable Name",
      "description": "Detailed description...",
      // Other properties specific to this data type
    },
    // More items of the same type
  ]
}
```

The `type` field helps identify the content type, and the `data` array contains the actual game entities.

## Creating Game Content

### Locations

Locations are the spaces the player can navigate through in your game world.

```json
{
  "type": "locations",
  "data": [
    {
      "id": "entrance_hall",
      "name": "Grand Entrance Hall",
      "description": "A spacious hall with a marble floor and a grand staircase. Dusty chandeliers hang from the high ceiling, and portraits of stern-looking ancestors line the walls.",
      "exits": {
        "north": "grand_staircase",
        "east": "dining_room",
        "west": "library",
        "south": "front_garden"
      },
      "lockedExits": {
        "east": {
          "isLocked": true,
          "keyItem": "dining_room_key",
          "lockedMessage": "The ornate double doors to the east are locked. You need a key to open them."
        }
      },
      "features": {
        "portraits": "The portraits depict the former owners of the mansion. Their eyes seem to follow you as you move across the hall.",
        "chandelier": "A massive crystal chandelier hangs from the ceiling. Although covered in cobwebs, it still sparkles when light hits it.",
        "staircase": "A sweeping staircase leads to the upper floor. The red carpet is worn in the middle from years of use."
      },
      "timeBasedDescriptions": {
        "morning": "Morning light streams through the tall windows, illuminating dancing dust particles in the air.",
        "afternoon": "The afternoon sun casts long shadows across the marble floor from the tall windows.",
        "evening": "As evening falls, the hall grows dimmer, with shadows gathering in the corners.",
        "night": "In the darkness, the hall is barely visible. Moonlight creates eerie patterns on the floor through the windows."
      },
      "itemIds": ["silver_candlestick", "dusty_book"],
      "characterIds": [],
      "enemyIds": [],
      "puzzleIds": ["chandelier_puzzle"],
      "region": "Mansion Ground Floor",
      "isDiscovered": false,
      "stateBasedFeatures": {
        "chandelier_fixed": "The chandelier has been repaired and now bathes the hall in warm light, revealing intricate details in the architecture you hadn't noticed before."
      }
    }
  ]
}
```

#### Locked Exits Explanation

Locked exits require a specific item to unlock:

```json
"lockedExits": {
  "east": {
    "isLocked": true,
    "keyItem": "dining_room_key",
    "lockedMessage": "The ornate double doors to the east are locked. You need a key to open them."
  }
}
```

### Items

Items are objects that players can interact with, collect, and use in the game.

```json
{
  "type": "items",
  "data": [
    {
      "id": "silver_candlestick",
      "name": "Silver Candlestick",
      "description": "An ornate silver candlestick, tarnished with age but still valuable.",
      "canTake": true,
      "weight": 2.0,
      "category": "Valuable",
      "itemType": "Miscellaneous",
      "useValue": 0,
      "isStackable": false,
      "defaultUseResult": "You lift the candlestick, but there's no candle to light."
    },
    {
      "id": "dining_room_key",
      "name": "Ornate Brass Key",
      "description": "A heavy brass key with an intricate design. It might unlock an important door.",
      "canTake": true,
      "weight": 0.5,
      "category": "Key",
      "itemType": "Key",
      "useValue": 0,
      "useTargets": {
        "dining_room_door": "You insert the key into the lock and turn it. With a click, the door to the dining room unlocks."
      }
    },
    {
      "id": "healing_potion",
      "name": "Healing Potion",
      "description": "A small flask containing a ruby-red liquid that smells of herbs and honey.",
      "canTake": true,
      "weight": 0.5,
      "category": "Potion",
      "itemType": "Healing",
      "useValue": 25,
      "isStackable": true,
      "defaultUseResult": "You drink the healing potion. The sweet liquid warms you from within, and you feel your wounds begin to close."
    },
    {
      "id": "rusty_sword",
      "name": "Rusty Sword",
      "description": "An old sword with a rusty blade. Despite its condition, it's still sharp enough to be dangerous.",
      "canTake": true,
      "weight": 3.0,
      "category": "Weapon",
      "itemType": "Weapon",
      "useValue": 5,
      "defaultUseResult": "You swing the sword through the air with a satisfying whoosh."
    }
  ]
}
```

#### Container Items

Containers are special items that can hold other items:

```json
{
  "id": "wooden_chest",
  "name": "Wooden Chest",
  "description": "A sturdy wooden chest with iron bands and a simple lock.",
  "canTake": false,
  "weight": 10.0,
  "category": "Container",
  "itemType": "Container",
  "isOpen": false,
  "capacity": 20.0,
  "contents": ["gold_coin", "silver_ring", "old_letter"]
}
```

### Characters and Dialogue

Characters bring your game world to life with dialogue and interactions.

```json
{
  "type": "characters",
  "data": [
    {
      "id": "old_butler",
      "name": "Grimsworth the Butler",
      "description": "An elderly man with impeccable posture, dressed in a somewhat outdated but perfectly maintained butler's uniform. His face is weathered, but his eyes are sharp and observant.",
      "dialogue": {
        "greeting": {
          "text": "Good day. I am Grimsworth, the butler of this estate. How may I be of service?",
          "responses": [
            {
              "pattern": "(house|mansion|building|estate)",
              "nextDialogueId": "about_mansion"
            },
            {
              "pattern": "(you|butler|grimsworth|yourself)",
              "nextDialogueId": "about_self"
            },
            {
              "pattern": "(owner|master|mistress|family)",
              "nextDialogueId": "about_owners"
            },
            {
              "pattern": "(key|dining room|locked|east)",
              "nextDialogueId": "about_key"
            },
            {
              "pattern": "(bye|goodbye|farewell|leave)",
              "nextDialogueId": "farewell"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "about_mansion": {
          "text": "This mansion has stood for over 150 years. The Blackwood family built it at the height of their prosperity. It has many rooms, some of which have remained locked for decades.",
          "responses": [
            {
              "pattern": "(locked|rooms|key)",
              "nextDialogueId": "about_key"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "about_self": {
          "text": "I have served in this house for over forty years. I know every corner, every secret. Well, almost every secret.",
          "defaultResponseId": "default_response"
        },
        "about_owners": {
          "text": "The master and mistress have been... away for some time now. I maintain the house in their absence, awaiting their return.",
          "responses": [
            {
              "pattern": "(where|gone|away|return)",
              "nextDialogueId": "owners_whereabouts"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "owners_whereabouts": {
          "text": "I... I'm not at liberty to discuss the particulars of their absence. House policy, you understand.",
          "defaultResponseId": "default_response"
        },
        "about_key": {
          "text": "The dining room key? Yes, I believe the master kept it in his study upstairs. But that room has been locked since... the incident.",
          "responses": [
            {
              "pattern": "(incident|what happened)",
              "nextDialogueId": "about_incident"
            }
          ],
          "defaultResponseId": "default_response",
          "action": "activate_quest_find_key"
        },
        "about_incident": {
          "text": "I... I shouldn't speak of it. Some things are best left in the past.",
          "defaultResponseId": "default_response"
        },
        "farewell": {
          "text": "Very good. If you require anything further, I shall be around."
        },
        "default_response": {
          "text": "I'm afraid I don't have any particular insights on that matter. Is there something else about the house you wish to know?"
        }
      }
    }
  ]
}
```

#### Dialogue Actions

For dialogue nodes that should trigger game events, add an `action` field with a unique identifier that the game code can interpret:

```json
"about_key": {
  "text": "The dining room key? Yes, I believe the master kept it in his study upstairs.",
  "defaultResponseId": "default_response",
  "action": "activate_quest_find_key"
}
```

### Enemies

Enemies provide combat challenges for the player.

```json
{
  "type": "enemies",
  "data": [
    {
      "id": "mansion_ghost",
      "name": "Spectral Servant",
      "description": "A translucent figure in servant's attire from a bygone era. It floats slightly above the ground, and its eyes glow with an unnatural blue light.",
      "health": 30,
      "maxHealth": 30,
      "attackPower": 5,
      "defense": 2,
      "experienceReward": 20,
      "goldReward": 0,
      "canFlee": true,
      "isAggressive": false,
      "deathMessage": "The spectral servant dissipates with a mournful wail, leaving behind a faint blue mist that quickly fades.",
      "dialog": {
        "encounter": "The spirit regards you with hollow eyes. 'Leave this place... you do not belong...'",
        "attack": "The spirit moans in pain as your attack disrupts its form.",
        "death": "You... cannot... save them..."
      },
      "loot": [
        {
          "itemId": "spectral_essence",
          "dropChance": 0.8
        },
        {
          "itemId": "silver_ring",
          "dropChance": 0.2
        }
      ],
      "specialAbilities": [
        {
          "name": "Chilling Touch",
          "description": "The ghost passes its hands through your body, sending a freezing shudder through your bones.",
          "damage": 8,
          "cooldown": 3
        }
      ],
      "tags": ["undead", "ghost", "mansion"]
    }
  ]
}
```

### Puzzles

Puzzles provide mental challenges and unlock new opportunities when solved.

```json
{
  "type": "puzzles",
  "data": [
    {
      "id": "chandelier_puzzle",
      "name": "The Fallen Chandelier",
      "description": "The once-grand chandelier has fallen and lies broken on the floor. If repaired and rehung, it might illuminate this gloomy hall.",
      "solutions": [
        {
          "type": "itemCombination",
          "requiredItems": ["rope", "chandelier_gear", "candles"],
          "consumeItems": true,
          "message": "You use the rope to hoist the chandelier back into position, replace the missing gear, and light the candles. The hall is suddenly bathed in warm light."
        }
      ],
      "hints": [
        "The chandelier looks like it needs to be hoisted back up somehow.",
        "You'll need something to hang it with, a missing mechanical part, and a way to light it.",
        "Perhaps a rope, a replacement gear, and some candles would help fix the chandelier."
      ],
      "onSolvedEffect": {
        "stateChange": "chandelier_fixed",
        "messageToPlayer": "The chandelier now hangs proudly from the ceiling, casting a warm glow throughout the hall. The room feels less oppressive now.",
        "rewardItemIds": ["brass_key"]
      }
    },
    {
      "id": "library_riddle",
      "name": "The Librarian's Riddle",
      "description": "On the desk is a note with a riddle: 'I speak without a mouth and hear without ears. I have no body, but I come alive with wind. What am I?'",
      "solutions": [
        {
          "type": "exactMatch",
          "answer": "echo",
          "ignoreCase": true,
          "message": "As you say 'echo', a hidden mechanism activates somewhere in the room."
        }
      ],
      "hints": [
        "The answer is something that repeats what it hears.",
        "Think about what happens when you shout in a canyon.",
        "The answer is 'echo'."
      ],
      "onSolvedEffect": {
        "unlockExitInLocation": {
          "locationId": "library",
          "exit": "down"
        },
        "messageToPlayer": "As you speak the word 'echo', a section of the bookshelf slides away, revealing a hidden staircase leading down."
      }
    }
  ]
}
```

### Quests

Quests guide the player through your game's narrative and objectives.

```json
{
  "type": "quests",
  "data": [
    {
      "id": "find_dining_key",
      "name": "The Missing Key",
      "description": "The butler mentioned that the dining room key is in the master's study upstairs, but the way is blocked. Find a way to access the study and retrieve the key.",
      "isActive": false,
      "isCompleted": false,
      "objectives": [
        {
          "id": "access_study",
          "description": "Find a way to access the master's study",
          "isCompleted": false
        },
        {
          "id": "find_key",
          "description": "Locate the dining room key",
          "isCompleted": false
        },
        {
          "id": "unlock_dining",
          "description": "Unlock the dining room",
          "isCompleted": false
        }
      ],
      "triggers": [
        {
          "type": "Location",
          "targetId": "master_study",
          "objectiveIds": ["access_study"]
        },
        {
          "type": "Take",
          "targetId": "dining_room_key",
          "objectiveIds": ["find_key"]
        },
        {
          "type": "UseOn",
          "targetId": "dining_room_key",
          "secondaryTargetId": "dining_room_door",
          "objectiveIds": ["unlock_dining"]
        }
      ],
      "rewards": {
        "experiencePoints": 50,
        "gold": 0,
        "itemIds": ["silver_spoon"],
        "onCompletionMessage": "You've unlocked the dining room, revealing a new area of the mansion to explore."
      }
    }
  ]
}
```

## Loading JSON Data

To use JSON data in the Text Adventure Framework, you'll need to modify the `ResourceLoader` class to load and parse the JSON files.

```csharp
using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

public class ResourceLoader
{
    private string _dataPath;
    private string _configPath;
    private GameConfig _config;
    
    public ResourceLoader(string configPath)
    {
        _configPath = configPath;
        _dataPath = Path.GetDirectoryName(configPath);
        LoadConfig();
    }
    
    private void LoadConfig()
    {
        string jsonText = File.ReadAllText(_configPath);
        _config = JsonSerializer.Deserialize<GameConfig>(jsonText);
    }
    
    public Dictionary<string, Location> LoadLocations()
    {
        var locations = new Dictionary<string, Location>();
        var locationFiles = Directory.GetFiles(Path.Combine(_dataPath, "locations"), "*.json");
        
        foreach (var file in locationFiles)
        {
            string jsonText = File.ReadAllText(file);
            var locationData = JsonSerializer.Deserialize<LocationDataFile>(jsonText);
            
            foreach (var locData in locationData.data)
            {
                var location = new Location
                {
                    Id = locData.id,
                    Name = locData.name,
                    Description = locData.description,
                    Region = locData.region ?? "General",
                    IsDiscovered = locData.isDiscovered
                };
                
                // Set up exits
                if (locData.exits != null)
                {
                    foreach (var exit in locData.exits)
                    {
                        location.Exits[exit.Key] = exit.Value;
                    }
                }
                
                // Set up locked exits
                if (locData.lockedExits != null)
                {
                    foreach (var lockedExit in locData.lockedExits)
                    {
                        var lockInfo = new LockInfo
                        {
                            IsLocked = lockedExit.Value.isLocked,
                            KeyItem = lockedExit.Value.keyItem,
                            LockedMessage = lockedExit.Value.lockedMessage
                        };
                        
                        location.LockedExits[lockedExit.Key] = lockInfo;
                    }
                }
                
                // Set up features
                if (locData.features != null)
                {
                    foreach (var feature in locData.features)
                    {
                        location.Features[feature.Key] = feature.Value;
                    }
                }
                
                // Set up time-based descriptions
                if (locData.timeBasedDescriptions != null)
                {
                    foreach (var timeDesc in locData.timeBasedDescriptions)
                    {
                        location.TimeBasedDescriptions[timeDesc.Key] = timeDesc.Value;
                    }
                }
                
                locations[location.Id] = location;
            }
        }
        
        return locations;
    }
    
    // Similar methods for loading items, characters, enemies, puzzles, etc.
    
    public string GetStartingLocationId()
    {
        return _config.startingLocationId;
    }
    
    public List<Item> LoadStartingInventory()
    {
        var inventory = new List<Item>();
        
        if (_config.startingInventory != null)
        {
            var allItems = LoadAllItems();
            
            foreach (var itemId in _config.startingInventory)
            {
                if (allItems.TryGetValue(itemId, out var item))
                {
                    inventory.Add(item);
                }
            }
        }
        
        return inventory;
    }
}

// Data model classes for deserializing JSON
public class GameConfig
{
    public string gameTitle { get; set; }
    public string version { get; set; }
    public string author { get; set; }
    public string startingLocationId { get; set; }
    public List<string> startingInventory { get; set; }
    public float timeScale { get; set; }
    public PlayerStartStats playerStartStats { get; set; }
}

public class PlayerStartStats
{
    public int health { get; set; }
    public int maxHealth { get; set; }
    public int gold { get; set; }
    public int strength { get; set; }
    public int dexterity { get; set; }
    public int intelligence { get; set; }
}

public class LocationDataFile
{
    public string type { get; set; }
    public List<LocationData> data { get; set; }
}

public class LocationData
{
    public string id { get; set; }
    public string name { get; set; }
    public string description { get; set; }
    public Dictionary<string, string> exits { get; set; }
    public Dictionary<string, LockedExitData> lockedExits { get; set; }
    public Dictionary<string, string> features { get; set; }
    public Dictionary<string, string> timeBasedDescriptions { get; set; }
    public List<string> itemIds { get; set; }
    public List<string> characterIds { get; set; }
    public List<string> enemyIds { get; set; }
    public List<string> puzzleIds { get; set; }
    public string region { get; set; }
    public bool isDiscovered { get; set; }
    public Dictionary<string, string> stateBasedFeatures { get; set; }
}

public class LockedExitData
{
    public bool isLocked { get; set; }
    public string keyItem { get; set; }
    public string lockedMessage { get; set; }
}

// Similar data model classes for items, characters, enemies, etc.
```

## Advanced JSON Techniques

### Referencing Between Files

To handle relationships between game entities, use consistent ID references:

```json
// In locations/library.json
{
  "id": "library",
  "itemIds": ["ancient_book", "quill_pen"],
  "characterIds": ["old_librarian"]
}

// In characters/scholars.json
{
  "id": "old_librarian",
  "name": "Professor Whitmore",
  "description": "An elderly scholar with spectacles perched on his nose."
}
```

### State-Dependent Content

Use conditional descriptions that change based on game state:

```json
"stateBasedFeatures": {
  "chandelier_fixed": "The chandelier now hangs majestically from the ceiling, its candles casting warm light throughout the room.",
  "ghost_appeased": "The room feels peaceful now, the oppressive atmosphere lifted."
}
```

### Event Triggers

Define specific events that should occur when players take certain actions:

```json
"triggers": [
  {
    "type": "Enter",
    "condition": "first_time_only",
    "message": "As you enter the library for the first time, you hear a faint whisper from somewhere among the bookshelves.",
    "action": "spawn_library_ghost"
  },
  {
    "type": "Examine",
    "targetId": "strange_book",
    "condition": "!book_already_read",
    "message": "As you open the book, a slip of paper falls out, revealing a mysterious code.",
    "action": "add_item_secret_code"
  }
]
```

## Example Game

Below is a mini-example of a JSON-based game with interconnected elements.

### Config

```json
// config.json
{
  "gameTitle": "The Haunted Library",
  "version": "1.0",
  "author": "Your Name",
  "startingLocationId": "library_entrance",
  "startingInventory": ["small_lantern", "library_card"],
  "timeScale": 60.0,
  "playerStartStats": {
    "health": 100,
    "maxHealth": 100,
    "gold": 5,
    "strength": 10,
    "dexterity": 10,
    "intelligence": 12
  }
}
```

### Locations

```json
// locations/library.json
{
  "type": "locations",
  "data": [
    {
      "id": "library_entrance",
      "name": "Library Entrance",
      "description": "The grand entrance to the Miskatonic University Library. Towering oak doors stand open beneath an arch of carved stone. The air smells of old books and dust.",
      "exits": {
        "north": "main_reading_room",
        "east": "librarian_desk",
        "south": "university_grounds"
      },
      "features": {
        "oak doors": "Massive oak doors with intricate carvings of books and scholars.",
        "stone arch": "The archway bears the library's motto: 'In ancient words, eternal light'."
      },
      "timeBasedDescriptions": {
        "morning": "Morning light streams through the windows, illuminating dancing dust motes in the air.",
        "night": "At night, the entrance is lit by gas lamps that cast long shadows across the floor."
      },
      "itemIds": ["welcome_pamphlet"],
      "characterIds": ["security_guard"],
      "isDiscovered": true
    },
    {
      "id": "restricted_section",
      "name": "Restricted Section",
      "description": "A dimly lit area with locked cabinets containing the library's rarest and most dangerous texts. The air feels heavy and unnaturally still.",
      "exits": {
        "south": "main_reading_room"
      },
      "lockedExits": {
        "west": {
          "isLocked": true,
          "keyItem": "ancient_key",
          "lockedMessage": "A heavy iron door blocks the way west. It has an unusual lock that seems incredibly old."
        }
      },
      "features": {
        "locked cabinets": "Glass-fronted cabinets secured with locks contain books bound in unusual materials. Some seem to shift slightly when not looked at directly.",
        "strange symbols": "The floor has a circular pattern of inlaid marble with symbols that resemble no known alphabet."
      },
      "isDiscovered": false,
      "itemIds": ["unusual_manuscript"],
      "enemyIds": ["library_ghost"],
      "puzzleIds": ["cabinet_puzzle"]
    }
  ]
}
```

### Items

```json
// items/library_items.json
{
  "type": "items",
  "data": [
    {
      "id": "welcome_pamphlet",
      "name": "Library Welcome Pamphlet",
      "description": "A small paper pamphlet that outlines the library rules and floor plan.",
      "canTake": true,
      "weight": 0.1,
      "category": "Document",
      "itemType": "Miscellaneous",
      "defaultUseResult": "You read the pamphlet again. It mentions a restricted section that requires special permission to access."
    },
    {
      "id": "ancient_key",
      "name": "Ancient Bronze Key",
      "description": "A heavy key made of tarnished bronze. Unusual symbols are engraved on its handle.",
      "canTake": true,
      "weight": 0.5,
      "category": "Key",
      "itemType": "Key",
      "useTargets": {
        "restricted_door": "You insert the ancient key into the lock. It turns with surprising ease, and the door swings open silently."
      }
    },
    {
      "id": "unusual_manuscript",
      "name": "Leather-bound Manuscript",
      "description": "A manuscript bound in what appears to be leather, but has an unsettling texture. The pages are filled with indecipherable writing and disturbing illustrations.",
      "canTake": true,
      "weight": 1.0,
      "category": "Book",
      "itemType": "QuestItem",
      "defaultUseResult": "As you open the manuscript, the strange symbols seem to shift and move on the page. You quickly close it, feeling slightly dizzy."
    }
  ]
}
```

### Characters

```json
// characters/library_staff.json
{
  "type": "characters",
  "data": [
    {
      "id": "security_guard",
      "name": "Night Guard Jenkins",
      "description": "An older man with a bushy white mustache wearing a navy uniform. He looks tired but alert.",
      "dialogue": {
        "greeting": {
          "text": "Evening. Library's closing soon. Can I help you find something before we lock up?",
          "responses": [
            {
              "pattern": "(restricted|section|rare books)",
              "nextDialogueId": "restricted_section"
            },
            {
              "pattern": "(ghost|haunted|strange|weird)",
              "nextDialogueId": "about_ghost"
            },
            {
              "pattern": "(closing|hours|time)",
              "nextDialogueId": "closing_time"
            },
            {
              "pattern": "(thanks|bye|goodbye|no)",
              "nextDialogueId": "farewell"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "restricted_section": {
          "text": "The restricted section? That's only accessible to faculty and students with special permission. You'd need to speak with the head librarian, Dr. Morgan, for that. But she's gone home for the day.",
          "responses": [
            {
              "pattern": "(where|find|morgan)",
              "nextDialogueId": "about_morgan"
            },
            {
              "pattern": "(key|sneak|access)",
              "nextDialogueId": "sneaking_in"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "about_ghost": {
          "text": "Ghost? *lowers voice* So you've heard the stories too. Look, I don't officially believe in that nonsense, but... I've worked here for twenty years and seen things I can't explain. Especially in the restricted section after dark.",
          "action": "activate_quest_investigate_ghost",
          "defaultResponseId": "default_response"
        },
        "about_morgan": {
          "text": "Dr. Morgan lives in the faculty housing on the east side of campus. But she won't be back until tomorrow morning.",
          "defaultResponseId": "default_response"
        },
        "sneaking_in": {
          "text": "*frowns* I wouldn't recommend trying to access the restricted section without permission. Security is tight, and some say there's more than just alarms protecting those books. *lowers voice* Not that I believe those stories.",
          "defaultResponseId": "default_response"
        },
        "closing_time": {
          "text": "We close at 9 PM sharp. I'll be making my final rounds in about half an hour, so don't get too engrossed in your reading.",
          "defaultResponseId": "default_response"
        },
        "farewell": {
          "text": "Have a good evening then. Don't stay too late."
        },
        "default_response": {
          "text": "Hmm, not sure about that. Is there something specific about the library you're interested in?"
        }
      }
    },
    {
      "id": "head_librarian",
      "name": "Dr. Morgan",
      "description": "A stern-looking woman in her fifties with silver-streaked black hair pulled into a tight bun. She wears wire-rimmed glasses and a formal tweed suit.",
      "dialogue": {
        "greeting": {
          "text": "Yes? How may I assist you? I'm rather busy cataloging these new acquisitions.",
          "responses": [
            {
              "pattern": "(restricted|section|access|permission)",
              "nextDialogueId": "request_access"
            },
            {
              "pattern": "(ghost|haunted|supernatural|strange)",
              "nextDialogueId": "deny_ghost"
            },
            {
              "pattern": "(manuscript|ancient text|unusual book)",
              "nextDialogueId": "about_manuscripts"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "request_access": {
          "text": "The restricted section? Out of the question. Those materials are only accessible to senior faculty and qualified researchers with proper credentials.",
          "responses": [
            {
              "pattern": "(research|study|scholar|qualified)",
              "nextDialogueId": "prove_qualification"
            },
            {
              "pattern": "(ghost|haunted|supernatural)",
              "nextDialogueId": "deny_ghost"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "prove_qualification": {
          "text": "If you're serious about gaining access, you would need to provide evidence of your research credentials and complete the proper paperwork. And frankly, I don't have time for that today.",
          "responses": [
            {
              "pattern": "(professor|letter|reference|evidence)",
              "nextDialogueId": "check_credentials"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "check_credentials": {
          "text": "Very well. If you have a letter of reference or credentials to show me, I might consider your request.",
          "action": "activate_quest_gain_access",
          "defaultResponseId": "default_response"
        },
        "deny_ghost": {
          "text": "*adjusts glasses* I have no patience for ridiculous ghost stories. This is a place of learning and scholarship, not superstition. If you heard such nonsense from the night guard, I'll be having a word with him.",
          "defaultResponseId": "default_response"
        },
        "default_response": {
          "text": "Is there something specific related to the library's collection you need? Otherwise, I must return to my work."
        }
      }
    }
  ]
}
```

### Enemies

```json
// enemies/library_entities.json
{
  "type": "enemies",
  "data": [
    {
      "id": "library_ghost",
      "name": "Scholar's Apparition",
      "description": "A translucent figure in Victorian-era clothing, hunched as if reading an invisible book. Its face is hidden in shadows beneath a scholarly cap.",
      "health": 40,
      "maxHealth": 40,
      "attackPower": 6,
      "defense": 3,
      "experienceReward": 25,
      "goldReward": 0,
      "canFlee": true,
      "isAggressive": false,
      "deathMessage": "The apparition shimmers and fades, leaving behind a whispered phrase that echoes in your mind: 'Find the truth...'",
      "dialog": {
        "encounter": "You disturb my research... why have you come to this forbidden place?",
        "attack": "Your attacks pass partially through the ghost, causing it to flicker like a faulty lamp.",
        "special": "Knowledge is power, but some knowledge is dangerous...",
        "death": "Perhaps you will succeed where I failed... look for the hidden page..."
      },
      "loot": [
        {
          "itemId": "spectral_bookmark",
          "dropChance": 1.0
        }
      ],
      "specialAbilities": [
        {
          "name": "Spectral Drain",
          "description": "The ghost reaches into your chest, causing an unnatural cold to spread through your body.",
          "damage": 10,
          "cooldown": 3
        }
      ],
      "tags": ["undead", "ghost", "scholar"]
    }
  ]
}
```

### Puzzles

```json
// puzzles/library_puzzles.json
{
  "type": "puzzles",
  "data": [
    {
      "id": "cabinet_puzzle",
      "name": "The Librarian's Cabinet",
      "description": "A specially-locked cabinet contains what appears to be an important manuscript. Above the cabinet, a riddle is carved: 'I have keys but no locks. I have space but no room. You can enter, but you can't go in. What am I?'",
      "solutions": [
        {
          "type": "exactMatch",
          "answer": "keyboard",
          "ignoreCase": true,
          "message": "As you say 'keyboard', you hear a click and the cabinet unlocks."
        },
        {
          "type": "exactMatch",
          "answer": "a keyboard",
          "ignoreCase": true,
          "message": "As you say 'a keyboard', you hear a click and the cabinet unlocks."
        }
      ],
      "hints": [
        "It might be related to something you use to input information.",
        "Think about something with keys that you might use with a computer.",
        "The answer is 'keyboard'."
      ],
      "onSolvedEffect": {
        "unlockItem": "unusual_manuscript",
        "messageToPlayer": "The cabinet door swings open, revealing an unusual manuscript bound in what appears to be leather, with strange symbols embossed on its cover."
      }
    },
    {
      "id": "ancient_door_puzzle",
      "name": "The Ancient Door",
      "description": "A stone door at the back of the restricted section has five rotating rings with strange symbols. A plaque nearby reads: 'Arrange the symbols to match the stars at solstice.'",
      "solutions": [
        {
          "type": "itemCombination",
          "requiredItems": ["star_chart", "ancient_key"],
          "consumeItems": false,
          "message": "Using the star chart as a guide, you arrange the symbols correctly and insert the ancient key. The door grinds open slowly."
        }
      ],
      "hints": [
        "You might need some sort of astronomical reference.",
        "The symbols seem to represent constellations in different positions.",
        "A star chart might help you align the symbols correctly."
      ],
      "onSolvedEffect": {
        "unlockExitInLocation": {
          "locationId": "restricted_section",
          "exit": "west"
        },
        "messageToPlayer": "The massive stone door slides open with a deep rumbling sound, revealing a hidden chamber beyond."
      }
    }
  ]
}
```

### Quests

```json
// quests/library_quests.json
{
  "type": "quests",
  "data": [
    {
      "id": "investigate_ghost",
      "name": "The Scholar's Ghost",
      "description": "The night guard mentioned strange occurrences in the restricted section after dark. Investigate to determine if there really is a supernatural presence in the library.",
      "isActive": false,
      "isCompleted": false,
      "objectives": [
        {
          "id": "access_restricted",
          "description": "Gain access to the restricted section",
          "isCompleted": false
        },
        {
          "id": "find_ghost",
          "description": "Search the restricted section at night",
          "isCompleted": false
        },
        {
          "id": "communicate",
          "description": "Find a way to communicate with the ghost",
          "isCompleted": false
        }
      ],
      "triggers": [
        {
          "type": "Location",
          "targetId": "restricted_section",
          "objectiveIds": ["access_restricted"]
        },
        {
          "type": "Location",
          "targetId": "restricted_section",
          "conditions": {
            "timeOfDay": "night"
          },
          "objectiveIds": ["find_ghost"]
        },
        {
          "type": "UseOn",
          "targetId": "ancient_tome",
          "secondaryTargetId": "library_ghost",
          "objectiveIds": ["communicate"]
        }
      ],
      "rewards": {
        "experiencePoints": 100,
        "gold": 0,
        "itemIds": ["spectral_lens"],
        "onCompletionMessage": "You've discovered the truth behind the library's ghost and helped put a restless spirit at ease."
      }
    },
    {
      "id": "gain_access",
      "name": "Academic Credentials",
      "description": "To access the restricted section, you'll need to convince Dr. Morgan of your scholarly credentials.",
      "isActive": false,
      "isCompleted": false,
      "objectives": [
        {
          "id": "find_professor",
          "description": "Find a professor who might vouch for you",
          "isCompleted": false
        },
        {
          "id": "get_letter",
          "description": "Obtain a letter of recommendation",
          "isCompleted": false
        },
        {
          "id": "show_credentials",
          "description": "Present your credentials to Dr. Morgan",
          "isCompleted": false
        }
      ],
      "triggers": [
        {
          "type": "Talk",
          "targetId": "professor_williams",
          "objectiveIds": ["find_professor"]
        },
        {
          "type": "Take",
          "targetId": "recommendation_letter",
          "objectiveIds": ["get_letter"]
        },
        {
          "type": "UseOn",
          "targetId": "recommendation_letter",
          "secondaryTargetId": "head_librarian",
          "objectiveIds": ["show_credentials"]
        }
      ],
      "rewards": {
        "experiencePoints": 50,
        "gold": 0,
        "onCompletionMessage": "Dr. Morgan has granted you access to the restricted section. You can now explore its forbidden knowledge."
      }
    }
  ]
}
```

## Best Practices

### Organization

1. **Split your content logically**: Divide your JSON files based on content type and region or theme. For a large game, you might have:
   - `locations/mansion_ground_floor.json`
   - `locations/mansion_upper_floor.json`
   - `characters/mansion_servants.json`
   - `enemies/undead_enemies.json`

2. **Use consistent naming conventions**:
   - Use snake_case for IDs (e.g., `ancient_book`, `master_bedroom`)
   - Keep IDs short but descriptive
   - Group related items with common prefixes (e.g., `key_basement`, `key_attic`)

3. **Create content libraries**: For reusable elements like common items or standard enemies, create library files.

### Data Structure

1. **Maintain ID uniqueness**: Ensure each entity has a globally unique ID.

2. **Validate your JSON**: Use a JSON validator to check for syntax errors.

3. **Document your schema**: Create schema files or documentation for your data formats.

4. **Use default values**: In your code, provide sensible defaults for optional JSON properties.

### Content Creation

1. **Start small**: Create a minimal viable game first, then expand.

2. **Test as you go**: After adding new content, playtest to ensure everything connects properly.

3. **Create content templates**: Make templates for different entity types to maintain consistency.

4. **Balance detail and brevity**: Be descriptive but concise in your descriptions.

5. **Create internal links**: Reference other game elements in your descriptions to create a connected world.

### Examples of Good Descriptions

**Location description (detailed but concise):**
```json
"description": "Mahogany bookshelves line the walls from floor to ceiling, while a massive oak table dominates the center of the room. The smell of leather-bound books and pipe tobacco fills the air. A grandfather clock ticks solemnly in the corner."
```

**Object description (evocative with implied history):**
```json
"description": "A silver-plated pocket watch with intricate engravings. The glass is cracked, and the hands are frozen at 3:42. An inscription on the back reads 'To James, Time is our greatest treasure.'"
```

**Character dialogue (natural with personality):**
```json
"text": "Well, look what the cat dragged in. *adjusts spectacles* I haven't seen a new face in this dusty old place for months. What brings you to our little corner of forgotten knowledge?"
```

## Troubleshooting

### Common JSON Errors

1. **Missing Commas**: Each item in an array or property in an object needs to be separated by commas.
   ```json
   // Incorrect
   {
     "name": "Library Key"
     "description": "A small brass key."
   }
   
   // Correct
   {
     "name": "Library Key",
     "description": "A small brass key."
   }
   ```

2. **Trailing Commas**: JSON doesn't allow a comma after the last item.
   ```json
   // Incorrect
   {
     "name": "Library Key",
     "description": "A small brass key",
   }
   
   // Correct
   {
     "name": "Library Key",
     "description": "A small brass key"
   }
   ```

3. **Mismatched Brackets**: Ensure all opening brackets have matching closing brackets.

4. **Incorrect Quotes**: JSON requires double quotes for strings and property names.
   ```json
   // Incorrect
   {
     'name': 'Library Key'
   }
   
   // Correct
   {
     "name": "Library Key"
   }
   ```

### Common Implementation Problems

1. **Broken References**: If an item, character, or enemy referenced by ID doesn't exist.
   - Solution: Implement validation to check all referenced IDs actually exist.

2. **Circular Dependencies**: Locations reference each other in a way that creates a cycle.
   - Solution: Ensure exits properly connect in both directions.

3. **Missing Required Properties**: Some properties might be essential for game logic.
   - Solution: Define required fields and provide default values for optional ones.

4. **Case Sensitivity**: IDs referenced with different capitalization.
   - Solution: Standardize on lowercase for all IDs and use case-insensitive comparisons.

5. **Special Characters**: Some characters might need escaping in JSON strings.
   - Solution: Use a JSON serializer rather than manually constructing JSON.

### Debugging Tips

1. **JSON Validators**: Use online tools like JSONLint to validate syntax.

2. **Incremental Testing**: Add content in small batches and test after each addition.

3. **Create Data Visualizers**: Develop tools to visualize relationships between game entities.

4. **Console Logging**: When loading data, log important details to help spot issues.

5. **Custom Diagnostics**: Add a developer mode with commands to inspect game state.

## Conclusion

Using JSON for your text adventure game content offers flexibility, maintainability, and accessibility to non-programmers. By separating your content from your code, you can focus on creating rich, engaging worlds while allowing writers and designers to contribute without needing to understand C# programming.

Remember to start small, maintain consistent structures, and test frequently. As your game grows, your organization systems will become increasingly important, so establish good practices early.

With the Text Adventure Framework's robust engine and well-structured JSON data, you have everything you need to create compelling interactive fiction experiences.