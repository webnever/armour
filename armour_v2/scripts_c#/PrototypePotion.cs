using Godot;
using System;
using System.Collections.Generic; // For using Dictionary

// Example implementation for a saveable object (like your PrototypePotion)
public partial class PrototypePotion : Node3D, ISaveable
{
    public string Id => Name;

    public Godot.Collections.Dictionary GetState()
    {
        return new Godot.Collections.Dictionary { { "visible", Visible } };
    }

    public void SetState(Godot.Collections.Dictionary state)
    {
        if (state.ContainsKey("visible"))
        {
            bool visible = (bool)state["visible"];
            if (visible)
                ShowAndShowChildren();
            else
                HideAndHideChildren();
        }
    }

    public override void _Ready()
    {
        var gameStateManager = GetNode<GameSceneManager>("/root/mainScene/GameSceneManager");
        gameStateManager.RegisterSaveable(this, GetParent().Name);
    }

    private void HideAndHideChildren()
    {
        foreach (Node staticBody in GetChildren())
        {
            foreach (Node child in staticBody.GetChildren())
            {
                if (child is CollisionShape3D collider)
                {
                    collider.Disabled = true;
                }
            }
        }
        Visible = false;
    }

    private void ShowAndShowChildren()
    {
        foreach (Node staticBody in GetChildren())
        {
            foreach (Node child in staticBody.GetChildren())
            {
                if (child is CollisionShape3D collider)
                {
                    collider.Disabled = false;
                }
            }
        }
        Visible = true;
    }
}