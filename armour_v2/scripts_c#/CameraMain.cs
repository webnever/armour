using Godot;
using System;

public partial class CameraMain : Camera3D
{
    public override void _Ready()
    {
        GD.Print("CameraMain Ready called");
        
        var player = GetTree().GetFirstNodeInGroup("Player") as PlayerController;
        if (player != null) 
        { 
            player.SetCamera(this);
            GD.Print("Camera set on player");
        }
        else
        {
            GD.Print("Player not found!");
        }

        // Initialize DamageNumbers with camera reference
        var damageNumbers = GetNode<DamageNumbers>("/root/DamageNumbers");
        var viewport = GetNode<SubViewport>("/root/mainScene/Control/SubViewport");
        
        if (damageNumbers != null && viewport != null)
        {
            damageNumbers.Initialize(this, viewport);
            GD.Print("DamageNumbers initialized");
        }
        else
        {
            GD.Print($"Failed to initialize DamageNumbers: DamageNumbers exists: {damageNumbers != null}, Viewport exists: {viewport != null}");
        }
    }
}