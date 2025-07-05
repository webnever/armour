using Godot;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

/// <summary>
/// Responsible for loading game data from a unified JSON file into game objects
/// </summary>
public class GameResourceLoader
{
    private string _dataPath;
    private string _gameDataPath;
    private GameConfig _config;
    private UnifiedGameData _gameData;

    // Cache for loaded data
    private Dictionary<string, Item> _allItems = new Dictionary<string, Item>();
    private Dictionary<string, Character> _allCharacters = new Dictionary<string, Character>();
    private Dictionary<string, Enemy> _allEnemies = new Dictionary<string, Enemy>();
    private List<Quest> _allQuests = new List<Quest>();

    /// <summary>
    /// Constructor that initializes the GameResourceLoader with game data path
    /// </summary>
    /// <param name="gameDataPath">Path to the unified game data file</param>
    public GameResourceLoader(string gameDataPath)
    {
        // Handle legacy config.json path by converting to game_data.json
        if (gameDataPath.EndsWith("config.json"))
        {
            var directory = Path.GetDirectoryName(gameDataPath);
            _gameDataPath = Path.Combine(directory, "game_data.json");
            GD.Print($"Converting legacy config path to: {_gameDataPath}");
        }
        else
        {
            _gameDataPath = gameDataPath;
        }
        
        _dataPath = Path.GetDirectoryName(_gameDataPath);
        
        LoadUnifiedGameData();

        // Pre-load all data
        LoadAllItems();
        LoadAllCharacters();
        LoadAllEnemies();
        LoadAllQuests();
    }

    /// <summary>
    /// Loads the unified game data file including config
    /// </summary>
    private void LoadUnifiedGameData()
    {
        try
        {
            using var file = Godot.FileAccess.Open(_gameDataPath, Godot.FileAccess.ModeFlags.Read);
            if (file == null)
            {
                GD.PrintErr($"Could not open unified game data file: {_gameDataPath}. Error: {Godot.FileAccess.GetOpenError()}");
                CreateDefaultGameData();
                return;
            }

            string jsonText = file.GetAsText();
            
            // First deserialize as JsonElement to handle the config separately
            using var jsonDoc = JsonDocument.Parse(jsonText);
            var root = jsonDoc.RootElement;
            
            // Deserialize the main game data structure
            _gameData = JsonSerializer.Deserialize<UnifiedGameData>(jsonText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            // Manually deserialize the config section if it exists
            if (root.TryGetProperty("config", out var configElement))
            {
                _config = JsonSerializer.Deserialize<GameConfig>(configElement.GetRawText(), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip
                });
            }
            else
            {
                _config = CreateDefaultConfigObject();
            }

            GD.Print($"Loaded unified game data: {_gameData.Type} v{_gameData.Version}");
            GD.Print($"Loaded game config: {_config.GameTitle} v{_config.Version}");
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error loading unified game data: {ex.Message}");
            CreateDefaultGameData();
        }
    }

    private GameConfig CreateDefaultConfigObject()
    {
        return new GameConfig
        {
            GameTitle = "Text Adventure",
            Version = "1.0",
            Author = "Unknown",
            StartingLocationId = "armour",
            TimeScale = 60.0f,
            PlayerStartStats = new PlayerStartStats
            {
                Health = 100,
                MaxHealth = 100,
                Satoshi = 0,
                Strength = 10,
                Dexterity = 10,
                Intelligence = 10
            }
        };
    }

    private void CreateDefaultGameData()
    {
        _config = CreateDefaultConfigObject();
        _gameData = new UnifiedGameData
        {
            Type = "unified_game_data",
            Version = "1.0",
            Locations = new List<LocationData>(),
            Items = new List<ItemData>(),
            Characters = new List<CharacterData>(),
            Enemies = new List<EnemyData>(),
            Quests = new List<QuestData>()
        };
    }

    /// <summary>
    /// Loads all locations from unified game data
    /// </summary>
    /// <returns>Dictionary of locations by ID</returns>
    public Dictionary<string, Location> LoadLocations()
    {
        var locations = new Dictionary<string, Location>();

        if (_gameData?.Locations != null)
        {
            foreach (var locData in _gameData.Locations)
            {
                var location = CreateLocationFromData(locData);
                locations[location.Id] = location;
            }
        }

        // Make another pass to resolve item, character, enemy, and puzzle references
        PopulateLocationContents(locations);

        GD.Print($"Loaded {locations.Count} locations.");
        return locations;
    }

    /// <summary>
    /// Creates a Location object from JSON data
    /// </summary>
    private Location CreateLocationFromData(LocationData data)
    {
        var location = new Location
        {
            Id = data.Id,
            Name = data.Name,
            Description = data.Description,
            Region = data.Region ?? "General",
            IsDiscovered = data.IsDiscovered,
            ImagePath = data.ImagePath ?? "",
            ScenePath = data.ScenePath ?? "",
            ImageDisplayMode = data.ImageDisplayMode ?? "default"
        };

        // Set up exits
        if (data.Exits != null)
        {
            foreach (var exit in data.Exits)
            {
                location.Exits[exit.Key.ToLower()] = exit.Value;
            }
        }

        // Set up locked exits
        if (data.LockedExits != null)
        {
            foreach (var lockedExit in data.LockedExits)
            {
                var lockInfo = new LockInfo
                {
                    IsLocked = lockedExit.Value.IsLocked,
                    KeyItem = lockedExit.Value.KeyItem,
                    LockedMessage = lockedExit.Value.LockedMessage
                };

                location.LockedExits[lockedExit.Key.ToLower()] = lockInfo;
            }
        }

        // Set up features
        if (data.Features != null)
        {
            foreach (var feature in data.Features)
            {
                location.Features[feature.Key.ToLower()] = feature.Value;
            }
        }

        // Set up time-based descriptions
        if (data.TimeBasedDescriptions != null)
        {
            foreach (var timeDesc in data.TimeBasedDescriptions)
            {
                location.TimeBasedDescriptions[timeDesc.Key.ToLower()] = timeDesc.Value;
            }
        }

        // Set up state-based features
        if (data.StateBasedFeatures != null)
        {
            foreach (var stateFeature in data.StateBasedFeatures)
            {
                location.StateBasedFeatures[stateFeature.Key] = stateFeature.Value;
            }
        }

        // Parse custom commands
        if (data.CustomCommands != null)
        {
            foreach (var command in data.CustomCommands)
            {
                location.CustomCommands[command.Key] = command.Value;
            }
        }

        return location;
    }

    /// <summary>
    /// Adds items, characters, enemies, and puzzles to locations based on their ID references
    /// </summary>
    private void PopulateLocationContents(Dictionary<string, Location> locations)
    {
        foreach (var location in locations.Values)
        {
            var locationData = _gameData.Locations?.FirstOrDefault(l => l.Id == location.Id);
            if (locationData == null) continue;

            // Add items
            if (locationData.ItemIds != null)
            {
                foreach (var itemId in locationData.ItemIds)
                {
                    if (_allItems.TryGetValue(itemId, out var item))
                    {
                        // Create a copy so the same item can be in multiple locations
                        location.Items.Add(item.Clone());
                    }
                    else
                    {
                        GD.PrintErr($"Item '{itemId}' referenced in location '{location.Id}' not found.");
                    }
                }
            }

            // Add characters
            if (locationData.CharacterIds != null)
            {
                foreach (var characterId in locationData.CharacterIds)
                {
                    if (_allCharacters.TryGetValue(characterId, out var character))
                    {
                        location.Characters.Add(character);
                    }
                    else
                    {
                        GD.PrintErr($"Character '{characterId}' referenced in location '{location.Id}' not found.");
                    }
                }
            }

            // Add enemies
            if (locationData.EnemyIds != null)
            {
                location.Enemies = new List<Enemy>();
                foreach (var enemyId in locationData.EnemyIds)
                {
                    if (_allEnemies.TryGetValue(enemyId, out var enemy))
                    {
                        location.Enemies.Add(enemy.Clone());
                    }
                    else
                    {
                        GD.PrintErr($"Enemy '{enemyId}' referenced in location '{location.Id}' not found.");
                    }
                }
            }
        }
    }

    /// <summary>
    /// Loads all items from unified game data
    /// /// <returns>Dictionary of all items by ID</returns>
    public Dictionary<string, Item> LoadAllItems()
    {
        if (_allItems.Count > 0) return _allItems;

        if (_gameData?.Items != null)
        {
            foreach (var data in _gameData.Items)
            {
                var item = CreateItemFromData(data);
                _allItems[item.Id] = item;
            }

            // Handle containers - second pass for contents
            foreach (var data in _gameData.Items)
            {
                if (data.ItemType == "Container" && data.Contents != null &&
                    _allItems.TryGetValue(data.Id, out var item) && item is Container container)
                {
                    foreach (var contentId in data.Contents)
                    {
                        if (_allItems.TryGetValue(contentId, out var contentItem))
                        {
                            container.Items.Add(contentItem.Clone());
                        }
                        else
                        {
                            GD.PrintErr($"Item '{contentId}' referenced in container '{container.Id}' not found.");
                        }
                    }
                }
            }
        }

        GD.Print($"Loaded {_allItems.Count} items.");
        return _allItems;
    }

    /// <summary>
    /// Creates an Item object from JSON data
    /// </summary>
    private Item CreateItemFromData(ItemData data)
    {
        if (data.ItemType?.ToLower() == "container")
        {
            return CreateContainerFromData(data);
        }

        var item = new Item
        {
            Id = data.Id,
            Name = data.Name,
            Description = data.Description,
            CanTake = data.CanTake,
            Category = data.Category ?? "Miscellaneous"
        };

        // Parse item type
        if (!string.IsNullOrEmpty(data.ItemType) && Enum.TryParse<ItemType>(data.ItemType, true, out var itemType))
        {
            item.ItemType = itemType;
        }
        else
        {
            item.ItemType = ItemType.Miscellaneous;
        }

        item.UseValue = data.UseValue;
        item.IsStackable = data.IsStackable;
        item.StackCount = data.StackCount > 0 ? data.StackCount : 1;

        // Set default use action
        if (!string.IsNullOrEmpty(data.DefaultUseResult))
        {
            string resultText = data.DefaultUseResult;
            item.DefaultUseAction = () => resultText;
        }

        // Set use targets
        if (data.UseTargets != null)
        {
            foreach (var target in data.UseTargets)
            {
                string targetId = target.Key;
                string resultText = target.Value;

                item.UseTargets[targetId] = () => resultText;
            }
        }

        // Set default transition
        if (data.DefaultTransition != null)
        {
            item.DefaultTransition = new ItemTransition
            {
                TargetLocationId = data.DefaultTransition.TargetLocationId,
                TransitionMessage = data.DefaultTransition.TransitionMessage ?? "",
                ConsumeItem = data.DefaultTransition.ConsumeItem,
                RequiredLocationId = data.DefaultTransition.RequiredLocationId ?? "",
                RequiredLocationIds = data.DefaultTransition.RequiredLocationIds ?? new List<string>(),
                RequiredFlags = data.DefaultTransition.RequiredFlags ?? new Dictionary<string, bool>()
            };
        }

        return item;
    }

    /// <summary>
    /// Creates a Container object from JSON data
    /// </summary>
    private Container CreateContainerFromData(ItemData data)
    {
        var container = new Container
        {
            Id = data.Id,
            Name = data.Name,
            Description = data.Description,
            CanTake = data.CanTake,
            Category = data.Category ?? "Container",
            ItemType = ItemType.Container,
            IsOpen = data.IsOpen,
            Capacity = data.Capacity > 0 ? data.Capacity : 20.0f
        };

        // The actual contents are populated in a second pass

        return container;
    }

    /// <summary>
    /// Loads all characters from unified game data
    /// </summary>
    /// <returns>Dictionary of all characters by ID</returns>
    public Dictionary<string, Character> LoadAllCharacters()
    {
        if (_allCharacters.Count > 0) return _allCharacters;

        if (_gameData?.Characters != null)
        {
            foreach (var data in _gameData.Characters)
            {
                var character = CreateCharacterFromData(data);
                _allCharacters[character.Id] = character;
            }
        }

        GD.Print($"Loaded {_allCharacters.Count} characters.");
        return _allCharacters;
    }

    /// <summary>
    /// Creates a Character object from JSON data
    /// </summary>
    private Character CreateCharacterFromData(CharacterData data)
    {
        var character = new Character
        {
            Id = data.Id,
            Name = data.Name,
            Description = data.Description,
            CurrentDialogueId = "greeting"
        };

        // Set up dialogue
        if (data.Dialogue != null)
        {
            foreach (var dialogue in data.Dialogue)
            {
                DialogueNode node = new DialogueNode
                {
                    Id = dialogue.Key,
                    Text = dialogue.Value.Text,
                    DefaultResponseId = dialogue.Value.DefaultResponseId
                };

                // Add responses
                if (dialogue.Value.Responses != null)
                {
                    foreach (var response in dialogue.Value.Responses)
                    {
                        var dialogueResponse = new DialogueResponse
                        {
                            Pattern = response.Pattern,
                            NextDialogueId = response.NextDialogueId
                        };

                        // Set action if there is one
                        if (!string.IsNullOrEmpty(response.Action))
                        {
                            dialogueResponse.Action = () =>
                            {
                                // This is where you would handle the action
                                // For example, activating a quest
                                GD.Print($"Dialogue action triggered: {response.Action}");
                            };
                        }

                        node.Responses.Add(dialogueResponse);
                    }
                }

                character.Dialogue[dialogue.Key] = node;
            }
        }

        return character;
    }

    /// <summary>
    /// Loads all enemies from unified game data
    /// </summary>
    /// <returns>Dictionary of all enemies by ID</returns>
    public Dictionary<string, Enemy> LoadAllEnemies()
    {
        if (_allEnemies.Count > 0) return _allEnemies;

        if (_gameData?.Enemies != null)
        {
            foreach (var data in _gameData.Enemies)
            {
                var enemy = CreateEnemyFromData(data);
                _allEnemies[enemy.Id] = enemy;
            }
        }

        GD.Print($"Loaded {_allEnemies.Count} enemies.");
        return _allEnemies;
    }

    /// <summary>
    /// Creates an Enemy object from JSON data
    /// </summary>
    private Enemy CreateEnemyFromData(EnemyData data)
    {
        var enemy = new Enemy
        {
            Id = data.Id,
            Name = data.Name,
            Description = data.Description,
            Health = data.Health,
            MaxHealth = data.MaxHealth,
            AttackPower = data.AttackPower,
            Defense = data.Defense,
            ExperienceReward = data.ExperienceReward,
            SatoshiReward = data.GoldReward,
            CanFlee = data.CanFlee,
            IsAggressive = data.IsAggressive,
            DeathMessage = data.DeathMessage
        };

        // Set dialog
        if (data.Dialog != null)
        {
            foreach (var dialog in data.Dialog)
            {
                enemy.Dialog[dialog.Key] = dialog.Value;
            }
        }

        // Set loot
        if (data.Loot != null)
        {
            foreach (var loot in data.Loot)
            {
                enemy.PossibleLoot.Add(new LootEntry(loot.ItemId, loot.DropChance));
            }
        }

        // Set special abilities
        if (data.SpecialAbilities != null)
        {
            foreach (var ability in data.SpecialAbilities)
            {
                enemy.SpecialAbilities[ability.Name] = (gameState) =>
                {
                    // This is where you would handle the special ability
                    // For example, dealing damage to the player
                    GD.Print($"Enemy special ability triggered: {ability.Name}");
                };
            }
        }

        // Set tags
        if (data.Tags != null)
        {
            enemy.Tags.AddRange(data.Tags);
        }

        return enemy;
    }

    /// <summary>
    /// Loads all quests from unified game data
    /// </summary>
    /// <returns>List of all quests</returns>
    public List<Quest> LoadAllQuests()
    {
        if (_allQuests.Count > 0) return _allQuests;

        if (_gameData?.Quests != null)
        {
            foreach (var data in _gameData.Quests)
            {
                var quest = CreateQuestFromData(data);
                _allQuests.Add(quest);
            }
        }

        GD.Print($"Loaded {_allQuests.Count} quests.");
        return _allQuests;
    }

    /// <summary>
    /// Creates a Quest object from JSON data
    /// </summary>
    private Quest CreateQuestFromData(QuestData data)
    {
        var quest = new Quest
        {
            Id = data.Id,
            Name = data.Name,
            Description = data.Description,
            IsActive = data.IsActive,
            IsCompleted = data.IsCompleted
        };

        // Add objectives
        if (data.Objectives != null)
        {
            foreach (var objective in data.Objectives)
            {
                quest.Objectives.Add(new QuestObjective
                {
                    Id = objective.Id,
                    Description = objective.Description,
                    IsCompleted = objective.IsCompleted
                });
            }
        }

        // Add triggers
        if (data.Triggers != null)
        {
            foreach (var trigger in data.Triggers)
            {
                TriggerType triggerType;
                if (Enum.TryParse<TriggerType>(trigger.Type, true, out triggerType))
                {
                    quest.Triggers.Add(new QuestTrigger
                    {
                        Type = triggerType,
                        TargetId = trigger.TargetId,
                        SecondaryTargetId = trigger.SecondaryTargetId,
                        ObjectiveIds = trigger.ObjectiveIds ?? new List<string>()
                    });
                }
            }
        }

        return quest;
    }

    /// <summary>
    /// Returns the starting location ID from the config
    /// </summary>
    public string GetStartingLocationId()
    {
        return _config.StartingLocationId ?? "armour";
    }

    /// <summary>
    /// Loads the starting inventory items from the config
    /// </summary>
    public List<Item> LoadStartingInventory()
    {
        var startingItems = new List<Item>();
        
        if (_config.StartingInventory != null)
        {
            foreach (var itemId in _config.StartingInventory)
            {
                if (_allItems.TryGetValue(itemId, out var item))
                {
                    startingItems.Add(item.Clone());
                }
                else
                {
                    GD.PrintErr($"Starting inventory item '{itemId}' not found.");
                }
            }
        }
        
        return startingItems;
    }

    /// <summary>
    /// Returns the player's starting stats from the config
    /// </summary>
    public PlayerStartStats GetPlayerStartStats()
    {
        return _config.PlayerStartStats;
    }

    /// <summary>
    /// Returns the time scale from the config
    /// </summary>
    public float GetTimeScale()
    {
        return _config.TimeScale;
    }

    private Dictionary<string, object> LoadConfigAsDict()
    {
        // Return config as dictionary from unified data
        try
        {
            var configDict = new Dictionary<string, object>();
            if (_config != null)
            {
                configDict["gameTitle"] = _config.GameTitle ?? "";
                configDict["version"] = _config.Version ?? "";
                configDict["author"] = _config.Author ?? "";
                configDict["startingLocationId"] = _config.StartingLocationId ?? "";
                configDict["timeScale"] = _config.TimeScale;
                // Add other config properties as needed
            }
            return configDict;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error converting config to dictionary: {ex.Message}");
        }
        
        return new Dictionary<string, object>();
    }
}

#region Data Models for JSON Deserialization

/// <summary>
/// Unified game data model
/// </summary>
public class UnifiedGameData
{
    public string Type { get; set; }
    public string Version { get; set; }
    // Remove Config property since we handle it separately
    public List<LocationData> Locations { get; set; }
    public List<ItemData> Items { get; set; }
    public List<CharacterData> Characters { get; set; }
    public List<EnemyData> Enemies { get; set; }
    public List<QuestData> Quests { get; set; }
}

/// <summary>
/// Game configuration data model
/// </summary>
public class GameConfig
{
    public string GameTitle { get; set; }
    public string Version { get; set; }
    public string Author { get; set; }
    public string StartingLocationId { get; set; }
    public List<string> StartingInventory { get; set; }
    public float TimeScale { get; set; }
    public PlayerStartStats PlayerStartStats { get; set; }
}

/// <summary>
/// Player starting stats data model
/// </summary>
public class PlayerStartStats
{
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Satoshi { get; set; } = 50; // Changed from 0 - starting with $50
    public int Strength { get; set; } = 10;
    public int Dexterity { get; set; } = 10;
    public int Intelligence { get; set; } = 10;
}

/// <summary>
/// Location data model
/// </summary>
public class LocationData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, string> Exits { get; set; }
    public Dictionary<string, LockedExitData> LockedExits { get; set; }
    public Dictionary<string, string> Features { get; set; }
    public Dictionary<string, string> TimeBasedDescriptions { get; set; }
    public List<string> ItemIds { get; set; }
    public List<string> CharacterIds { get; set; }
    public List<string> EnemyIds { get; set; }
    public string Region { get; set; }
    public bool IsDiscovered { get; set; }
    public Dictionary<string, string> StateBasedFeatures { get; set; }
    public string ImagePath { get; set; }
    public string ScenePath { get; set; }
    public string ImageDisplayMode { get; set; } // "ascii", "regular", or "default"
    public Dictionary<string, string> CustomCommands { get; set; }
}

/// <summary>
/// Locked exit data model
/// </summary>
public class LockedExitData
{
    public bool IsLocked { get; set; }
    public string KeyItem { get; set; }
    public string LockedMessage { get; set; }
}

/// <summary>
/// Item data model - complete implementation
/// </summary>
public class ItemData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool CanTake { get; set; } = true;
    public string Category { get; set; } = "Miscellaneous";
    public string ItemType { get; set; } = "Miscellaneous";
    public int UseValue { get; set; } = 0;
    public bool IsStackable { get; set; } = false;
    public int StackCount { get; set; } = 1;
    public string DefaultUseResult { get; set; }
    public Dictionary<string, string> UseTargets { get; set; }
    public ItemTransitionData DefaultTransition { get; set; }
    
    // Container-specific properties
    public bool IsOpen { get; set; } = false;
    public float Capacity { get; set; } = 20.0f;
    public List<string> Contents { get; set; }
}

/// <summary>
/// Item transition data model
/// </summary>
public class ItemTransitionData
{
    public string TargetLocationId { get; set; }
    public string TransitionMessage { get; set; } = "";
    public bool ConsumeItem { get; set; } = false;
    public string RequiredLocationId { get; set; } = "";
    public List<string> RequiredLocationIds { get; set; } = new List<string>();
    public Dictionary<string, bool> RequiredFlags { get; set; } = new Dictionary<string, bool>();
}

/// <summary>
/// Character data model
/// </summary>
public class CharacterData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, DialogueNodeData> Dialogue { get; set; }
}

/// <summary>
/// Dialogue node data model
/// </summary>
public class DialogueNodeData
{
    public string Text { get; set; }
    public List<DialogueResponseData> Responses { get; set; }
    public string DefaultResponseId { get; set; }
}

/// <summary>
/// Dialogue response data model
/// </summary>
public class DialogueResponseData
{
    public string Pattern { get; set; }
    public string NextDialogueId { get; set; }
    public string Action { get; set; }
}

/// <summary>
/// Enemy data model
/// </summary>
public class EnemyData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public int Health { get; set; }
    public int MaxHealth { get; set; }
    public int AttackPower { get; set; }
    public int Defense { get; set; }
    public int ExperienceReward { get; set; }
    public int GoldReward { get; set; }
    public bool CanFlee { get; set; } = true;
    public bool IsAggressive { get; set; } = false;
    public string DeathMessage { get; set; }
    public List<string> Tags { get; set; }
    public Dictionary<string, string> Dialog { get; set; }
    public List<LootData> Loot { get; set; }
    public List<SpecialAbilityData> SpecialAbilities { get; set; }
}

/// <summary>
/// Loot data model
/// </summary>
public class LootData
{
    public string ItemId { get; set; }
    public float DropChance { get; set; }
}

/// <summary>
/// Special ability data model
/// </summary>
public class SpecialAbilityData
{
    public string Name { get; set; }
    public string Description { get; set; }
    public int Damage { get; set; }
    public int Cooldown { get; set; }
}

/// <summary>
/// Quest data model
/// </summary>
public class QuestData
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public bool IsActive { get; set; } = false;
    public bool IsCompleted { get; set; } = false;
    public List<QuestObjectiveData> Objectives { get; set; }
    public List<QuestTriggerData> Triggers { get; set; }
}

/// <summary>
/// Quest objective data model
/// </summary>
public class QuestObjectiveData
{
    public string Id { get; set; }
    public string Description { get; set; }
    public bool IsCompleted { get; set; } = false;
}

/// <summary>
/// Quest trigger data model
/// </summary>
public class QuestTriggerData
{
    public string Type { get; set; }
    public string TargetId { get; set; }
    public string SecondaryTargetId { get; set; }
    public List<string> ObjectiveIds { get; set; }
}

#endregion