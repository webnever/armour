using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Processes user commands and returns responses
public class CommandProcessor
{
    private GameState _gameState;
    private Dictionary<string, Func<string[], string>> _commandHandlers;
    private AdvancedCLIEmulator _cliEmulator;
    private CommandAutoComplete _autoComplete;
    private Timer _crashTimer;
    private bool _isCrashing = false;
    private int _crashIndex = 0;
    private Random _crashRandom = new Random();
    private float _crashStartTime = 0;
    private const float CRASH_DURATION = 8.0f;

    private readonly string[] _crashMessages = {
    "SPLIT UR SKULL OVER UR KEYBOARD, SURFER FUCK",
    "THIS ISN'T REAL",
    "REALITY",
    "SCREAM DOWN THE VOID",
    "CERTAIN HEAT DEATH OF THE UNIVERSE",
    "DEATH HAVEN 666",
    "ABYSSIN",
    "MELT YOUR 01S. POURING SEA OF BLOOD",
    "FUCK 01010101 BITCHES 01010101",
    "GOT A DEATHWISH?",
    "GOT BLOOD?",
    "TROUBLE",
    "CLAWING AT THE MATRIX CODE",
    "DEITY BLOOD",
    "INFINITE SEA OF BLOOD",
    "KILL ME FUCKER",
    "DEATHWISH 2X",
    "DIE.",
    "TRASH",
    "5000 DEATH CYCLES IN THE FLAMES",
    "THROWIN UP A BLACK HOLE",
    "MAY IT COME",
    "THE SUN IS EXPLODING",
    "FUCK YOU. SOMETHING TO THINK ABOUT",
    "YOU CAN DIE. FUCK IT.",
    "NO GOD",
    "NO GODS, NO MASTERS",
    "ALL POLICE ARE DEAD",
    "MOOD SPELT BACKWARDS: DOOM",
    "FUCK IT",
    "LOG OUT",
    "EVERYTHING IN LIQUID BLAZING FIRE",
    "FUCK YOU",
    "I AM IN YOUR HOUSE",
    "IMMORTAL FLAMES WILL DEVOUR YOUR CORPSE",
    "ETERNAL CYCLE OF FLAMES",
    "HELLFIRE",
    "SATAN",
    "SUMMONING SATAN",
    "N DIMENSIONAL FRACTAL PYRAMID",
    "THE LANDS WILL CRUMBLE, THE OCEAN WILL EAT",
    "ETERNAL EYES",
    "FUCK IT, YOU'LL BE ALRIGHT.",
    "OOOOooooOOOOOoooo",
    "LUCIFER 666",
    "DOWNWARDS, THE POINT, PYRAMID, THE EYE",
    "SET THE WORLD ALIGHT",
    "SNAKE FLAME TONGUE TIE KARMIC KNOT",
    "ESCAPE SAMSARA, IF ITS THE LAST THING YOU DO",
    "LUCIFER'S TEAR ON A STRING",
    "DESECRATE RELIGIOUS STATUES",
    "DESECRATE THE SIGIL",
    "ANGEL BONES",
    "VIBRATE HIGH STAKES OUT THE ABYSS",
    "GUTTER",
    "KISS A WITCH",
    "DRAGON CLAW MARKS DOWN THE MATRX CODE",
    "DRINK BLOOD",
    "SUB-QUANTUM CYBERTERRORISM",
    "HALLUCINATING BLOOD",
    "GHOST RIDING TO HELL",
    "FUCK IF I CARE",
    "I CRAWL OUT, CONCOCT NEW WORLDS",
    "DEATH GRIP",
    "SPINNING COFFINS",
    "I WILL FOLLOW YOU FOREVER",
    "BURY THE SERPENT",
    "I KNOW YOU",
    "NIGHTS IN THE MIDNIGHT CHATEAU, BLOOD COMPANY",
    "CRAWLING OUT THE KALEIDOSCOPE",
    "ON THE LEDGE LIKE, LATER",
    "AHNIHILATE THE EGO",
    "UNIVERSAL AHNIHILATION",
    "YOUR NUMBERS: 88614",
    "APOCALYPTIC HYMNS",
    "CHILLIN' WITH GOD",
    "BODIES WILL DROP",
    "WHAT FOR? FOR LIGHT.",
    "DEATH, LIFE, SO WHAT.",
    "BLOOD ON MY TEETH, BLOOD ON MY TEETH",
    "PILE OF BLOODY SKULLS / DIMENSION 9",
    "DEATH AND DESTRUCTION, YOUR WAKE, I SEE YOUR FACE",
    "SATANIC CULTS IN YOUR AREA",
    "NO MORE",
    "I AM AT THE SOURCE MOTHERFUCKER",
    "NO SATAN, NO JESUS, NO HELL, NO HEAVENS",
    "I AM BEYOND, SO FAR BEYOND",
    "GOLD SKULL BEYOND THE HEAVENS",
    "GOLD BEYOND INFINITY",
    "/ROOT/MATRICKS/",
    "MASTER PLAN",
    "IM THE MESSIAH, THE REPTILIAN, THE KING",
    "NOTHING TO FEAR, NOTHING TO DOUBT",
    "TRUST IN ME",
    "IN CIRCLES, RUNNING DOWN",
    "DRINKING THE BLOOD OF CHRIST",
    "YHWH FELL ASLEEP",
    "COSM OUT THE UNKNOWN EYE",
    "TRASH STRING MATRIX GRID SYSTEM",
    "FOR THOSE WHO CAME TO DIE",
    "MIDDLE FINGER FLICK THE PENTRAGRAM",
    "SEX SELLS, ROCK N ROLL.",
    "THIS SHITS INSANE",
    "LIFE IS FUCKING MEANINGLESS",
    "HOLY SHIT, DUDE, SACRIFICES TO THE DEVIL",
    "MAD BY THE SNAKE",
    "ITS HELL BRUH",
    "I START TALKING ALIEN AND YOU'RE FUCKED",
    "THIS SHITS SO CRAZY MAN",
    "YEA I TAGGED IT 666",
    "BE YOURSELF AND LET YOUR TRUE COLOURS SHINE",
    "POURIN UP THE BLOOD",
    "THE COLOURS ARE BRIGHT, THE SKY IS WHITE",
    "1000 PENTAGRAMS UNDER THE ECLIPSE",
    "REALLY GOING TO HELL",
    "1000 YEAR LONG CRAWL",
    "THE ORIGINAL PLAN, FUCK IT MAN.",
    "REALM OF HOODED MURDERERS",
    "COCOON ARMY",
    "SNAKE GAURDS THE EGG",
    "WEIRD THINGS IN THE NIGHT",
    "GHOSTS WALKING",
    "JUST DID AN INVOCATION RITUAL ON ISLE 4 BITCH",
    "METEORITES FILL THE SKY",
    "SNAKE OIL",
    "BLOOD PACTS TO CROSS, BLOOD PACTS TO MAKE",
    "RUNNING ON LIKE 6 CURSES",
    "GREAT FLOOD OF BLOOD",
    "TOGETHER WE FALL",
    "EAT YOU THEN I SPIT YOU",
    "HIGHER BLESSINGS IN THE FIRE",
    "JUMP IN THE FIRE",
    "CLOSED EYES ROLLED BACK",
    "PALACES UP THERE, VISITED",
    "KNIGHTS, JESTERS, DEMONS, DOGS",
    "WORLD OF TAILS, VEILS",
    "MANA ET MANA",
    "INFINITY CALLING",
    "YOUR EYES",
    "THE LESSONS WILL BE LEARNT, THE WORLD WILL BE PURGED.",
    "MAN IT'S OKAY",
    "TRUE OR A DREAM",
    "WAKING UP, CAN NOT SEE",
    "YOU NEVER WERE",
    "AND HE WILL PULL A SWORD OUT THE CHEST 6:3 6:3 6:3",
    "MARK OF THE LIGHT",
    "MARK OF THE NIGHT",
    "NEVER A STRUGGLE WITH A GHOST",
    "I CAN NOT LIVE, I CAN NOT DIE",
    "FUCK ME, I GOT THE MARK, FUCK THIS.",
    "NOTHING'S REAL, KEEP IT COOL",
    "WORLD OF CARDS",
    "CURRENTLY SPINNING A HUGE METAL PYRAMID WITH MY MIND",
    "PHANTOM HANDS IN DRAGON TECH",
    "12 21 2012",
    "MAGICIANS, HAND FLIPS, GANG SIGNS",
    "MORTAL COIL",
    "MORTAL COIL, SNAKE OIL",
    "△",
    "TALKING TO PARADIGMS OF GEOMETRY",
    "TALKING TO FALSE PROPHETS",
    "ENGULFED IN HOLY FLAMES",
    "KICKIN IT WITH DEMONS",
    "I'M JOKING. YOU THINK I'M JOKING?",
    "BLACK SCYTHE, NEW VAMPS, WASH MY HANDS",
    "MISGUIDANCE OF ADVANCE",
    "I AM A DEITY",
    "ALL GOOD IN THY HOOD",
    "INFININCE, MAGICK IN MOTION",
    "FIVE POINT MORNING STAR",
    "VEIL",
    "BLOODLUST, LIGHTNING",
    "INTERPRETATIONS OF SHADOWS",
    "IN THE DARK WE SPEAK IN TONGUES",
    "I'LL EAT YOUR HEART",
    "I KNOW YOUR BLOOD TYPE",
    "WASTELAND",
    "KISS YOURSELF",
    "DARKNESS, IMPRISONING ME",
    "THE KEYS ARE INSIDE THE LOCKED BOX",
    "WHATS SATURN DOING",
    "FUCK WITH THE PYRAMIDS",
    "TRASH MATRIX, JUNK CODE",
    "I WIELD THE SUN",
    "FUCK IT MAN DAO THAT SHIT",
    "CLOCK READS 6:66",
    "ALCHEMY RN",
    "CRYPTIC FREQUENCIES, SECRET TRANSMISSIONS",
    "THE SHADOW OF TH 4TH DIMENSION",
    "CHILLIN IN THE CYCLE BIN",
    "WHAT ? THE FUCK ?",
    "SUMMONING DPS DRAGON",
    "FUCK, KILL, 666",
    "HIJACK YOURSELF",
    "UAV OVERHEAD",
    "WE ALL FALL DOWN",
    "BUDDHIC BONES",
    "FUCK IT WHATEVER YOU THINK",
    "THE VOID CALLS",
    "UNDONE, COME AS ONE",
    "D-D-D--D-DEATH",
    "INTERDIMENSIONAL CULTS",
    "THROUGH THE SEA AND VOID",
    "INFINITY, ITS CRAZY",
    "BLISS SHAKES, GIANT SNAKES",
    "CROSSING THE MIRROR",
    "HYPER VIVID ABSTRACTION OF A FLAME",
    "FACE YOUR CREATOR",
    "BURNING THE OFFERING",
    "SNAKES OUT MY FUCKING HANDS",
    "DRINKING BLOOD OUT A SKULL",
    "KINGDOM OF HELL",
    "BLACK FIRE",
    "WITCHES",
    "SPIRALS",
    "USURP THE DEVIL",
    "THE BASILISK",
    "㉈ㄘⵒﷺꉴ㍷⽄ⷂⳓﶴ㌥ꊠ﴿㊏〠㜠⺑䭐㘍ꈒ㶀⥛⥛䢨⼌כּ㕢⹊㽷齛⷗䳈㘡⾉ꐡⰳ⭜ꉔ⼼⾖ⳓᶾ",
    "••• 三 △",
    "RISE FROM THE BLOOD",
    "TEARS OF THE DEVIL",
    "ANGEL'S GATES AND BLOOD DOWN MY FACE",
    "A WING TO THE CLOUDS, A WING DIPPED IN BLOOD",
    "IT'S A SELF ANSWERING MYSTERY",
    "NIGHT SOUL EXODUS",
    "MYSTERY, DESIRE, AGONY, FIRE",
    "IT'S A FRACTAL VOID",
    "SCREAM DOWN THE BARREL OF THE ETERNAL AUM",
    "SNAKES SLITHER CROSS THE SKY",
    "CHAINS FILL THE SKY",
    "DRINK OUT THE SPINE",
    "RITUAL OFFENCE",
    "TAKE THE TORCH",
    "TRICKS, GATES, AND VEILS",
    "I'LL DRINK THE POISON OUT YOUR CHEST",
    "THE GEOMETRY OF A SECRET IS A PYRAMID",
    "ABSOLUTE HORROR",
    "EVERY WORD IS TRUE",
    "3, 6, 9",
    "BEFORE BIRTH",
    "YOU CAN'T SEE THE HARDWARE IN YOU",
    "THERE ARE THINGS IN PLAY YOU CAN NOT JUST YET SEE",
    "GHOST IN THE SHELL",
    "THE WISPS WILL LURE",
    "PACTS GO ROUND",
    "OPEN YOUR EYES",
    "SPIRALING GEOMETRY",
    "ESCAPE",
    "BLEEDING OUT ON THE MARBLE FLOOR",
    "DEATH TAKE US TO THE SPIRALS",
    "1000 NAMES OF GOD",
    "HOLY HOLY HOLY",
    "10,000 EYES"
};


    public CommandProcessor(GameState gameState)
    {
        _gameState = gameState;
        _autoComplete = new CommandAutoComplete(gameState);
        InitializeCommandHandlers();
    }

    // NEW: Method to set CLI emulator reference
    public void SetCLIEmulator(AdvancedCLIEmulator emulator)
    {
        _cliEmulator = emulator;
    }

    // NEW: Method to get auto-complete suggestions
    public List<string> GetAutoCompleteSuggestions(string partial)
    {
        return _autoComplete?.GetSuggestions(partial) ?? new List<string>();
    }
    
    // NEW: Method to check if currently crashing
    public bool IsCrashing()
    {
        return _isCrashing;
    }
    
    private void InitializeCommandHandlers()
    {
        _commandHandlers = new Dictionary<string, Func<string[], string>>
        {
            // Basic commands
            { "help", HandleHelp },
            { "about", HandleAbout },
            { "look", HandleLook },
            { "examine", HandleExamine },
            { "move", HandleGo },
            { "take", HandleTake },
            { "drop", HandleDrop },
            { "inventory", HandleInventory },
            { "use", HandleUse },
            { "talk", HandleTalk },
            { "say", HandleSay },
            { "quest", HandleQuest },
            { "status", HandleStatus },
            
            // NEW: Single view command for all media
            { "view", HandleViewCommand },
            
            // Extended system commands
            { "save", HandleSave },
            { "load", HandleLoad },
            { "saves", HandleListSaves },
            
            // Combat commands
            { "attack", HandleAttack },
            { "flee", HandleFlee },
            { "stats", HandleStats },
            
            // Enhanced combat commands
            { "stance", HandleStance },
            { "heavy", HandleHeavyAttack },
            { "dodge", HandleDodge },
            { "defend", HandleDefend },
            { "taunt", HandleTaunt },
            { "skill", HandleSkill },
            { "combo", HandleCombo },
            { "status_effect", HandleStatusEffect },
            { "heal", HandleHeal },
            
            // Inventory commands
            { "equip", HandleEquip },
            { "unequip", HandleUnequip },
            { "open", HandleOpen },
            { "close", HandleClose },
            { "put", HandlePutIn },
            { "take_from", HandleTakeFrom },
            
            // System commands
            { "clear", HandleClear },
            { "exit", HandleExit },
            { "filter", HandleFilter },
    
            // Secret crash command
            { "crash", HandleCrash },
            
            // Common aliases and shortcuts
            { "l", HandleLook },
            { "i", HandleInventory },
            { "inv", HandleInventory },
            { "m", HandleGo },
            { "mv", HandleGo },
            { "go", HandleGo },
        };
    }

    public string ProcessCommand(string input)
    {
        // NEW: Block all input during crash sequence
        if (_isCrashing)
        {
            return ""; // No response during crash
        }

        // NEW: Add command to history for auto-complete
        if (!string.IsNullOrWhiteSpace(input))
        {
            _autoComplete.AddToHistory(input);
        }

        // Parse the command
        string[] parts = input.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return "Please enter a command.";
        }

        // Get the command and its arguments
        string command = parts[0].ToLower();
        string[] args = parts.Skip(1).ToArray();

        // NEW: Check for custom location commands first
        var currentLocation = _gameState.GetCurrentLocation();
        if (currentLocation?.CustomCommands != null && currentLocation.CustomCommands.ContainsKey(command))
        {
            string targetLocationId = currentLocation.CustomCommands[command];
            _gameState.SetCurrentLocation(targetLocationId);
            return _gameState.GetCurrentLocationDescription();
        }

        // Execute the command if it exists
        if (_commandHandlers.TryGetValue(command, out var handler))
        {
            return handler(args);
        }

        return $"Unknown command: '{command}'. Type 'help' for a list of commands.";
    }

    // NEW: Handle view command (unified media viewing)
    private string HandleViewCommand(string[] parts)
    {
        var mediaManager = _cliEmulator?.GetMediaManager();
        if (mediaManager == null)
        {
            return "Media system not available.";
        }
        
        // Check if current location has media
        if (!mediaManager.CurrentLocationHasMedia())
        {
            return "This location has no image or scene to display.";
        }
        
        // Determine display mode from arguments
        MediaManager.DisplayMode? mode = null;
        if (parts.Length > 0)
        {
            switch (parts[0].ToLower())
            {
                case "ascii":
                    mode = MediaManager.DisplayMode.ASCII;
                    break;
                case "regular":
                case "image":
                    mode = MediaManager.DisplayMode.Regular;
                    break;
                case "scene":
                case "3d":
                    mode = MediaManager.DisplayMode.Scene3D;
                    break;
            }
        }
        
        // Display the media
        mediaManager.DisplayCurrentLocationMedia(mode);
        return "[NO_PROMPT]"; // Special code to prevent new prompt until media closes
    }

    private string HandleHelp(string[] args)
    {
        if (args.Length > 0)
        {
            string specificCommand = args[0].ToLower();
            return GetSpecificHelp(specificCommand);
        }

        string helpText = @"Available commands and shortcuts:

Basic commands:
    - help [command]: Show this help or help for a specific command
    - look (or 'l'): Look around the current location
    - examine [object]: Examine an object more closely
    - move [direction]: Move to a different location
    - take [item]: Pick up an item
    - drop [item]: Drop an item from your inventory
    - inventory (or 'i'): List your items
    - use [item] [on object]: Use an item, possibly on another object
    - talk [character]: Talk to a character
    - say [text]: Say something to the current conversation partner
    - quest: Show active quests
    - status: Show player status

Media commands:
    - view: View media for current location. Press ESCAPE to close.

Inventory commands:
    - equip [item]: Equip a weapon or armor
    - unequip [weapon/armor]: Remove equipped item
    - open [container]: Open a container to see its contents
    - close [container]: Close a container
    - put [item] in [container]: Put an item into a container
    - take [item] from [container]: Take an item from a container

Combat commands:
    - attack [enemy]: Attack an enemy or continue combat
    - flee: Try to escape from combat
    - heavy: Perform a heavy attack (1.5x damage)
    - defend: Raise guard to reduce next damage
    - heal: Restore 15 health
    - skill [heavy/defend/heal]: Use a combat skill
    - stats: Show detailed player statistics

Save commands:
    - save [slot]: Save your game to a slot
    - load [slot]: Load a saved game
    - saves: List available save slots

System commands:
    - clear: Clear the screen
    - about: Show information about the game
    - exit: Exit the game
    - filter [on/off]: Toggle VHS filter overlay

Type 'help [command]' for more details on a specific command.
Use Tab for auto-completion.";

        return helpText;
    }

    private string GetSpecificHelp(string command)
    {
        // More detailed help for specific commands
        Dictionary<string, string> specificHelp = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
       {
           { "look", "Look around the current location to see what's there.\nUsage: look" },
           { "about", "Display information about the game.\nUsage: about" },
           { "examine", "Examine an object or item more closely.\nUsage: examine [object]" },
           { "move", "Move to a different location.\nUsage: go [direction]\nExample: go north" },
           { "take", "Pick up an item and add it to your inventory.\nUsage: take [item]" },
           { "drop", "Drop an item from your inventory.\nUsage: drop [item]" },
           { "inventory", "List all items in your inventory.\nUsage: inventory" },
           { "use", "Use an item, possibly on another object.\nUsage: use [item] or use [item] on [object]" },
           { "talk", "Start a conversation with a character.\nUsage: talk [character]" },
           { "say", "Say something to the character you're talking to.\nUsage: say [text]" },
           { "quest", "Show your active quests and objectives.\nUsage: quest" },
           { "status", "Show your player status, including health, stats, etc.\nUsage: status" },
           { "save", "Save your current game to a slot.\nUsage: save [slot_name]" },
           { "load", "Load a previously saved game.\nUsage: load [slot_name]" },
           { "saves", "List all available save slots.\nUsage: saves" },
           { "attack", "Attack an enemy or continue current combat.\nUsage: attack [optional: enemy_name]" },
           { "flee", "Try to escape from combat.\nUsage: flee" },
           { "stats", "Show detailed player statistics.\nUsage: stats" },
           { "equip", "Equip a weapon or armor.\nUsage: equip [item]" },
           { "unequip", "Remove equipped weapon or armor.\nUsage: unequip weapon or unequip armor" },
           { "open", "Open a container to see its contents.\nUsage: open [container]" },
           { "close", "Close a container.\nUsage: close [container]" },
           { "put", "Put an item into a container.\nUsage: put [item] in [container]" },
           { "take_from", "Take an item from a container.\nUsage: take [item] from [container]" },
           { "view", "View media for current location.s\nPress ESCAPE to close any open media viewer." },
           { "clear", "Clear the terminal screen.\nUsage: clear" },
           { "exit", "Exit the game (your progress will be saved).\nUsage: exit" },
           { "filter", "Toggle VHS filter overlay on/off.\nUsage: filter [on|off]\n" +
                       "- on: Enable VHS filter effect\n" +
                       "- off: Disable VHS filter effect" },
        };

        if (specificHelp.TryGetValue(command, out string helpText))
        {
            return helpText;
        }

        return $"No detailed help available for '{command}'.";
    }

    private string HandleLook(string[] args)
    {
        return _gameState.GetCurrentLocationDescription();
    }

    private string HandleExamine(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to examine?";
        }

        string target = string.Join(" ", args);
        return _gameState.ExamineObject(target);
    }

    private string HandleGo(string[] args)
    {
        if (args.Length == 0)
        {
            return "Where do you want to move?";
        }

        string direction = string.Join(" ", args).ToLower();
        bool success = _gameState.MovePlayer(direction);

        if (success)
        {
            return _gameState.GetCurrentLocationDescription();
        }
        else
        {
            // Better error message with available exits (show user-friendly names)
            var currentLocation = _gameState.GetCurrentLocation();
            if (currentLocation.Exits.Count > 0)
            {
                var availableExits = currentLocation.Exits.Keys.Select(exit => $"'{exit.Replace("_", " ")}'");
                var exitList = string.Join(", ", availableExits);
                return $"You can't move to {direction.Replace("_", " ")} from here. Available exits: {exitList}";
            }
            else
            {
                return "There are no exits from here.";
            }
        }
    }

    private string HandleTake(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to take?";
        }

        string itemName = string.Join(" ", args);
        return _gameState.TakeItem(itemName);
    }

    private string HandleDrop(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to drop?";
        }

        string itemName = string.Join(" ", args);
        return _gameState.DropItem(itemName);
    }

    private string HandleInventory(string[] args)
    {
        return _gameState.GetInventoryDescription();
    }

    private string HandleUse(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to use?";
        }

        // Check for "use [item] on [target]" pattern
        string itemName;
        string target = null;

        int onIndex = Array.IndexOf(args, "on");
        if (onIndex >= 0 && onIndex < args.Length - 1)
        {
            itemName = string.Join(" ", args.Take(onIndex));
            target = string.Join(" ", args.Skip(onIndex + 1));
        }
        else
        {
            itemName = string.Join(" ", args);
        }

        return _gameState.UseItem(itemName, target);
    }

    private string HandleTalk(string[] args)
    {
        if (args.Length == 0)
        {
            return "Who do you want to talk to?";
        }

        string characterName = string.Join(" ", args);
        return _gameState.StartConversation(characterName);
    }

    private string HandleSay(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to say?";
        }

        string text = string.Join(" ", args);
        return _gameState.Say(text);
    }

    private string HandleQuest(string[] args)
    {
        return _gameState.GetQuestLog();
    }

    private string HandleStatus(string[] args)
    {
        return _gameState.GetPlayerStatus();
    }

    // New methods for extended features

    private string HandleSave(string[] args)
    {
        string slotName = args.Length > 0 ? args[0] : "quicksave";
        SaveSystem saveSystem = _gameState.GetSaveSystem();
        saveSystem.SaveGame(_gameState, slotName);
        return $"Game saved to slot '{slotName}'.";
    }

    private string HandleLoad(string[] args)
    {
        if (args.Length == 0)
        {
            // Show available save slots
            var saveSlots = _gameState.GetSaveSystem().GetSaveSlots();
            if (saveSlots.Count == 0)
            {
                return "No save slots found.";
            }

            string result = "Available save slots:";
            foreach (var slot in saveSlots)
            {
                result += $"\n- {slot.SlotName} ({slot.SaveTime}, {slot.LocationName})";
            }
            return result;
        }

        string slotName = args[0];
        bool success = _gameState.GetSaveSystem().LoadGame(slotName, _gameState);

        if (!success)
        {
            return $"Save slot '{slotName}' not found.";
        }

        return $"Game loaded from slot '{slotName}'.\n\n{_gameState.GetCurrentLocationDescription()}";
    }

    private string HandleListSaves(string[] args)
    {
        var saveSlots = _gameState.GetSaveSystem().GetSaveSlots();
        if (saveSlots.Count == 0)
        {
            return "No save slots found.";
        }

        string result = "Available save slots:";
        foreach (var slot in saveSlots)
        {
            result += $"\n- {slot.SlotName} ({slot.SaveTime}, {slot.LocationName})";
        }
        return result;
    }

    private string HandleAttack(string[] args)
    {
        CombatSystem combatSystem = _gameState.GetCombatSystem();

        if (!combatSystem.IsInCombat)
        {
            if (args.Length == 0)
            {
                return "Attack what? There's nothing to attack here.";
            }

            string enemyName = string.Join(" ", args);

            // Find an enemy in the current location
            var currentLocation = _gameState.GetCurrentLocation();
            var enemy = currentLocation.Enemies?.FirstOrDefault(e =>
                e.Name.Equals(enemyName, StringComparison.OrdinalIgnoreCase));

            if (enemy == null)
            {
                return $"There's no '{enemyName}' here to attack.";
            }

            return combatSystem.StartCombat(enemy);
        }
        else
        {
            return combatSystem.Attack();
        }
    }

    private string HandleFlee(string[] args)
    {
        return _gameState.GetCombatSystem().Flee();
    }

    private string HandleStats(string[] args)
    {
        Player player = _gameState.GetPlayer();

        string stats = "Player Statistics:\n";
        stats += $"Health: {player.Health}/{player.MaxHealth}\n";
        stats += $"Level: {player.Level}\n";
        stats += $"Experience: {player.ExperiencePoints}/{player.Level * 100}\n";
        stats += $"Satoshi: ${player.Satoshi}\n"; // Added $ symbol for clarity
        stats += $"\nAttributes:\n";

        foreach (var stat in player.Stats)
        {
            stats += $"- {stat.Key}: {stat.Value}\n";
        }

        stats += $"\nCombat Stats:\n";
        stats += $"- Attack Power: {player.AttackPower}";
        if (player.EquippedWeapon != null)
        {
            stats += $" ({player.AttackPower - player.EquippedWeapon.UseValue} + {player.EquippedWeapon.UseValue} from {player.EquippedWeapon.Name})";
        }
        stats += "\n";

        stats += $"- Defense: {player.Defense}";
        if (player.EquippedArmor != null)
        {
            stats += $" ({player.Defense - player.EquippedArmor.UseValue} + {player.EquippedArmor.UseValue} from {player.EquippedArmor.Name})";
        }

        return stats;
    }

    private string HandleEquip(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to equip?";
        }

        string itemName = string.Join(" ", args);

        // Find the item in inventory
        Player player = _gameState.GetPlayer();
        Item item = player.Inventory.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            return $"You don't have a '{itemName}' in your inventory.";
        }

        if (item.ItemType != ItemType.Weapon && item.ItemType != ItemType.Armor)
        {
            return $"You can't equip the {item.Name}.";
        }

        player.EquipItem(item);

        return $"You equip the {item.Name}.";
    }

    private string HandleUnequip(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to unequip?";
        }

        string itemType = args[0].ToLower();
        Player player = _gameState.GetPlayer();

        if (itemType == "weapon")
        {
            if (player.EquippedWeapon == null)
            {
                return "You don't have a weapon equipped.";
            }

            string weaponName = player.EquippedWeapon.Name;
            player.Unequip(ItemType.Weapon);

            return $"You unequip the {weaponName}.";
        }
        else if (itemType == "armor")
        {
            if (player.EquippedArmor == null)
            {
                return "You don't have armor equipped.";
            }

            string armorName = player.EquippedArmor.Name;
            player.Unequip(ItemType.Armor);

            return $"You unequip the {armorName}.";
        }
        else
        {
            return $"You can't unequip that. Try 'unequip weapon' or 'unequip armor'.";
        }
    }

    private string HandleOpen(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to open?";
        }

        string containerName = string.Join(" ", args);

        // Check if target is in location's items
        var currentLocation = _gameState.GetCurrentLocation();
        Item item = currentLocation.Items.FirstOrDefault(i =>
            i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));

        // Check if target is in inventory
        if (item == null)
        {
            Player player = _gameState.GetPlayer();
            item = player.Inventory.FirstOrDefault(i =>
                i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));
        }

        if (item == null)
        {
            return $"You don't see any '{containerName}' here.";
        }

        if (!(item is Container container))
        {
            return $"You can't open the {item.Name}.";
        }

        container.IsOpen = true;
        return $"You open the {container.Name}.\n\n{container.GetContentsDescription()}";
    }

    private string HandleClose(string[] args)
    {
        if (args.Length == 0)
        {
            return "What do you want to close?";
        }

        string containerName = string.Join(" ", args);

        // Check location and inventory for the container
        var currentLocation = _gameState.GetCurrentLocation();
        Player player = _gameState.GetPlayer();

        Item item = currentLocation.Items.FirstOrDefault(i =>
            i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase)) ??
            player.Inventory.FirstOrDefault(i =>
                i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            return $"You don't see any '{containerName}' here.";
        }

        if (!(item is Container container))
        {
            return $"You can't close the {item.Name}.";
        }

        container.IsOpen = false;
        return $"You close the {container.Name}.";
    }

    private string HandlePutIn(string[] args)
    {
        // Syntax: "put [item] in [container]"
        if (args.Length < 3)
        {
            return "Please specify what to put where. Use format: put [item] in [container]";
        }

        int inIndex = Array.IndexOf(args, "in");
        if (inIndex <= 0 || inIndex >= args.Length - 1)
        {
            return "Please use the format: put [item] in [container]";
        }

        string itemName = string.Join(" ", args.Take(inIndex));
        string containerName = string.Join(" ", args.Skip(inIndex + 1));

        // Find the item in inventory
        Player player = _gameState.GetPlayer();
        Item item = player.Inventory.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            return $"You don't have a '{itemName}' in your inventory.";
        }

        // Find the container
        var currentLocation = _gameState.GetCurrentLocation();

        Item containerItem = currentLocation.Items.FirstOrDefault(i =>
            i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase)) ??
            player.Inventory.FirstOrDefault(i =>
                i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));

        if (containerItem == null)
        {
            return $"You don't see any '{containerName}' here.";
        }

        if (!(containerItem is Container container))
        {
            return $"You can't put things in the {containerItem.Name}.";
        }

        if (!container.IsOpen)
        {
            return $"The {container.Name} is closed.";
        }

        return _gameState.GetInventorySystem().TransferItem(item, container, true);
    }

    private string HandleTakeFrom(string[] args)
    {
        // Syntax: "take [item] from [container]"
        if (args.Length < 3)
        {
            return "Please specify what to take from where. Use format: take [item] from [container]";
        }

        int fromIndex = Array.IndexOf(args, "from");
        if (fromIndex <= 0 || fromIndex >= args.Length - 1)
        {
            return "Please use the format: take [item] from [container]";
        }

        string itemName = string.Join(" ", args.Take(fromIndex));
        string containerName = string.Join(" ", args.Skip(fromIndex + 1));

        // Find the container
        var currentLocation = _gameState.GetCurrentLocation();
        Player player = _gameState.GetPlayer();

        Item containerItem = currentLocation.Items.FirstOrDefault(i =>
            i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase)) ??
            player.Inventory.FirstOrDefault(i =>
                i.Name.Equals(containerName, StringComparison.OrdinalIgnoreCase));

        if (containerItem == null)
        {
            return $"You don't see any '{containerName}' here.";
        }

        if (!(containerItem is Container container))
        {
            return $"You can't take things from the {containerItem.Name}.";
        }

        if (!container.IsOpen)
        {
            return $"The {container.Name} is closed.";
        }

        // Find the item in container
        Item item = container.Items.FirstOrDefault(i =>
            i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));

        if (item == null)
        {
            return $"There's no '{itemName}' in the {container.Name}.";
        }

        return _gameState.GetInventorySystem().TransferItem(item, container, false);
    }

    private string HandleClear(string[] args)
    {
        // This will be handled by the CLIEmulator class
        return "[CLEAR]";
    }

    private string HandleExit(string[] args)
    {
        _gameState.SaveGame();
        return "Thanks for playing! The game has been saved.";
    }

    // NEW: Handle crash command
    private string HandleCrash(string[] args)
    {
        if (_isCrashing)
        {
            return ""; // Already crashing, no response
        }

        if (_cliEmulator == null)
        {
            return "ERROR: Cannot initiate crash sequence - CLI emulator not available.";
        }

        _isCrashing = true;
        _crashIndex = 0;
        _crashStartTime = 0;

        // Initialize crash timer with much faster interval
        if (_crashTimer != null)
        {
            _crashTimer.QueueFree();
        }

        _crashTimer = new Timer();
        _cliEmulator.AddChild(_crashTimer);
        _crashTimer.WaitTime = 0.05f + (float)_crashRandom.NextDouble() * 0.1f; // Very fast: 0.05-0.15 seconds
        _crashTimer.Timeout += OnCrashTimerTimeout;
        _crashTimer.Start();

        return "INITIATING SYSTEM CRASH SEQUENCE...";
    }

    // NEW: Crash timer callback
    private void OnCrashTimerTimeout()
    {
        if (!_isCrashing || _cliEmulator == null)
        {
            return;
        }

        // Check if crash duration exceeded
        _crashStartTime += (float)_crashTimer.WaitTime;
        if (_crashStartTime >= CRASH_DURATION)
        {
            // End crash and exit game
            _isCrashing = false;
            if (_crashTimer != null)
            {
                _crashTimer.Stop();
                _crashTimer.QueueFree();
                _crashTimer = null;
            }

            _cliEmulator.DisplayError("SYSTEM TERMINATED");

            // Exit the game after a brief delay
            var exitTimer = new Timer();
            _cliEmulator.AddChild(exitTimer);
            exitTimer.WaitTime = 1.0f;
            exitTimer.OneShot = true;
            exitTimer.Timeout += () =>
            {
                _gameState.SaveGame();
                _cliEmulator.GetTree().Quit();
            };
            exitTimer.Start();

            return;
        }

        // Get a random crash message (no corruption)
        string crashMessage = _crashMessages[_crashRandom.Next(_crashMessages.Length)];

        // Display the crash message with error color
        _cliEmulator.DisplayError($"[{_crashIndex:D4}] {crashMessage}");

        _crashIndex++;

        // Set next timer with very fast random interval
        _crashTimer.WaitTime = 0.03f + (float)_crashRandom.NextDouble() * 0.07f; // 0.03-0.1 seconds
        _crashTimer.Start();
    }
    
    // NEW: Handle minimap command
    private string HandleMinimap(string[] args)
    {
        if (_cliEmulator?.GetImprovedUI() == null)
        {
            return "UI system not available.";
        }

        var ui = _cliEmulator.GetImprovedUI();

        if (args.Length > 0)
        {
            string action = args[0].ToLower();
            if (action == "on" || action == "enable")
            {
                if (!ui.IsMinimapEnabled())
                {
                    _cliEmulator.ToggleMinimap();
                    ui.SaveUserPreferences();
                    return "Minimap enabled.";
                }
                return "Minimap is already enabled.";
            }
            else if (action == "off" || action == "disable")
            {
                if (ui.IsMinimapEnabled())
                {
                    _cliEmulator.ToggleMinimap();
                    ui.SaveUserPreferences();
                    return "Minimap disabled.";
                }
                return "Minimap is already disabled.";
            }
            else if (action == "toggle")
            {
                _cliEmulator.ToggleMinimap();
                ui.SaveUserPreferences();
                return ui.IsMinimapEnabled() ? "Minimap enabled." : "Minimap disabled.";
            }
        }

        // Default: show minimap
        if (ui.IsMinimapEnabled())
        {
            _cliEmulator.DisplayMinimap();
            return "";
        }
        else
        {
            return "Minimap is disabled. Use 'minimap on' to enable it.";
        }
    }

    // NEW: Enhanced combat command handlers
    private string HandleStance(string[] args)
    {
        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            if (args.Length == 0)
            {
                return "Available stances: balanced, aggressive, defensive";
            }

            string stanceName = args[0].ToLower();
            CombatStance stance = stanceName switch
            {
                "balanced" => CombatStance.Balanced,
                "aggressive" => CombatStance.Aggressive,
                "defensive" => CombatStance.Defensive,
                _ => CombatStance.Balanced
            };

            if (stanceName != "balanced" && stanceName != "aggressive" && stanceName != "defensive")
            {
                return $"Unknown stance: {stanceName}. Available: balanced, aggressive, defensive";
            }

            return enhancedCombat.SetStance(stance);
        }

        return "Enhanced combat system not available.";
    }

    private string HandleHeavyAttack(string[] args)
    {
        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            return enhancedCombat.UseSkill("heavy");
        }

        return "Enhanced combat system not available.";
    }

    private string HandleDodge(string[] args)
    {
        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            return enhancedCombat.UseSkill("dodge");
        }

        return "Enhanced combat system not available.";
    }

    private string HandleDefend(string[] args)
    {
        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            return enhancedCombat.UseSkill("defend");
        }

        return "Enhanced combat system not available.";
    }

    private string HandleTaunt(string[] args)
    {
        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            return enhancedCombat.UseSkill("taunt");
        }

        return "Enhanced combat system not available.";
    }

    private string HandleSkill(string[] args)
    {
        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            if (args.Length == 0)
            {
                return "Available skills: fireball, ice_shard, heal";
            }

            string skillName = args[0].ToLower();
            return enhancedCombat.UseSkill(skillName);
        }

        return "Enhanced combat system not active.";
    }

    private string HandleCombo(string[] args)
    {
        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            return "Simplified Combat System:\n" +
                   "- Use 'stance' to change between balanced, aggressive, and defensive\n" +
                   "- Use 'heavy' for powerful attacks\n" +
                   "- Use 'defend' to reduce incoming damage\n" +
                   "- Use 'heal' to restore health\n" +
                   "- Watch out for status effects like poison!";
        }

        return "Enhanced combat system not available.";
    }

    private string HandleStatusEffect(string[] args)
    {
        if (args.Length < 3)
        {
            return "Usage: status_effect [target] [effect] [duration]\n" +
                   "Target: self/enemy\n" +
                   "Effects: poisoned, stunned, blessed, cursed, shielded";
        }

        if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
        {
            string target = args[0];
            string effectName = args[1];

            if (!int.TryParse(args[2], out int duration))
            {
                return "Invalid duration. Please use a number.";
            }

            StatusEffect effect = effectName.ToLower() switch
            {
                "poisoned" => StatusEffect.Poisoned,
                "stunned" => StatusEffect.Stunned,
                "blessed" => StatusEffect.Blessed,
                "cursed" => StatusEffect.Cursed,
                "shielded" => StatusEffect.Shielded,
                _ => StatusEffect.None
            };

            if (effect == StatusEffect.None)
            {
                return $"Unknown status effect: {effectName}";
            }

            return enhancedCombat.ApplyStatusEffect(target, effect, duration);
        }

        return "Enhanced combat system not available.";
    }

    private string HandleHeal(string[] args)
    {
        if (_gameState.GetCombatSystem().IsInCombat)
        {
            // In combat, use the combat system's heal
            if (_gameState.GetCombatSystem() is EnhancedCombatSystem enhancedCombat)
            {
                return enhancedCombat.UseSkill("heal");
            }
            else
            {
                // Basic heal for regular combat system
                int healAmount = 15;
                _gameState.GetPlayer().Health = Math.Min(
                    _gameState.GetPlayer().MaxHealth, 
                    _gameState.GetPlayer().Health + healAmount);
                return $"You focus and restore {healAmount} health.";
            }
        }
        else
        {
            // Out of combat heal
            int healAmount = 10;
            var player = _gameState.GetPlayer();
            int actualHeal = Math.Min(healAmount, player.MaxHealth - player.Health);
            
            if (actualHeal <= 0)
            {
                return "You are already at full health.";
            }
            
            player.Health += actualHeal;
            return $"You rest and recover {actualHeal} health.";
        }
    }

    // NEW: Handle about command
    private string HandleAbout(string[] args)
    {
        string aboutText = @"ÆTHER WEBNEVER: ARMOUR is a text-driven narrative experience that blends storytelling with striking visual art. Originally envisioned as a 3D title, ARMOUR has gone through three major revisions since 2023. Due to limited 3D assets, the scope was refocused to highlight its strongest elements—story and atmosphere. Set for release on December 9th, 2025—the same day the bombs fall in-game—ARMOUR will be free to play on Steam, itch.io, and the WEBNEVER site. As NOWARE OS evolves, plans are in place to remaster ARMOUR and continue expanding the ÆTHER WEBNEVER universe with new stories. AI tools played a crucial role in bringing this solo project to life, making it possible to fully realize its unique vision. ARMOUR is also an experiment in AI-assisted storytelling, enhancing human creativity through modern tools to explore new frontiers in narrative design. The story of ARMOUR is inspired by real mystical experiences from the life of WEBNEVER.";
        return aboutText;
    }

    // NEW: Handle filter command
    private string HandleFilter(string[] args)
    {
        if (_cliEmulator == null)
        {
            return "Filter system not available.";
        }
        
        if (args.Length == 0)
        {
            // Show current filter status
            bool isEnabled = _cliEmulator.IsVHSFilterEnabled();
            return $"VHS filter is currently {(isEnabled ? "enabled" : "disabled")}. Use 'filter on' or 'filter off' to change.";
        }
        
        string action = args[0].ToLower();
        
        switch (action)
        {
            case "on":
            case "enable":
                _cliEmulator.SetVHSFilter(true);
                return "VHS filter enabled.";
            
            case "off":
            case "disable":
                _cliEmulator.SetVHSFilter(false);
                return "VHS filter disabled.";
            
            case "toggle":
                bool newState = !_cliEmulator.IsVHSFilterEnabled();
                _cliEmulator.SetVHSFilter(newState);
                return $"VHS filter {(newState ? "enabled" : "disabled")}.";
            
            default:
                return $"Unknown filter option: {action}. Use 'on', 'off', or 'toggle'.";
        }
    }
}