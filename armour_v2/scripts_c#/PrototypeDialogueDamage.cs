using Godot;
using System;
using System.Collections.Generic;

public partial class PrototypeDialogueDamage : Node3D, IInteractable
{
    private RandomNumberGenerator rng = new RandomNumberGenerator();
    private DialogueManager dialogueManager;
    private TerminalConsole _debugConsole;
    private ShaderMaterial _highlightMaterial;
    private MeshInstance3D _meshInstance;

    public override void _Ready()
    {
        rng.Randomize(); // Initialize the RNG with a random seed
        dialogueManager = GetNode<DialogueManager>("/root/DialogueManager");
        _debugConsole = GetNode<TerminalConsole>("/root/mainScene/Control/uiElements/TerminalConsole");

        _meshInstance = GetParent<MeshInstance3D>(); // Adjust the node path as needed
        if (_meshInstance != null && _meshInstance.MaterialOverlay is ShaderMaterial material)
        {
            _highlightMaterial = material;
            // Initialize as not highlighted
            _highlightMaterial.SetShaderParameter("is_highlighted", false);
        }

        // Subscribe to the currentInteractable changed event
        var player = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
        if (player != null)
        {
            player.CurrentInteractableChanged += OnCurrentInteractableChanged;
        }
    }

    private void OnCurrentInteractableChanged(IInteractable newInteractable)
    {
        if (_highlightMaterial != null)
        {
            bool shouldHighlight = newInteractable == this;
            _highlightMaterial.SetShaderParameter("is_highlighted", shouldHighlight);
        }
    }

    public override void _ExitTree()
    {
        // Unsubscribe from the event when the node is removed
        var player = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
        if (player != null)
        {
            player.CurrentInteractableChanged -= OnCurrentInteractableChanged;
        }
        base._ExitTree();
    }

    public void Interact()
    {
        var player = GetTree().GetFirstNodeInGroup("Player") as PlayerController; // Cast to your Player class; ensure this matches your implementation
        if (player != null)
        {
            player.HP -= (float)rng.RandfRange(100.0f, 200.0f); // Assuming Hp and Mp are publicly accessible in your Player class
            player.MP -= (float)rng.RandfRange(1.0f, 5.0f);
        }
        //dialogueManager.StartDialogue("prototype_level_1_scene"); // Ensure DialogueManager is accessible
    }

    public List<ContextMenuAction> GetContextActions()
    {
        GD.Print("=== Getting Context Actions ===");
        var actions = new List<ContextMenuAction>();

        // Create actions with debug prints
        var examineAction = new ContextMenuAction("Examine", () =>
        {
            GD.Print("Examine callback created");
            OnExamine();
        });

        var useAction = new ContextMenuAction("Use", () =>
        {
            GD.Print("Use callback created");
            OnUse();
        });

        actions.Add(examineAction);
        actions.Add(useAction);

        GD.Print($"Created {actions.Count} actions");
        foreach (var action in actions)
        {
            GD.Print($"Action: {action.Label}, Has Callback: {action.Callback != null}");
        }
        return actions;
    }
    
    private void OnExamine()
    {
        GD.Print("OnExamine called");
        DebugLog("It's a cube that damages you.");
    }

    private void OnPickUp()
    {
        DebugLog("You can't pick it up.");
    }

    private void OnUse()
    {
        GD.Print("OnUse called");
        DebugLog("Using the damaging cube.");
        Interact();
    }

    private void DebugLog(string message, string type = "normal")
    {
        if (_debugConsole != null)
        {
            _debugConsole.PrintMessage($"{message}", type);
        }
    }
}
