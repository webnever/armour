using Godot;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.IO;
using System.Collections.Generic;
using System.Collections;
using System.Threading.Tasks;

public partial class DialogueManager : Node
{
    private Dictionary<string, List<Dictionary<string, object>>> dialogues = new Dictionary<string, List<Dictionary<string, object>>>();
    private List<Dictionary<string, object>> currentDialogue = new List<Dictionary<string, object>>();
    private int currentPart = 0;
    private bool isInitialized = false;
    public bool inDialogue = false;
    public DialogueBox dialogueBox;
    private Label dialogueBoxText;
    private PlayerController player;
    private bool isAnimating = false;
    private Vector2 viewportMousePosition;
    private Resource arrow;
    private Resource link;

    public override void _Ready()
    {
        arrow = ResourceLoader.Load("res://cursor/tex_cursor_pointer.png");
        link = ResourceLoader.Load("res://cursor/tex_cursor_link.png");
        dialogueBox = GetNode<DialogueBox>("/root/mainScene/DialogueBox");
        dialogueBoxText = dialogueBox.GetChild(0) as Label;
        player = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
        if (!isInitialized)
        {
            LoadDialogues("res://dialogue.json");
            isInitialized = true;
        }
    }

    private void LoadDialogues(string path)
    {
        var file = Godot.FileAccess.Open(path, Godot.FileAccess.ModeFlags.Read);
        var jsonString = file.GetAsText();
        Dictionary<string, object> jsonAsDict = JsonConvert.DeserializeObject<Dictionary<string, object>>(jsonString);
        JArray dialogueArray = (JArray)jsonAsDict["english"];
        dialogues.Clear();
        foreach (JObject aDialogue in dialogueArray)
        {
            if (aDialogue.ContainsKey("id"))
            {
                string dialogueId = (string)aDialogue.GetValue("id");
                List<Dictionary<string, object>> dialogueParts = aDialogue.GetValue("parts").ToObject<List<Dictionary<string, object>>>();
                dialogues[dialogueId] = dialogueParts;
            }
        }
    }

    public async void StartDialogue(string id)
    {
        GD.Print("Starting dialogue with ID: " + id);
        if (dialogues.ContainsKey(id))
        {
            inDialogue = true;
            isAnimating = true;
            player.DisableInput();
            currentDialogue = (List<Dictionary<string, object>>)dialogues[id];
            currentPart = 0;
            await ShowNextPart();
        }
        else
        {
            GD.Print("Dialogue ID not found: " + id);
        }
    }

    public async Task ShowNextPart()
    {
        if (currentPart >= currentDialogue.Count)
        {
            await EndDialogue();
            return;
        }

        // If not the first dialogue part, hide the current dialogue first
        if (currentPart > 0)
        {
            await dialogueBox.HideUIElement();
        }

        Dictionary<string, object> part = currentDialogue[currentPart];
        dialogueBoxText.Text = (string)part["text"];
        await dialogueBox.RevealUIElement();
        currentPart++;
    }

    private async Task EndDialogue()
    {
        await dialogueBox.HideUIElement();
        await ToSignal(GetTree().CreateTimer(0.5), "timeout");
        player.EnableInput();
        inDialogue = false;
        isAnimating = false;
    }

    public override void _Input(InputEvent @event)
    {
        // Your input handling code here
    }
}