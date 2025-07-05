using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.IO;
using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;

public class ImprovedSaveSystem : SaveSystem
{
    private const int CURRENT_SAVE_VERSION = 2;
    private const string AUTOSAVE_SLOT = "autosave";
    private float _autosaveInterval = 300f; // 5 minutes
    private float _timeSinceLastAutosave = 0f;
    private bool _autosaveEnabled = true;
    
    // Cloud save configuration
    private bool _cloudSaveEnabled = false;
    private string _cloudSaveUrl = "";
    private string _userId = "";
    
    private string _saveDirectory = "user://saves/";
    
    public ImprovedSaveSystem() : base()
    {
        LoadSettings();
        
        // Ensure save directory exists
        using var dir = DirAccess.Open("user://");
        if (dir != null && !dir.DirExists("saves"))
        {
            dir.MakeDir("saves");
        }
    }
    
    private void LoadSettings()
    {
        // Load save settings from config
        string settingsPath = "user://save_settings.json";
        if (Godot.FileAccess.FileExists(settingsPath))
        {
            try
            {
                using (var file = Godot.FileAccess.Open(settingsPath, Godot.FileAccess.ModeFlags.Read))
                {
                    if (file != null)
                    {
                        string json = file.GetAsText();
                        var settings = JsonSerializer.Deserialize<SaveSettings>(json);
                        _autosaveEnabled = settings.AutosaveEnabled;
                        _autosaveInterval = settings.AutosaveInterval;
                        _cloudSaveEnabled = settings.CloudSaveEnabled;
                        _cloudSaveUrl = settings.CloudSaveUrl;
                        _userId = settings.UserId;
                    }
                }
            }
            catch (Exception ex)
            {
                GD.PrintErr($"Error loading save settings: {ex.Message}");
            }
        }
    }
    
    public void UpdateAutosave(float delta)
    {
        if (!_autosaveEnabled) return;
        
        _timeSinceLastAutosave += delta;
        
        if (_timeSinceLastAutosave >= _autosaveInterval)
        {
            _timeSinceLastAutosave = 0f;
            // Autosave will be triggered by GameState
        }
    }
    
    public bool ShouldAutosave()
    {
        return _autosaveEnabled && _timeSinceLastAutosave >= _autosaveInterval;
    }
    
    public void ResetAutosaveTimer()
    {
        _timeSinceLastAutosave = 0f;
    }
    
    public override bool SaveGame(GameState gameState, string slotName)
    {
        try
        {
            // Create enhanced save data
            var saveData = new EnhancedSaveData
            {
                Version = CURRENT_SAVE_VERSION,
                SaveTime = DateTime.Now,
                PlayTime = gameState.GetTotalPlayTime(),
                SaveCount = gameState.GetSaveCount(),
                
                // Player data
                PlayerHealth = gameState.GetPlayer().Health,
                PlayerMaxHealth = gameState.GetPlayer().MaxHealth,
                PlayerSatoshi = gameState.GetPlayer().Satoshi,
                PlayerLevel = gameState.GetPlayer().Level,
                PlayerExperience = gameState.GetPlayer().ExperiencePoints,
                PlayerAttackPower = gameState.GetPlayer().AttackPower,
                PlayerDefense = gameState.GetPlayer().Defense,
                PlayerStats = new Dictionary<string, int>(gameState.GetPlayer().Stats),
                
                // Location data
                CurrentLocationId = gameState.GetCurrentLocation().Id,
                LocationName = gameState.GetCurrentLocation().Name,
                DiscoveredLocationIds = gameState.GetDiscoveredLocationIds(),
                
                // Progress data
                CompletedQuestIds = gameState.GetCompletedQuestIds(),
                ActiveQuestIds = gameState.GetActiveQuestIds(),
                QuestProgress = gameState.GetQuestProgress(),
                
                // Inventory with metadata
                Inventory = SerializeInventoryWithMetadata(gameState.GetPlayer().Inventory),
                EquippedWeaponId = gameState.GetPlayer().EquippedWeapon?.Id,
                EquippedArmorId = gameState.GetPlayer().EquippedArmor?.Id,
                
                // World state
                WorldEvents = gameState.GetWorldEvents(),
                NPCStates = gameState.GetNPCStates(),
                
                // Combat state (if in combat)
                InCombat = gameState.GetCombatSystem().IsInCombat,
                CombatData = SerializeCombatState(gameState.GetCombatSystem()),
                
                // Achievement progress
                AchievementProgress = gameState.GetAchievementSystem()?.GetProgress() ?? new Dictionary<string, float>(),
                
                // Custom flags
                GameFlags = gameState.GetGameFlags(),
                
                // Checksum for save integrity
                Checksum = ""
            };
            
            // Calculate checksum
            saveData.Checksum = CalculateChecksum(saveData);
            
            // Convert to JSON
            string saveJson = JsonSerializer.Serialize(saveData, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            
            // Compress the save data
            byte[] compressedData = CompressData(saveJson);
            
            // Write to file using Godot's FileAccess
            string filePath = _saveDirectory + slotName + EXTENSION;
            using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write))
            {
                if (file != null)
                {
                    file.StoreVar(compressedData);
                }
            }
            
            // Create backup
            CreateBackup(filePath);
            
            // Upload to cloud if enabled
            if (_cloudSaveEnabled && slotName != AUTOSAVE_SLOT)
            {
                UploadToCloud(slotName, compressedData);
            }
            
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error saving game: {ex.Message}");
            return false;
        }
    }
    
    public override bool LoadGame(string slotName, GameState gameState)
    {
        try
        {
            string filePath = _saveDirectory + slotName + EXTENSION;
            
            // Try loading from cloud first if enabled
            if (_cloudSaveEnabled && !Godot.FileAccess.FileExists(filePath))
            {
                byte[] cloudData = DownloadFromCloud(slotName);
                if (cloudData != null)
                {
                    using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write))
                    {
                        if (file != null)
                        {
                            file.StoreVar(cloudData);
                        }
                    }
                }
            }
            
            if (!Godot.FileAccess.FileExists(filePath))
            {
                // Try loading from backup
                string backupPath = filePath + ".bak";
                if (Godot.FileAccess.FileExists(backupPath))
                {
                    using (var sourceFile = Godot.FileAccess.Open(backupPath, Godot.FileAccess.ModeFlags.Read))
                    using (var destFile = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Write))
                    {
                        if (sourceFile != null && destFile != null)
                        {
                            var data = sourceFile.GetBuffer((int)sourceFile.GetLength());
                            destFile.StoreVar(data);
                        }
                    }
                }
                else
                {
                    return false;
                }
            }
            
            // Read and decompress file
            byte[] compressedData;
            using (var file = Godot.FileAccess.Open(filePath, Godot.FileAccess.ModeFlags.Read))
            {
                if (file == null) return false;
                compressedData = file.GetVar().AsByteArray();
            }
            
            string saveJson = DecompressData(compressedData);
            
            // Parse JSON
            var saveData = JsonSerializer.Deserialize<EnhancedSaveData>(saveJson);
            
            // Verify checksum
            string calculatedChecksum = CalculateChecksum(saveData);
            if (calculatedChecksum != saveData.Checksum)
            {
                GD.PrintErr("Save file integrity check failed!");
                // Optionally continue loading anyway
            }
            
            // Handle version differences
            if (saveData.Version != CURRENT_SAVE_VERSION)
            {
                saveData = MigrateSaveData(saveData);
            }
            
            // Load all the data
            LoadEnhancedSaveData(saveData, gameState);
            
            return true;
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error loading game: {ex.Message}");
            return false;
        }
    }
    
    private byte[] CompressData(string data)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(data);
        
        using (var outputStream = new MemoryStream())
        {
            using (var gzipStream = new GZipStream(outputStream, CompressionMode.Compress))
            {
                gzipStream.Write(bytes, 0, bytes.Length);
            }
            return outputStream.ToArray();
        }
    }
    
    private string DecompressData(byte[] compressedData)
    {
        using (var inputStream = new MemoryStream(compressedData))
        {
            using (var gzipStream = new GZipStream(inputStream, CompressionMode.Decompress))
            {
                using (var outputStream = new MemoryStream())
                {
                    gzipStream.CopyTo(outputStream);
                    return Encoding.UTF8.GetString(outputStream.ToArray());
                }
            }
        }
    }
    
    private string CalculateChecksum(EnhancedSaveData saveData)
    {
        // Create a string representation of important data
        string dataString = $"{saveData.SaveTime}{saveData.PlayerLevel}{saveData.CurrentLocationId}{saveData.PlayTime}{saveData.PlayerSatoshi}";
        
        using (SHA256 sha256 = SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(dataString));
            return Convert.ToBase64String(hashBytes);
        }
    }
    
    private void CreateBackup(string originalPath)
    {
        try
        {
            string backupPath = originalPath + ".bak";
            if (Godot.FileAccess.FileExists(originalPath))
            {
                using (var sourceFile = Godot.FileAccess.Open(originalPath, Godot.FileAccess.ModeFlags.Read))
                using (var destFile = Godot.FileAccess.Open(backupPath, Godot.FileAccess.ModeFlags.Write))
                {
                    if (sourceFile != null && destFile != null)
                    {
                        var data = sourceFile.GetBuffer((int)sourceFile.GetLength());
                        destFile.StoreVar(data);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            GD.PrintErr($"Error creating backup: {ex.Message}");
        }
    }
    
    private void UploadToCloud(string slotName, byte[] data)
    {
        // Implement cloud save upload
        // This would typically use HTTP requests to upload to a server
        GD.Print($"Cloud save upload for slot {slotName} (not implemented)");
    }
    
    private byte[] DownloadFromCloud(string slotName)
    {
        // Implement cloud save download
        // This would typically use HTTP requests to download from a server
        GD.Print($"Cloud save download for slot {slotName} (not implemented)");
        return null;
    }
    
    private EnhancedSaveData MigrateSaveData(EnhancedSaveData oldData)
    {
        // Handle save data migration between versions
        GD.Print($"Migrating save data from version {oldData.Version} to {CURRENT_SAVE_VERSION}");
        
        // Add any new fields with default values
        if (oldData.Version < 2)
        {
            oldData.PlayTime = 0;
            oldData.SaveCount = 0;
            oldData.GameFlags = new Dictionary<string, bool>();
            oldData.AchievementProgress = new Dictionary<string, float>();
        }
        
        oldData.Version = CURRENT_SAVE_VERSION;
        return oldData;
    }
    
    private List<SerializedItemWithMetadata> SerializeInventoryWithMetadata(List<Item> inventory)
    {
        var serializedItems = new List<SerializedItemWithMetadata>();
        
        foreach (var item in inventory)
        {
            var serializedItem = new SerializedItemWithMetadata
            {
                Id = item.Id,
                Name = item.Name,
                Description = item.Description,
                ItemType = item.ItemType.ToString(),
                UseValue = item.UseValue,
                Category = item.Category,
                CanTake = item.CanTake,
                CustomProperties = new Dictionary<string, string>()
            };
            
            serializedItems.Add(serializedItem);
        }
        
        return serializedItems;
    }
    
    private void LoadEnhancedSaveData(EnhancedSaveData saveData, GameState gameState)
    {
        // Load basic game data first
        gameState.GetPlayer().Health = saveData.PlayerHealth;
        gameState.GetPlayer().MaxHealth = saveData.PlayerMaxHealth;
        gameState.GetPlayer().Satoshi = saveData.PlayerSatoshi;
        gameState.GetPlayer().Level = saveData.PlayerLevel;
        gameState.GetPlayer().ExperiencePoints = saveData.PlayerExperience;
        gameState.GetPlayer().AttackPower = saveData.PlayerAttackPower;
        gameState.GetPlayer().Defense = saveData.PlayerDefense;
        
        // Set current location
        var location = gameState.GetLocationById(saveData.CurrentLocationId);
        if (location != null)
        {
            gameState.SetCurrentLocation(location);
        }
        
        // Set discovered locations
        gameState.SetDiscoveredLocations(saveData.DiscoveredLocationIds);
        
        // Set completed quests
        gameState.SetCompletedQuests(saveData.CompletedQuestIds);
        
        // Load additional enhanced data
        gameState.SetTotalPlayTime(saveData.PlayTime);
        gameState.SetSaveCount(saveData.SaveCount);
        gameState.SetWorldEvents(saveData.WorldEvents);
        gameState.SetNPCStates(saveData.NPCStates);
        gameState.SetGameFlags(saveData.GameFlags);
        
        // Load achievement progress
        if (gameState.GetAchievementSystem() != null)
        {
            gameState.GetAchievementSystem().LoadProgress(saveData.AchievementProgress);
        }
        
        // Restore combat state if applicable
        if (saveData.InCombat && saveData.CombatData != null)
        {
            RestoreCombatState(saveData.CombatData, gameState);
        }
    }
    
    private CombatSaveData SerializeCombatState(CombatSystem combatSystem)
    {
        // Serialize current combat state if in combat
        if (!combatSystem.IsInCombat)
            return null;
            
        var currentEnemy = combatSystem.GetCurrentEnemy();
        return new CombatSaveData
        {
            EnemyId = currentEnemy?.Id ?? "",
            EnemyHealth = currentEnemy?.Health ?? 0,
            StatusEffects = new List<string>(),
            CombatStance = "normal"
        };
    }
    
    private void RestoreCombatState(CombatSaveData combatData, GameState gameState)
    {
        // Restore combat state - this would need implementation in CombatSystem
        // For now, just log that combat state was saved
        GD.Print($"Combat state loaded for enemy: {combatData.EnemyId}");
    }
    
    public void ClearErrorLog()
    {
        try
        {
            string logPath = "user://save_log.txt";
            if (Godot.FileAccess.FileExists(logPath))
            {
                DirAccess.RemoveAbsolute(logPath);
            }
        }
        catch
        {
            // Ignore
        }
    }
}

// Enhanced save data structure
public class EnhancedSaveData : SaveData
{
    public int Version { get; set; }
    public float PlayTime { get; set; }
    public int SaveCount { get; set; }
    public float GameTime { get; set; }
    public List<string> ActiveQuestIds { get; set; }
    public Dictionary<string, int> QuestProgress { get; set; }
    public Dictionary<string, string> WorldEvents { get; set; }
    public Dictionary<string, NPCState> NPCStates { get; set; }
    public bool InCombat { get; set; }
    public CombatSaveData CombatData { get; set; }
    public Dictionary<string, float> AchievementProgress { get; set; }
    public Dictionary<string, bool> GameFlags { get; set; }
    public string Checksum { get; set; }
    
    // Use the enhanced inventory type instead of overriding
    public new List<SerializedItemWithMetadata> Inventory { get; set; }
}

public class SerializedItemWithMetadata : SerializedItem
{
    public int Durability { get; set; }
    public int MaxDurability { get; set; }
    public int EnhancementLevel { get; set; }
    public Dictionary<string, string> CustomProperties { get; set; }
}

public class CombatSaveData
{
    public string EnemyId { get; set; }
    public int EnemyHealth { get; set; }
    public List<string> StatusEffects { get; set; }
    public string CombatStance { get; set; }
}

public class NPCState
{
    public string LocationId { get; set; }
    public string CurrentDialogueId { get; set; }
    public Dictionary<string, bool> Flags { get; set; }
}

public class SaveSettings
{
    public bool AutosaveEnabled { get; set; }
    public float AutosaveInterval { get; set; }
    public bool CloudSaveEnabled { get; set; }
    public string CloudSaveUrl { get; set; }
    public string UserId { get; set; }
}