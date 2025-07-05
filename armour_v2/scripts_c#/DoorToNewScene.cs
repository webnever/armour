using Godot;
using System;
using System.Collections.Generic;

public partial class DoorToNewScene : Node3D, IInteractable
{
    [Export]
    public string PathOfSceneToSwitchTo { get; set; }

    private ShaderMaterial _highlightMaterial;
    private MeshInstance3D _meshInstance;

    public override void _Ready(){
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
        GetNode<GameSceneManager>("/root/mainScene/GameSceneManager").SwitchScene(PathOfSceneToSwitchTo, true);
    }

    public List<ContextMenuAction> GetContextActions()
    {
        return new List<ContextMenuAction>
        {
            new ContextMenuAction("Examine", () => GD.Print("Examining object...")),
            new ContextMenuAction("Pick Up", () => GD.Print("Picking up object...")),
            new ContextMenuAction("Use", () => GD.Print("Using object..."))
        };
    }
}
