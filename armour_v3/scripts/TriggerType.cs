using Godot;
using System;

// Defines the different types of triggers for quests and events
public enum TriggerType
{
    None,       // No specific trigger
    Enter,      // Triggered when entering a location
    Take,       // Triggered when taking an item
    Drop,       // Triggered when dropping an item
    Use,        // Triggered when using an item
    UseOn,      // Triggered when using an item on a target
    Talk,       // Triggered when talking to a character
    Kill,       // Triggered when defeating an enemy
    Solve,      // Triggered when solving a puzzle
    Examine,    // Triggered when examining an object
    TimeOfDay,  // Triggered at a specific time of day
    Custom      // Custom trigger defined by code
}