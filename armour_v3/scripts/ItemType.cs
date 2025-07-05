using Godot;
using System;

/// <summary>
/// Enumeration of all possible item types in the game
/// </summary>
public enum ItemType
{
    Miscellaneous,
    Weapon,
    Armor,
    Healing,
    QuestItem,
    Crafting,
    Key,
    Container,
    Damage,
    Tool,  // Add this missing type
    
    // Additional types from items.json
    Biological,
    Cybernetic,
    Dimensional,
    Memory,
    Identification,
    Currency,
    Archaeological,
    Scientific,
    Consciousness,
    Pharmaceutical,
    Emotional,
    Mathematical,
    Artificial,
    Historical,
    Digital,
    Psychological,
    Medical,
    Authentication,
    Programming,
    Void,
    Narrative,
    Meta,
    Legal,
    Technology,
    Ammunition,
    Glitch,
    Temporal,
    Economic
}
