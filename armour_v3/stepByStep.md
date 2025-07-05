# Creating Your First Text Adventure Game: A Step-by-Step Guide

This beginner-friendly guide will walk you through creating a text adventure game using the Text Adventure Framework with JSON data files. By the end, you'll have a simple but complete game that you can expand on your own.

## Table of Contents

1. [Setting Up Your Project](#setting-up-your-project)
2. [Creating the Basic File Structure](#creating-the-basic-file-structure)
3. [Setting Up the Configuration](#setting-up-the-configuration)
4. [Creating Your First Location](#creating-your-first-location)
5. [Adding Items](#adding-items)
6. [Creating Characters and Dialogue](#creating-characters-and-dialogue)
7. [Adding Puzzles](#adding-puzzles)
8. [Creating Enemies](#creating-enemies)
9. [Setting Up Quests](#setting-up-quests)
10. [Testing Your Game](#testing-your-game)
11. [Expanding Your Game](#expanding-your-game)
12. [Common Issues and Solutions](#common-issues-and-solutions)

## Setting Up Your Project

Let's start by setting up a new Godot project for your text adventure.

### Step 1: Create a New Godot Project

1. Open Godot Engine
2. Click "New Project"
3. Enter a project name (e.g., "MyTextAdventure")
4. Choose a directory for your project
5. Select "2D" as the renderer
6. Click "Create & Edit"

### Step 2: Import the Framework Scripts

1. Download the Text Adventure Framework scripts (or copy them from your existing project)
2. In Godot, create a new folder called "src" in your project directory
3. Copy all the framework scripts into this folder (AdvancedCLIEmulator.cs, CommandProcessor.cs, etc.)

### Step 3: Create the Basic Scenes

1. Create a new scene (Scene > New Scene)
2. Select "Node" as the root node type
3. Rename it to "Main"
4. Save it as "res://Main.tscn"

Now, let's add the CLI interface:

1. Add a child node to Main (Right-click > Add Child Node)
2. Search for "Control" and add it
3. Rename it to "CLIEmulator"
4. In the Inspector panel, attach a script to it (Click the "Script" button)
5. Create a new script named "CLIEmulator.cs"
6. Replace the contents with the code from AdvancedCLIEmulator.cs

Next, create the required subscenes:

1. Create a new scene with a RichTextLabel as root node
2. Save it as "res://scenes/add_line.tscn"
3. Create another scene with a LineEdit as root node
4. Save it as "res://scenes/command_prompt.tscn"

## Creating the Basic File Structure

Now let's set up the directory structure for your game data files.

### Step 1: Create Data Directories

1. In your project directory, create a new folder called "game_data"
2. Inside "game_data", create the following subfolders:
   - locations
   - items
   - characters
   - enemies
   - puzzles
   - quests

Your project structure should look like this:

```
MyTextAdventure/
├── src/
│   ├── AdvancedCLIEmulator.cs
│   ├── CommandProcessor.cs
│   ├── GameState.cs
│   └── ...
├── scenes/
│   ├── add_line.tscn
│   └── command_prompt.tscn
├── game_data/
│   ├── config.json
│   ├── locations/
│   ├── items/
│   ├── characters/
│   ├── enemies/
│   ├── puzzles/
│   └── quests/
└── Main.tscn
```

## Setting Up the Configuration

Let's create the main configuration file for your game.

### Step 1: Create the Config File

Create a new file in the "game_data" folder named "config.json" with the following content:

```json
{
  "gameTitle": "My First Adventure",
  "version": "1.0",
  "author": "Your Name",
  "startingLocationId": "cabin",
  "startingInventory": ["flashlight", "notebook"],
  "timeScale": 60.0,
  "playerStartStats": {
    "health": 100,
    "maxHealth": 100,
    "gold": 10,
    "strength": 10,
    "dexterity": 10,
    "intelligence": 10
  }
}
```

This configures the basic settings for your game, including:
- The game title and version
- Your name as the author
- Where the player starts (we'll create this location next)
- What items the player starts with
- Basic player statistics

## Creating Your First Location

Now let's create your first game location - a small cabin in the woods.

### Step 1: Create the First Location File

Create a new file in the "game_data/locations" folder named "starting_area.json" with the following content:

```json
{
  "type": "locations",
  "data": [
    {
      "id": "cabin",
      "name": "Abandoned Cabin",
      "description": "A small, wooden cabin with dust-covered furniture. Sunlight filters through cracks in the boarded-up windows, revealing dancing dust particles in the air. The cabin has clearly been abandoned for some time.",
      "exits": {
        "north": "forest_path",
        "east": "cabin_bedroom"
      },
      "features": {
        "windows": "The windows are boarded up, but thin beams of light manage to slip through the cracks.",
        "furniture": "A simple wooden table and chair sit in the center of the room. A thick layer of dust covers everything.",
        "fireplace": "A stone fireplace dominates the north wall. Cold ashes lie within, suggesting it hasn't been used in months."
      },
      "timeBasedDescriptions": {
        "morning": "Morning light streams through the cracks in the boarded windows, illuminating the dusty interior of the cabin.",
        "afternoon": "The cabin is well-lit by afternoon sunlight coming through the cracks in the wooden boards.",
        "evening": "Long shadows stretch across the cabin floor as evening light filters in.",
        "night": "The cabin is dark, with only faint moonlight slipping through the cracks in the boarded windows."
      },
      "itemIds": ["old_key", "dusty_book"],
      "isDiscovered": true
    },
    {
      "id": "cabin_bedroom",
      "name": "Cabin Bedroom",
      "description": "A small bedroom with a simple bed covered in a faded quilt. A wooden dresser stands against one wall, and a cracked mirror hangs above it.",
      "exits": {
        "west": "cabin"
      },
      "features": {
        "bed": "A simple bed with a faded blue quilt. It's been neatly made, as if waiting for someone to return.",
        "dresser": "An old wooden dresser with three drawers. The wood is weather-worn but still sturdy.",
        "mirror": "A cracked mirror that distorts your reflection. Something about it makes you uneasy."
      },
      "itemIds": ["silver_locket"],
      "isDiscovered": false
    },
    {
      "id": "forest_path",
      "name": "Forest Path",
      "description": "A narrow dirt path winding through tall pine trees. The forest is quiet except for the occasional bird call and the rustling of leaves in the breeze.",
      "exits": {
        "south": "cabin",
        "north": "forest_clearing"
      },
      "features": {
        "trees": "Tall pine trees tower overhead, their branches creating a canopy that filters the sunlight.",
        "path": "The dirt path is barely visible in places, nearly reclaimed by the forest undergrowth.",
        "undergrowth": "Ferns and wild berries grow along the sides of the path."
      },
      "timeBasedDescriptions": {
        "morning": "Dew clings to the undergrowth as morning light filters through the pine branches.",
        "afternoon": "Dappled sunlight plays across the forest floor, creating shifting patterns of light and shadow.",
        "evening": "Long shadows stretch across the path as the sun sinks lower, turning the forest a deep golden hue.",
        "night": "The forest is dark and mysterious, with moonlight creating eerie shadows between the trees."
      },
      "characterIds": ["old_hunter"],
      "isDiscovered": false
    },
    {
      "id": "forest_clearing",
      "name": "Forest Clearing",
      "description": "A circular clearing surrounded by dense forest. Wildflowers dot the grassy ground, and a large, flat rock sits in the center like a natural altar.",
      "exits": {
        "south": "forest_path"
      },
      "lockedExits": {
        "east": {
          "isLocked": true,
          "keyItem": "old_key",
          "lockedMessage": "A thicket of brambles blocks the path east. You notice what appears to be a small gate behind the thorny brush, but it's secured with a rusty lock."
        }
      },
      "features": {
        "rock": "A large, flat rock sits in the center of the clearing. Strange symbols have been carved into its surface, weathered by time.",
        "wildflowers": "Colorful wildflowers grow throughout the clearing, attracting butterflies and bees.",
        "symbols": "The symbols on the rock appear to be very old, possibly some form of ancient writing or magical runes."
      },
      "enemyIds": ["forest_wolf"],
      "puzzleIds": ["stone_riddle"],
      "isDiscovered": false
    }
  ]
}
```

This creates a small game world with four locations:
1. **Abandoned Cabin** - The starting location
2. **Cabin Bedroom** - A room connected to the main cabin
3. **Forest Path** - A path leading from the cabin into the forest
4. **Forest Clearing** - A clearing with a locked exit and a puzzle

Notice how we're referencing items, characters, enemies, and puzzles that we haven't created yet. We'll add those next.

## Adding Items

Now let's create the items for your game world.

### Step 1: Create the Items File

Create a new file in the "game_data/items" folder named "basic_items.json" with the following content:

```json
{
  "type": "items",
  "data": [
    {
      "id": "flashlight",
      "name": "Flashlight",
      "description": "A sturdy metal flashlight with a bright beam. It seems to be in good working condition.",
      "canTake": true,
      "weight": 0.5,
      "category": "Tool",
      "itemType": "Miscellaneous",
      "defaultUseResult": "You switch on the flashlight, illuminating the area around you with a bright beam of light."
    },
    {
      "id": "notebook",
      "name": "Small Notebook",
      "description": "A small, leather-bound notebook with blank pages. Perfect for jotting down important information.",
      "canTake": true,
      "weight": 0.2,
      "category": "Tool",
      "itemType": "Miscellaneous",
      "defaultUseResult": "You flip through the notebook. The pages are blank, waiting for you to write something."
    },
    {
      "id": "old_key",
      "name": "Rusty Key",
      "description": "An old iron key, covered in rust. Despite its age, it looks like it might still work in the right lock.",
      "canTake": true,
      "weight": 0.1,
      "category": "Key",
      "itemType": "Key",
      "useTargets": {
        "east": "You insert the rusty key into the lock. With some effort, it turns, and the small gate creaks open, revealing a path beyond the brambles."
      }
    },
    {
      "id": "dusty_book",
      "name": "Dusty Book",
      "description": "An old book with a leather cover, its pages yellowed with age. The title reads 'Local Folklore and Legends'.",
      "canTake": true,
      "weight": 1.0,
      "category": "Book",
      "itemType": "Miscellaneous",
      "defaultUseResult": "You flip through the book, finding stories about local legends. One page catches your eye, describing a hidden temple in these very woods, said to contain a powerful artifact."
    },
    {
      "id": "silver_locket",
      "name": "Silver Locket",
      "description": "A small silver locket on a delicate chain. It's tarnished but still beautiful. The locket contains a tiny portrait of a young woman.",
      "canTake": true,
      "weight": 0.1,
      "category": "Jewelry",
      "itemType": "QuestItem",
      "defaultUseResult": "You open the locket and look at the portrait inside. The young woman has a gentle smile, though her eyes seem sad somehow."
    },
    {
      "id": "healing_herb",
      "name": "Healing Herb",
      "description": "A small plant with distinctive blue flowers. It's known for its medicinal properties.",
      "canTake": true,
      "weight": 0.1,
      "category": "Consumable",
      "itemType": "Healing",
      "useValue": 20,
      "defaultUseResult": "You chew on the healing herb. It tastes bitter but refreshing, and you feel your wounds beginning to heal."
    },
    {
      "id": "ancient_coin",
      "name": "Ancient Coin",
      "description": "A coin made of an unusual golden metal. One side bears the image of a temple, the other a strange symbol similar to those on the rock in the clearing.",
      "canTake": true,
      "weight": 0.1,
      "category": "Valuable",
      "itemType": "QuestItem"
    }
  ]
}
```

This creates several items that the player can find and interact with in your game:
1. **Flashlight** - A starting item
2. **Notebook** - Another starting item
3. **Rusty Key** - Used to unlock the path in the forest clearing
4. **Dusty Book** - Provides clues about the game world
5. **Silver Locket** - A quest item
6. **Healing Herb** - Restores health when used
7. **Ancient Coin** - Another quest item (will appear later)

## Creating Characters and Dialogue

Now, let's add a character to your game world.

### Step 1: Create the Characters File

Create a new file in the "game_data/characters" folder named "forest_characters.json" with the following content:

```json
{
  "type": "characters",
  "data": [
    {
      "id": "old_hunter",
      "name": "Old Hunter",
      "description": "An elderly man with a weathered face and sharp eyes. He wears practical woodsman's clothes and carries a worn but well-maintained hunting bow.",
      "dialogue": {
        "greeting": {
          "text": "Well, hello there. Don't get many visitors in these parts. What brings you to these woods, stranger?",
          "responses": [
            {
              "pattern": "(lost|where|location|place)",
              "nextDialogueId": "about_location"
            },
            {
              "pattern": "(you|hunter|yourself|name)",
              "nextDialogueId": "about_self"
            },
            {
              "pattern": "(cabin|house|building)",
              "nextDialogueId": "about_cabin"
            },
            {
              "pattern": "(temple|artifact|treasure|hidden)",
              "nextDialogueId": "about_temple"
            },
            {
              "pattern": "(wolf|wolves|animal|beast)",
              "nextDialogueId": "about_wolves"
            },
            {
              "pattern": "(bye|goodbye|farewell|leave)",
              "nextDialogueId": "farewell"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "about_location": {
          "text": "You're in the northern woods, about half a day's walk from the nearest village. Been getting wilder out here lately... strange things happening.",
          "responses": [
            {
              "pattern": "(strange|things|happening)",
              "nextDialogueId": "about_strange"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "about_self": {
          "text": "Me? Name's Garret. Been hunting these woods for nigh on forty years. Know every trail and stream, or used to... things have been changing.",
          "defaultResponseId": "default_response"
        },
        "about_cabin": {
          "text": "That old cabin? Belonged to a scholar who came to study the old ruins. Left suddenly about a month ago, haven't seen him since. Strange fellow, always muttering about symbols and ancient power.",
          "responses": [
            {
              "pattern": "(scholar|man|fellow)",
              "nextDialogueId": "about_scholar"
            },
            {
              "pattern": "(ruins|ancient|old)",
              "nextDialogueId": "about_ruins"
            }
          ],
          "defaultResponseId": "default_response"
        },
        "about_temple": {
          "text": "*looks at you sharply* The hidden temple? Most folks think it's just a legend. But I've seen... things. Through the bramble thicket east of the clearing, there's a path. Never dared go far down it myself.",
          "action": "activate_quest_find_temple",
          "defaultResponseId": "default_response"
        },
        "about_wolves": {
          "text": "Be careful of the wolves in these parts. They've been acting strange lately - more aggressive, hunting in places they never used to. Something's got them riled up.",
          "defaultResponseId": "default_response"
        },
        "about_strange": {
          "text": "Animals behaving oddly. Strange lights at night. And the old ruins seem to be... waking up somehow. I'd be careful if I were you, especially near that clearing with the carved rock.",
          "defaultResponseId": "default_response"
        },
        "about_scholar": {
          "text": "Quiet fellow named Thorne. Spent most of his time reading old books and taking notes. Real interested in the local legends, especially about the hidden temple.",
          "defaultResponseId": "default_response"
        },
        "about_ruins": {
          "text": "There are old stone structures scattered throughout these woods. Most just foundations and broken columns now. But legend says there's a intact temple hidden somewhere, containing a powerful artifact.",
          "defaultResponseId": "default_response"
        },
        "farewell": {
          "text": "Take care of yourself in these woods, stranger. They're not as safe as they once were."
        },
        "default_response": {
          "text": "Hmm, can't say I know much about that. Anything else you're wondering about these woods?"
        }
      }
    }
  ]
}
```

This creates an old hunter character who provides information about the game world and can activate a quest. His dialogue is structured to respond to different topics the player might ask about.

## Adding Puzzles

Now let's add a puzzle to your game.

### Step 1: Create the Puzzles File

Create a new file in the "game_data/puzzles" folder named "forest_puzzles.json" with the following content:

```json
{
  "type": "puzzles",
  "data": [
    {
      "id": "stone_riddle",
      "name": "The Stone Riddle",
      "description": "As you examine the stone more closely, you notice that among the carved symbols is a riddle in an ancient but still readable script: 'I speak without a mouth and hear without ears. I have no body, but I come alive with wind. What am I?'",
      "solutions": [
        {
          "type": "exactMatch",
          "answer": "echo",
          "ignoreCase": true,
          "message": "As you speak the word 'echo', the symbols on the stone begin to glow with a soft blue light. A compartment slides open in the rock, revealing something inside."
        },
        {
          "type": "exactMatch",
          "answer": "an echo",
          "ignoreCase": true,
          "message": "As you speak the words 'an echo', the symbols on the stone begin to glow with a soft blue light. A compartment slides open in the rock, revealing something inside."
        },
        {
          "type": "exactMatch",
          "answer": "the echo",
          "ignoreCase": true,
          "message": "As you speak the words 'the echo', the symbols on the stone begin to glow with a soft blue light. A compartment slides open in the rock, revealing something inside."
        }
      ],
      "hints": [
        "The answer is something that repeats what it hears.",
        "Think about what happens when you shout in a canyon or a cave.",
        "The answer is 'echo'."
      ],
      "onSolvedEffect": {
        "messageToPlayer": "The compartment contains an ancient coin made of an unusual golden metal. One side bears the image of a temple, the other a strange symbol matching those on the rock.",
        "rewardItemIds": ["ancient_coin"],
        "stateChange": "stone_puzzle_solved"
      }
    }
  ]
}
```

This creates a riddle puzzle on the stone in the forest clearing. When solved, it reveals an ancient coin that can be used in a later part of the game.

## Creating Enemies

Now, let's add an enemy to your game.

### Step 1: Create the Enemies File

Create a new file in the "game_data/enemies" folder named "forest_enemies.json" with the following content:

```json
{
  "type": "enemies",
  "data": [
    {
      "id": "forest_wolf",
      "name": "Gray Wolf",
      "description": "A large gray wolf with piercing yellow eyes. Its fur is matted, and it appears unusually aggressive for a wild wolf.",
      "health": 30,
      "maxHealth": 30,
      "attackPower": 5,
      "defense": 2,
      "experienceReward": 20,
      "goldReward": 0,
      "canFlee": true,
      "isAggressive": true,
      "deathMessage": "The wolf collapses to the ground with a final whimper. As you approach cautiously, you notice an unusual marking on its fur - almost like a brand or symbol.",
      "dialog": {
        "encounter": "The wolf growls menacingly, baring its teeth. There's something unnatural about its behavior.",
        "attack": "The wolf yelps in pain but continues its aggressive stance.",
        "death": "The wolf lets out a final whimper before falling still."
      },
      "loot": [
        {
          "itemId": "healing_herb",
          "dropChance": 0.5
        }
      ],
      "specialAbilities": [
        {
          "name": "Savage Bite",
          "description": "The wolf lunges forward with incredible speed, its jaws clamping down with supernatural strength.",
          "damage": 8,
          "cooldown": 3
        }
      ],
      "tags": ["animal", "wolf", "forest"]
    }
  ]
}
```

This creates a wolf enemy that the player might encounter in the forest clearing. The wolf can drop a healing herb when defeated.

## Setting Up Quests

Finally, let's add a quest to your game.

### Step 1: Create the Quests File

Create a new file in the "game_data/quests" folder named "main_quests.json" with the following content:

```json
{
  "type": "quests",
  "data": [
    {
      "id": "find_temple",
      "name": "The Hidden Temple",
      "description": "The old hunter mentioned a hidden temple in the forest, beyond the bramble thicket east of the clearing. Find a way through and discover what lies beyond.",
      "isActive": false,
      "isCompleted": false,
      "objectives": [
        {
          "id": "unlock_path",
          "description": "Find a way through the bramble thicket",
          "isCompleted": false
        },
        {
          "id": "find_temple_entrance",
          "description": "Locate the hidden temple",
          "isCompleted": false
        }
      ],
      "triggers": [
        {
          "type": "UseOn",
          "targetId": "old_key",
          "secondaryTargetId": "east",
          "objectiveIds": ["unlock_path"]
        }
      ],
      "rewards": {
        "experiencePoints": 50,
        "gold": 0,
        "itemIds": [],
        "onCompletionMessage": "You've successfully found a way through the brambles! The path beyond leads deeper into the forest, and you can see ancient stone pillars in the distance..."
      }
    }
  ]
}
```

This creates a quest that is activated when talking to the old hunter about the temple. The player completes the first objective by using the rusty key on the locked exit in the forest clearing.

## Testing Your Game

Now that you've created all the necessary JSON files, let's set up the code to load them and test your game.

### Step 1: Modify your Main Script

Create a new script attached to your Main node (if you haven't already) with the following code:

```csharp
using Godot;
using System;

public partial class Main : Node
{
    private AdvancedCLIEmulator _cliEmulator;
    private ResourceLoader _resourceLoader;
    private GameState _gameState;
    private CommandProcessor _commandProcessor;
    
    public override void _Ready()
    {
        // Get the CLI emulator
        _cliEmulator = GetNode<AdvancedCLIEmulator>("CLIEmulator");
        
        // Initialize the resource loader
        _resourceLoader = new ResourceLoader("res://game_data/config.json");
        
        // Create game state
        _gameState = new GameState(_resourceLoader);
        
        // Create command processor
        _commandProcessor = new CommandProcessor(_gameState);
        
        // Display welcome message
        _cliEmulator.DisplayOutput($"Welcome to {_resourceLoader.GetGameTitle()}!", Colors.Yellow);
        _cliEmulator.DisplayOutput("Type 'help' for a list of commands.");
        
        // Display the starting location
        _cliEmulator.DisplayOutput(_gameState.GetCurrentLocationDescription());
    }
    
    public override void _Process(double delta)
    {
        // Update game state
        _gameState.Update((float)delta);
        
        // Check for any pending messages
        var pendingMessages = _gameState.GetAndClearPendingMessages();
        foreach (var message in pendingMessages)
        {
            _cliEmulator.DisplayOutput(message);
        }
    }
}
```

This script initializes all the necessary components of the framework and displays the starting information to the player.

### Step 2: Run and Test

1. Make sure your Main.tscn scene is set as the main scene (Project > Project Settings > Application > Run > Main Scene)
2. Run the game (F5 or the Play button)
3. Try out different commands:
   - `look` - To see the current location
   - `examine furniture` - To look at specific features
   - `go east` - To move to the bedroom
   - `take dusty_book` - To pick up the book
   - `inventory` - To see what you're carrying
   - `use dusty_book` - To read the book

## Expanding Your Game

Now that you have a basic game working, here are some ways to expand it:

### Add More Locations

Create more JSON files in the "locations" folder to expand your game world. Add new areas beyond the locked path in the forest clearing.

### Create More Complex Puzzles

Try creating puzzles that require item combinations or specific conditions to solve.

```json
{
  "id": "ancient_door",
  "name": "Temple Entrance",
  "description": "A massive stone door blocks the entrance to the temple. There is a circular indentation in the center that seems designed to hold something.",
  "solutions": [
    {
      "type": "itemCombination",
      "requiredItems": ["ancient_coin"],
      "consumeItems": false,
      "message": "You place the ancient coin in the circular indentation. It fits perfectly. The door rumbles and slowly slides open, revealing the dark interior of the temple."
    }
  ]
}
```

### Add More Characters and Dialogue

Expand your world with more NPCs that can provide information, quests, or items.

### Implement a Main Quest Line

Create a series of connected quests that form a complete story arc. For example:
1. Find the hidden temple
2. Retrieve the artifact from the temple
3. Discover the truth about the scholar
4. Confront the final challenge

## Common Issues and Solutions

### Items Not Appearing

**Problem**: You've added items to a location but can't see them when you look.

**Solution**: Check that:
1. The item ID in the location's "itemIds" list matches the actual item ID
2. The item is correctly defined in your items JSON file
3. You're loading the item file in your ResourceLoader

### Commands Not Working

**Problem**: Some commands don't seem to work as expected.

**Solution**: 
1. Check the console for error messages
2. Verify that all required components are initialized properly
3. Make sure your command is correctly formatted (e.g., `examine book` instead of `look at book`)

### Puzzles Not Solving

**Problem**: You've entered what you think is the correct solution, but the puzzle won't solve.

**Solution**:
1. Check the exact wording of your solution in the JSON file
2. Make sure you're using the `solve` command correctly (e.g., `solve stone_riddle echo`)
3. Verify that the puzzle is correctly loaded and attached to the location

### Game Crashes

**Problem**: The game crashes when trying to load or access certain content.

**Solution**:
1. Check your JSON syntax for errors (use a JSON validator)
2. Make sure all referenced IDs exist (e.g., items, locations, characters)
3. Verify that all required files are in the correct locations

## Conclusion

Congratulations! You've created your first text adventure game using the Text Adventure Framework with JSON data files. This approach allows you to focus on creating interesting content without having to worry about the underlying code.

As you continue to develop your game, remember:
- Keep your JSON files organized and well-structured
- Test frequently to catch issues early
- Build your game world gradually, testing each new addition
- Don't be afraid to experiment with complex puzzles, dialogues, and quest chains

Happy game developing!