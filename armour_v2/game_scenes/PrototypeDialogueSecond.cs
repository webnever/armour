using Godot;
using System;
using System.Collections.Generic;

public partial class PrototypeDialogueSecond : Node3D, IInteractable
{
	private DialogueManager dialogueManager;
	private TerminalConsole _debugConsole;

    private ShaderMaterial _highlightMaterial;
    private MeshInstance3D _meshInstance;

	public override void _Ready()
	{
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
		dialogueManager.StartDialogue("secondbox"); // Ensure DialogueManager is accessible
	}

	public List<ContextMenuAction> GetContextActions()
    {
        return new List<ContextMenuAction>
    {
        new ContextMenuAction("Examine", OnExamine),
        new ContextMenuAction("Talk To", OnTalkTo),
        new ContextMenuAction("Use", OnUse)
    };
    }

    private void OnExamine()
    {
        DebugLog("It's a cube that talks to you.");
    }

    private void OnTalkTo()
    {
        Interact();
    }

    private void OnUse()
    {
        DebugLog("You can't use it");
    }

    private void DebugLog(string message, string type = "normal")
    {
        if (_debugConsole != null)
        {
            _debugConsole.PrintMessage($"{message}", type);
        }
    }
}
