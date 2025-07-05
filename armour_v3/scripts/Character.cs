using Godot;
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// Represents a character (NPC)
public class Character
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Description { get; set; }
    public Dictionary<string, DialogueNode> Dialogue { get; set; } = new Dictionary<string, DialogueNode>();
    public string CurrentDialogueId { get; set; } = "greeting";
    
    public string StartConversation()
    {
        CurrentDialogueId = "greeting";
        if (Dialogue.TryGetValue(CurrentDialogueId, out DialogueNode node))
        {
            return $"{Name}: \"{node.Text}\"";
        }
        
        return $"{Name} doesn't have anything to say.";
    }
    
    public string RespondTo(string playerText)
    {
        if (Dialogue.TryGetValue(CurrentDialogueId, out DialogueNode currentNode))
        {
            foreach (var response in currentNode.Responses)
            {
                if (Regex.IsMatch(playerText, response.Pattern, RegexOptions.IgnoreCase))
                {
                    CurrentDialogueId = response.NextDialogueId;
                    
                    if (Dialogue.TryGetValue(CurrentDialogueId, out DialogueNode nextNode))
                    {
                        if (response.Action != null)
                        {
                            response.Action();
                        }
                        
                        return $"{Name}: \"{nextNode.Text}\"";
                    }
                }
            }
            
            // If no matching response, use default
            if (!string.IsNullOrEmpty(currentNode.DefaultResponseId))
            {
                CurrentDialogueId = currentNode.DefaultResponseId;
                if (Dialogue.TryGetValue(CurrentDialogueId, out DialogueNode defaultNode))
                {
                    return $"{Name}: \"{defaultNode.Text}\"";
                }
            }
        }
        
        return $"{Name} doesn't understand.";
    }
}

// Represents a dialogue node in a conversation
public class DialogueNode
{
    public string Id { get; set; }
    public string Text { get; set; }
    public List<DialogueResponse> Responses { get; set; } = new List<DialogueResponse>();
    public string DefaultResponseId { get; set; }
}

// Represents a possible response in a dialogue
public class DialogueResponse
{
    public string Pattern { get; set; }
    public string NextDialogueId { get; set; }
    public Action Action { get; set; }
}