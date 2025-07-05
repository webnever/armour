using Godot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

public class NaturalLanguageParser
{
    // Command synonyms and variations
    private readonly Dictionary<string, List<string>> _commandSynonyms = new Dictionary<string, List<string>>
    {
        { "look", new List<string> { "l", "examine", "inspect", "view", "check", "observe", "see" } },
        { "take", new List<string> { "get", "grab", "pick", "pickup", "collect", "obtain", "acquire" } },
        { "drop", new List<string> { "put", "place", "leave", "abandon", "discard", "release" } },
        { "use", new List<string> { "apply", "utilize", "employ", "activate", "operate" } },
        { "go", new List<string> { "move", "walk", "travel", "head", "proceed", "advance" } },
        { "talk", new List<string> { "speak", "chat", "converse", "discuss", "communicate" } },
        { "attack", new List<string> { "hit", "strike", "fight", "assault", "engage", "battle" } },
        { "inventory", new List<string> { "i", "inv", "items", "bag", "belongings", "possessions" } },
        { "help", new List<string> { "h", "?", "commands", "instructions", "assist" } },
        { "save", new List<string> { "store", "preserve", "keep", "record" } },
        { "load", new List<string> { "restore", "retrieve", "recover", "continue" } }
    };

    // Prepositions and articles to filter out
    private readonly HashSet<string> _filterWords = new HashSet<string>
    {
        "the", "a", "an", "to", "at", "in", "on", "with", "from", "up", "down",
        "into", "onto", "upon", "around", "through", "across", "over", "under"
    };

    // Direction mappings
    private readonly Dictionary<string, string> _directionMappings = new Dictionary<string, string>
    {
        { "n", "north" }, { "s", "south" }, { "e", "east" }, { "w", "west" },
        { "ne", "northeast" }, { "nw", "northwest" }, { "se", "southeast" }, { "sw", "southwest" },
        { "u", "up" }, { "d", "down" }, { "in", "enter" }, { "out", "exit" }
    };

    public ParsedCommand Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            return null;

        input = input.ToLower().Trim();
        
        // Handle special cases first
        if (_directionMappings.ContainsKey(input))
        {
            return new ParsedCommand { Command = "go", Arguments = new[] { _directionMappings[input] } };
        }

        // Tokenize and filter
        var tokens = Tokenize(input);
        if (tokens.Count == 0)
            return null;

        // Extract command
        string command = ExtractCommand(tokens);
        if (string.IsNullOrEmpty(command))
            return null;

        // Extract arguments
        var arguments = ExtractArguments(tokens, command);
        
        return new ParsedCommand
        {
            Command = command,
            Arguments = arguments.ToArray(),
            Original = input
        };
    }

    private List<string> Tokenize(string input)
    {
        // Split by whitespace but preserve quoted strings
        var regex = new Regex(@"[\""].+?[\""]|[^ ]+");
        var matches = regex.Matches(input);
        
        var tokens = new List<string>();
        foreach (Match match in matches)
        {
            string token = match.Value.Trim('"');
            if (!_filterWords.Contains(token))
            {
                tokens.Add(token);
            }
        }
        
        return tokens;
    }

    private string ExtractCommand(List<string> tokens)
    {
        foreach (var token in tokens)
        {
            // Check if token is a known command or synonym
            foreach (var kvp in _commandSynonyms)
            {
                if (kvp.Key == token || kvp.Value.Contains(token))
                {
                    return kvp.Key;
                }
            }
        }
        
        // If no known command found, assume first token is command
        return tokens[0];
    }

    private List<string> ExtractArguments(List<string> tokens, string command)
    {
        var arguments = new List<string>();
        bool commandFound = false;
        
        foreach (var token in tokens)
        {
            if (!commandFound)
            {
                // Skip until we find the command
                if (IsCommandToken(token, command))
                {
                    commandFound = true;
                }
                continue;
            }
            
            // Add everything after the command as arguments
            arguments.Add(token);
        }
        
        // Handle special patterns
        arguments = ProcessSpecialPatterns(arguments, command);
        
        return arguments;
    }

    private bool IsCommandToken(string token, string command)
    {
        if (token == command)
            return true;
            
        if (_commandSynonyms.TryGetValue(command, out var synonyms))
        {
            return synonyms.Contains(token);
        }
        
        return false;
    }

    private List<string> ProcessSpecialPatterns(List<string> arguments, string command)
    {
        // Handle "use X on Y" pattern
        if (command == "use" && arguments.Contains("on"))
        {
            int onIndex = arguments.IndexOf("on");
            if (onIndex > 0 && onIndex < arguments.Count - 1)
            {
                var item = string.Join(" ", arguments.Take(onIndex));
                var target = string.Join(" ", arguments.Skip(onIndex + 1));
                return new List<string> { item, "on", target };
            }
        }
        
        // Handle compound objects (e.g., "red key" or "old wizard")
        var processed = new List<string>();
        for (int i = 0; i < arguments.Count; i++)
        {
            if (i < arguments.Count - 1 && IsAdjective(arguments[i]))
            {
                processed.Add($"{arguments[i]} {arguments[i + 1]}");
                i++; // Skip next token
            }
            else
            {
                processed.Add(arguments[i]);
            }
        }
        
        return processed;
    }

    private bool IsAdjective(string word)
    {
        // Common adjectives in the game
        var adjectives = new HashSet<string>
        {
            "red", "blue", "green", "old", "new", "ancient", "rusty", "shiny",
            "large", "small", "heavy", "light", "dark", "bright", "mysterious"
        };
        
        return adjectives.Contains(word);
    }
}

public class ParsedCommand
{
    public string Command { get; set; }
    public string[] Arguments { get; set; }
    public string Original { get; set; }
}