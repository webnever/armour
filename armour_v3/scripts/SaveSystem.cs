using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

// Handles saving and loading game state
public class SaveSystem
{
    protected string _savePath = "user://saves/";
    protected const string EXTENSION = ".save";
    
    public SaveSystem()
    {
        // Ensure saves directory exists using Godot's DirAccess
        using var dir = DirAccess.Open("user://");
        if (dir != null && !dir.DirExists("saves"))
        {
            dir.MakeDir("saves");
        }
    }
    
    public virtual bool SaveGame(GameState gameState, string slotName)
    {
        try
        {
            // Create save data container
            var saveData = new SaveData
            {
                SaveTime = DateTime.Now,
                PlayerHealth = gameState.GetPlayer().Health,
                PlayerMaxHealth = gameState.GetPlayer().MaxHealth,
                PlayerSatoshi = gameState.GetPlayer().Satoshi,
                PlayerLevel = gameState.GetPlayer().Level,
                PlayerExperience = gameState.GetPlayer().ExperiencePoints,
                PlayerAttackPower = gameState.GetPlayer().AttackPower,
                PlayerDefense = gameState.GetPlayer().Defense,
                PlayerStats = new Dictionary<string, int>(gameState.GetPlayer().Stats),
                
                CurrentLocationId = gameState.GetCurrentLocation().Id,
                LocationName = gameState.GetCurrentLocation().Name,
                
                DiscoveredLocationIds = gameState.GetDiscoveredLocationIds(),
                CompletedQuestIds = gameState.GetCompletedQuestIds(),
                
                Inventory = SerializeInventory(gameState.GetPlayer().Inventory),
                EquippedWeaponId = gameState.GetPlayer().EquippedWeapon?.Id,
                EquippedArmorId = gameState.GetPlayer().EquippedArmor?.Id,                
            };
            
            // Convert to JSON
            string saveJson = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            // Write to file using Godot's FileAccess
            string filePath = _savePath + slotName + EXTENSION;
            using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write))
            {
                if (file != null)
                {
                    file.StoreString(saveJson);
                }
            }
            
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error saving game: {ex.Message}");
            return false;
        }
    }
    
    public virtual bool LoadGame(string slotName, GameState gameState)
    {
        try
        {
            string filePath = _savePath + slotName + EXTENSION;
            
            if (!Godot.FileAccess.FileExists(filePath))
            {
                return false;
            }
            
            // Read file using Godot's FileAccess
            string saveJson;
            using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read))
            {
                if (file == null)
                    return false;
                saveJson = file.GetAsText();
            }
            
            // Parse JSON
            var saveData = JsonSerializer.Deserialize<SaveData>(saveJson);
            
            // Load player data
            Player player = gameState.GetPlayer();
            player.Health = saveData.PlayerHealth;
            player.MaxHealth = saveData.PlayerMaxHealth;
            player.Satoshi = saveData.PlayerSatoshi;
            player.Level = saveData.PlayerLevel;
            player.ExperiencePoints = saveData.PlayerExperience;
            player.AttackPower = saveData.PlayerAttackPower;
            player.Defense = saveData.PlayerDefense;
            player.Stats = new Dictionary<string, int>(saveData.PlayerStats);
            
            // Set current location
            var newLocation = gameState.GetLocationById(saveData.CurrentLocationId);
            if (newLocation != null)
            {
                gameState.SetCurrentLocation(newLocation);
            }
            
            // Set discovered locations
            gameState.SetDiscoveredLocations(saveData.DiscoveredLocationIds);
            
            // Set completed quests
            gameState.SetCompletedQuests(saveData.CompletedQuestIds);
            
            // Load inventory
            LoadInventory(saveData.Inventory, player, gameState);
            
            // Set equipped items
            if (saveData.EquippedWeaponId != null)
            {
                Item weapon = player.Inventory.FirstOrDefault(i => i.Id == saveData.EquippedWeaponId);
                if (weapon != null)
                {
                    player.EquipItem(weapon);
                }
            }
            
            if (saveData.EquippedArmorId != null)
            {
                Item armor = player.Inventory.FirstOrDefault(i => i.Id == saveData.EquippedArmorId);
                if (armor != null)
                {
                    player.EquipItem(armor);
                }
            }
                        
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error loading game: {ex.Message}");
            return false;
        }
    }
    
    public List<SaveSlotInfo> GetSaveSlots()
    {
        var saveSlots = new List<SaveSlotInfo>();
        
        try
        {
            using var dir = DirAccess.Open(_savePath);
            if (dir != null)
            {
                dir.ListDirBegin();
                string fileName = dir.GetNext();
                
                while (fileName != "")
                {
                    if (fileName.EndsWith(EXTENSION))
                    {
                        try
                        {
                            string filePath = _savePath + fileName;
                            string saveJson;
                            
                            using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read))
                            {
                                if (file != null)
                                {
                                    saveJson = file.GetAsText();
                                    var saveData = JsonSerializer.Deserialize<SaveData>(saveJson);
                                    
                                    string slotName = fileName.Replace(EXTENSION, "");
                                    
                                    saveSlots.Add(new SaveSlotInfo
                                    {
                                        SlotName = slotName,
                                        SaveTime = saveData.SaveTime,
                                        LocationName = saveData.LocationName,
                                        PlayerLevel = saveData.PlayerLevel
                                    });
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            GD.PrintErr($"Error reading save file {fileName}: {ex.Message}");
                        }
                    }
                    fileName = dir.GetNext();
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error listing save files: {ex.Message}");
        }
        
        return saveSlots;
    }
    
    public bool DeleteSaveSlot(string slotName)
    {
        try
        {
            string filePath = _savePath + slotName + EXTENSION;
            
            if (Godot.FileAccess.FileExists(filePath))
            {
                DirAccess.RemoveAbsolute(filePath);
                return true;
            }
            
            return false;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error deleting save file: {ex.Message}");
            return false;
        }
    }
    
    private List<SerializedItem> SerializeInventory(List<Item> inventory)
    {
        var serializedItems = new List<SerializedItem>();
        
        foreach (var item in inventory)
        {
            serializedItems.Add(new SerializedItem
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                ItemType = item.ItemType.ToString(),
                UseValue = item.UseValue,
                Category = item.Category,
                CanTake = item.CanTake
            });
        }
        
        return serializedItems;
    }
    
    private void LoadInventory(List<SerializedItem> serializedItems, Player player, GameState gameState)
    {
        // Clear current inventory
        player.Inventory.Clear();
        
        // Load items from save data
        foreach (var serializedItem in serializedItems)
        {
            Item item = new Item
            {
                Id = serializedItem.Id,
                Name = serializedItem.Name,
                Description = serializedItem.Description,
                UseValue = serializedItem.UseValue,
                Category = serializedItem.Category,
                CanTake = serializedItem.CanTake
            };
            
            // Parse ItemType from string
            if (Enum.TryParse<ItemType>(serializedItem.ItemType, true, out var itemType))
            {
                item.ItemType = itemType;
            }
            else
            {
                item.ItemType = ItemType.Miscellaneous;
            }
            
            player.Inventory.Add(item);
        }
    }
}

// Container for save data
public class SaveData
{
    // Meta info
    public DateTime SaveTime { get; set; }
    
    // Player data
    public int PlayerHealth { get; set; }
    public int PlayerMaxHealth { get; set; }
    public int PlayerSatoshi { get; set; } // Changed from PlayerGold
    public int PlayerLevel { get; set; }
    public int PlayerExperience { get; set; }
    public int PlayerAttackPower { get; set; }
    public int PlayerDefense { get; set; }
    public Dictionary<string, int> PlayerStats { get; set; }
    
    // Game state
    public string CurrentLocationId { get; set; }
    public string LocationName { get; set; }
    public List<string> DiscoveredLocationIds { get; set; }
    public List<string> CompletedQuestIds { get; set; }
    
    // Inventory - use the base SerializedItem type for compatibility
    public virtual List<SerializedItem> Inventory { get; set; }
    public string EquippedWeaponId { get; set; }
    public string EquippedArmorId { get; set; }
    
    // Environment
    public string TimeOfDay { get; set; }
}

// Simple save slot info for the save menu
public class SaveSlotInfo
{
    public string SlotName { get; set; }
    public DateTime SaveTime { get; set; }
    public string LocationName { get; set; }
    public int PlayerLevel { get; set; }
}

// Serializable version of Item class
public class SerializedItem
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public string ItemType { get; set; }
    public int UseValue { get; set; }
    public string Category { get; set; }
    public bool CanTake { get; set; }
}