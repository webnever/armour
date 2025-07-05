using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

public class Player
{
    public int Health { get; set; } = 100;
    public int MaxHealth { get; set; } = 100;
    public int Satoshi { get; set; } = 0; // Renamed from Gold
    public int Level { get; set; } = 1;
    public int ExperiencePoints { get; set; } = 0;
    public int AttackPower { get; set; } = 10;
    public int Defense { get; set; } = 5;
    public Dictionary<string, int> Stats { get; set; } = new Dictionary<string, int>
    {
        { "Strength", 10 },
        { "Dexterity", 10 },
        { "Intelligence", 10 }
    };
    public List<Item> Inventory { get; set; } = new List<Item>();
    public Item EquippedWeapon { get; set; }
    public Item EquippedArmor { get; set; }
    
    public bool HasItem(string itemId)
    {
        return Inventory.Any(item => item.Id.Equals(itemId, StringComparison.OrdinalIgnoreCase) || 
                                    item.Name.Equals(itemId, StringComparison.OrdinalIgnoreCase));
    }
    
    public void GainExperience(int amount)
    {
        ExperiencePoints += amount;
        CheckForLevelUp();
    }
    
    private void CheckForLevelUp()
    {
        int expNeeded = Level * 100;
        if (ExperiencePoints >= expNeeded)
        {
            Level++;
            ExperiencePoints -= expNeeded;
            MaxHealth += 10;
            Health = MaxHealth;
            AttackPower += 2;
            Defense += 1;
        }
    }
    
    public void EquipItem(Item item)
    {
        if (item.ItemType == ItemType.Weapon)
        {
            if (EquippedWeapon != null)
            {
                Inventory.Add(EquippedWeapon);
                AttackPower -= EquippedWeapon.UseValue;
            }
            EquippedWeapon = item;
            Inventory.Remove(item);
            AttackPower += item.UseValue;
        }
        else if (item.ItemType == ItemType.Armor)
        {
            if (EquippedArmor != null)
            {
                Inventory.Add(EquippedArmor);
                Defense -= EquippedArmor.UseValue;
            }
            EquippedArmor = item;
            Inventory.Remove(item);
            Defense += item.UseValue;
        }
    }
    
    public string Unequip(ItemType itemType)
    {
        if (itemType == ItemType.Weapon && EquippedWeapon != null)
        {
            Inventory.Add(EquippedWeapon);
            AttackPower -= EquippedWeapon.UseValue;
            string weaponName = EquippedWeapon.Name;
            EquippedWeapon = null;
            return $"You unequip the {weaponName}.";
        }
        else if (itemType == ItemType.Armor && EquippedArmor != null)
        {
            Inventory.Add(EquippedArmor);
            Defense -= EquippedArmor.UseValue;
            string armorName = EquippedArmor.Name;
            EquippedArmor = null;
            return $"You unequip the {armorName}.";
        }
        
        return "Nothing to unequip.";
    }
}