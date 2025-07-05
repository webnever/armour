extends StaticBody3D

@onready var inventory = $/root/mainScene/Inventory
@onready var protoset = inventory.item_protoset
@export var hpOrMP: String = "hp"

func interact():
	if inventory.has_item_by_id(hpOrMP + "Potion"):
		var potionItem = inventory.get_item_by_id(hpOrMP + "Potion")
		potionItem.set_property("quantity", potionItem.get_property("quantity") + 1)
	else:
		var potionItem = inventory.create_and_add_item(hpOrMP + "Potion")
		potionItem.set_property("quantity", 1)
	DialogueManager.StartDialogue(hpOrMP + "PotionPickUp")
	get_parent().hide_and_hide_children()
