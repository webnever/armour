using Godot;
using System;
using System.Collections.Generic;

// Interface for the motorcycle to implement
public interface IVehicle
{
    void Mount(PlayerController player);
    void Dismount();
    Vector3 GetDismountPosition();
}

public partial class Motorcycle : CharacterBody3D, IVehicle, IInteractable
{
    [Export] public NodePath PlayerPath;
    private PlayerController player;
    private MotorcycleController controller;
    private bool isOccupied = false;
    
    // Mount/dismount offset
    [Export] private Vector3 dismountOffset = new Vector3(1.5f, 0, 0);
    
    public override void _Ready()
    {
        base._Ready();
        
        // Get controller reference
        controller = GetNode<MotorcycleController>("MotorcycleController");
        if (controller == null)
        {
            GD.PrintErr("MotorcycleController not found!");
            return;
        }
        
        // Get player reference from group
        var playersInGroup = GetTree().GetNodesInGroup("Player");
        if (playersInGroup.Count > 0)
        {
            player = playersInGroup[0] as PlayerController;
            if (player == null)
            {
                GD.PrintErr("Node in 'Player' group is not a PlayerController!");
            }
        }
        else
        {
            GD.PrintErr("No nodes found in 'Player' group!");
        }
                
        // Disable motorcycle physics initially
        controller.ProcessMode = ProcessModeEnum.Disabled;
    }
    
    public void Mount(PlayerController newPlayer)
    {
        if (isOccupied) return;
        
        player = newPlayer;
        isOccupied = true;
        
        // Enable motorcycle controller
        controller.ProcessMode = ProcessModeEnum.Always;
        
        // Parent the player to the motorcycle and adjust their position
        player.GlobalPosition = GlobalPosition;
        player.Reparent(this);
        
        // Disable player's movement controller but keep them visible
        player.DisableInput();
        player.SetPhysicsMode(false);
        
        // Adjust player animation/position for riding pose
        player.PlayAnimation("motorcycle_idle");
    }
    
    public void Dismount()
    {
        if (!isOccupied || player == null) return;
        
        // Calculate dismount position
        Vector3 dismountPos = GetDismountPosition();
        
        // Reparent player back to the original scene
        var mainScene = GetTree().Root.GetNode("mainScene");
        player.Reparent(mainScene);
        
        // Move player to dismount position
        player.GlobalPosition = dismountPos;
        
        // Re-enable player controller
        player.EnableInput();
        player.SetPhysicsMode(true);
        
        // Reset player state
        player.PlayAnimation("idle");
        
        // Disable motorcycle controller
        controller.ProcessMode = ProcessModeEnum.Disabled;
        
        player = null;
        isOccupied = false;
    }
    
    public Vector3 GetDismountPosition()
    {
        // Transform the local dismount offset to global coordinates based on motorcycle's rotation
        return GlobalPosition + Transform.Basis * dismountOffset;
    }
    
    // IInteractable implementation
    public void Interact()
    {
        if (!isOccupied && player != null)
        {
            Mount(player);
        }
    }
    
    public override void _Input(InputEvent @event)
    {
        // Check for dismount input when motorcycle is occupied
        if (isOccupied && Input.IsActionJustPressed("dismount"))
        {
            Dismount();
        }
    }

	public List<ContextMenuAction> GetContextActions()
    {
        return new List<ContextMenuAction>
    {
        new ContextMenuAction("Examine", OnExamine),
        new ContextMenuAction("Jump on", OnUse)
    };
    }

	private void OnExamine()
    {
        GlobalTerminal.Print("M.M.'s Motorcycle.");
    }

    private void OnUse()
    {
		Interact();
    }
}