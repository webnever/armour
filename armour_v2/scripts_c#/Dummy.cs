using Godot;
using System;
using System.Collections.Generic;

public partial class Dummy : StaticBody3D, IDamageable //, IInteractable
{
    private DialogueManager dialogueManager;
    private DamageNumbers damageNumbers;
    private Node3D damageNumbersOrigin;
    private float maxHealth = 10000f;
    private float currentHealth;
    private float previousHealth;
    private SubViewport subViewport;
    private Camera3D camera;

    //private TerminalConsole _debugConsole;

    public override void _Ready()
    {
        dialogueManager = GetNode<DialogueManager>("/root/DialogueManager");
        subViewport = GetNode<SubViewport>("/root/mainScene/Control/SubViewport");
        camera = subViewport.GetCamera3D();
        damageNumbers = GetNode<DamageNumbers>("/root/DamageNumbers");
        //_debugConsole = GetNode<TerminalConsole>("/root/mainScene/Control/uiElements/TerminalConsole");

        damageNumbersOrigin = new Node3D();
        AddChild(damageNumbersOrigin);
        damageNumbersOrigin.Position = new Vector3(0, 2, 0);

        currentHealth = maxHealth;
        previousHealth = maxHealth;
    }

    public override void _Process(double delta)
    {
        UpdateDamageNumbers();
    }

    private void UpdateDamageNumbers()
    {
        if (camera == null || subViewport?.GetCamera3D() == null) return;

        int damageTaken = (int)previousHealth - (int)currentHealth;
        if (damageTaken > 0)
        {
            var viewportPos = camera.UnprojectPosition(damageNumbersOrigin.GlobalTransform.Origin);
            damageNumbers.DisplayNumber(damageTaken, viewportPos, DamageType.Enemy);
        }
        previousHealth = currentHealth;
    }

    public void TakeDamage(float amount)
    {
        currentHealth -= amount;
    }

    // public void Interact()
    // {
    //     //dialogueManager.StartDialogue("dummy");
    //     OnExamine();
    // }

    // public List<ContextMenuAction> GetContextActions()
    // {
    //     return new List<ContextMenuAction>
    // {
    //     new ContextMenuAction("Examine", OnExamine),
    //     new ContextMenuAction("Use", OnUse)
    // };
    // }

    // private void OnExamine()
    // {
    //     GlobalTerminal.Print("It's a practice dummy.");
    //     //DebugLog("It's a practice dummy.");
    // }

    // private void OnUse()
    // {
    //     GlobalTerminal.Print("Using object...");
    // }

    // private void DebugLog(string message, string type = "normal")
    // {
    //     if (EnableDebugConsole && _debugConsole != null)
    //     {
    //         _debugConsole.PrintMessage($"{message}", type);
    //     }
    // }
}