using Godot;
using System;
using System.Collections.Generic;
using System.Linq;

// Manages inventory operations
public class InventorySystem
{
    private Player _player;
    private float _maxCarryWeight = 50.0f; // Default max carry weight

    public InventorySystem(Player player)
    {
        _player = player;
    }

    public float GetCurrentWeight()
    {
        float totalWeight = 0;

        foreach (var item in _player.Inventory)
        {
            totalWeight += item.Weight;
        }

        return totalWeight;
    }

    public bool CanAddItem(Item item)
    {
        float currentWeight = _player.Inventory.Sum(i => i.Weight);
        return currentWeight + item.Weight <= _maxCarryWeight;
    }

    public string GetInventoryDescription()
    {
        if (_player.Inventory.Count == 0)
        {
            return "Your inventory is empty.";
        }

        string description = $"Inventory ({GetCurrentWeight():F1}/{_maxCarryWeight:F1} weight):";

        // Group items by category
        var groupedItems = _player.Inventory
            .GroupBy(i => i.Category ?? "Miscellaneous")
            .OrderBy(g => g.Key);

        foreach (var group in groupedItems)
        {
            description += $"\n\n{group.Key}:";

            foreach (var item in group.OrderBy(i => i.Name))
            {
                description += $"\n- {item.Name}";

                // Show weight for heavier items
                if (item.Weight >= 1.0f)
                {
                    description += $" ({item.Weight:F1})";
                }

                // Show equipped status
                if (item == _player.EquippedWeapon)
                {
                    description += " (Equipped Weapon)";
                }
                else if (item == _player.EquippedArmor)
                {
                    description += " (Equipped Armor)";
                }
            }
        }

        return description;
    }

    public string TransferItem(Item item, Container container, bool toContainer)
    {
        if (toContainer)
        {
            // Transfer from player to container
            if (!_player.Inventory.Contains(item))
            {
                return $"You don't have the {item.Name} in your inventory.";
            }

            _player.Inventory.Remove(item);
            container.Items.Add(item);

            return $"You put the {item.Name} in the {container.Name}.";
        }
        else
        {
            // Transfer from container to player
            if (!container.Items.Contains(item))
            {
                return $"The {item.Name} is not in the {container.Name}.";
            }

            // Check weight limit
            if (!CanAddItem(item))
            {
                return $"The {item.Name} is too heavy to carry with everything else you have.";
            }

            container.Items.Remove(item);
            _player.Inventory.Add(item);

            return $"You take the {item.Name} from the {container.Name}.";
        }
    }

    public string SortInventory()
    {
        // Sort inventory by category then name
        _player.Inventory = _player.Inventory
            .OrderBy(i => i.Category ?? "Miscellaneous")
            .ThenBy(i => i.Name)
            .ToList();

        return "Inventory sorted.";
    }

    public List<Item> GetItemsByCategory(string category)
    {
        return _player.Inventory
            .Where(i => i.Category != null && i.Category.Equals(category, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public string GetItemDetailsDescription(Item item)
    {
        string description = $"{item.Name}\n";
        description += $"------------------------------\n";
        description += $"{item.Description}\n\n";

        description += $"Weight: {item.Weight:F1}\n";
        description += $"Category: {item.Category ?? "Miscellaneous"}\n";

        switch (item.ItemType)
        {
            case ItemType.Weapon:
                description += $"Type: Weapon\n";
                description += $"Attack Power: +{item.UseValue}\n";
                break;

            case ItemType.Armor:
                description += $"Type: Armor\n";
                description += $"Defense: +{item.UseValue}\n";
                break;

            case ItemType.Healing:
                description += $"Type: Healing Item\n";
                description += $"Healing Value: {item.UseValue} HP\n";
                break;

            case ItemType.QuestItem:
                description += $"Type: Quest Item\n";
                break;

            case ItemType.Crafting:
                description += $"Type: Crafting Material\n";
                break;

            case ItemType.Key:
                description += $"Type: Key\n";
                break;
        }

        // Show if equipped
        if (item == _player.EquippedWeapon)
        {
            description += "Status: Currently equipped as weapon.\n";
        }
        else if (item == _player.EquippedArmor)
        {
            description += "Status: Currently equipped as armor.\n";
        }

        return description;
    }

    public void SetMaxCarryWeight(float weight)
    {
        _maxCarryWeight = weight;
    }

    public float GetMaxCarryWeight()
    {
        return _maxCarryWeight;
    }

    public void AddItemToInventory(Item item)
    {
        if (CanAddItem(item))
        {
            _player.Inventory.Add(item);
        }
    }

    public bool RemoveItemFromInventory(string itemId)
    {
        Item item = _player.Inventory.FirstOrDefault(i => i.Id == itemId);

        if (item != null)
        {
            // Unequip if needed
            if (item == _player.EquippedWeapon)
            {
                _player.Unequip(ItemType.Weapon);
            }
            else if (item == _player.EquippedArmor)
            {
                _player.Unequip(ItemType.Armor);
            }

            _player.Inventory.Remove(item);
            return true;
        }

        return false;
    }
}