using Godot;
using System;
using System.Collections.Generic; // Add this for IEnumerable
using System.Linq;

public partial class MenuManager : Control
{
	private const float JOYSTICK_DEADZONE = 0.5f;

	#region Offsets and References
	private readonly Vector2[] OFFSETS = {
		new(-30, 20),  // SELECT
        new(18, 26),   // INVENTORY
        new(30, 56),   // SAVE
        new(60, 100)   // LOAD
    };

	private ShaderMaterial _shaderMaterial;
	private Button[] _menuButtons;
	private NinePatchRect[] _bigBoxes;
	private AnimatedSprite2D _select;
	private AnimationPlayer _animPlayer;
	private Node _inventory;
	private GameSceneManager _gameStateManager;
	private PackedScene _itemSlotScene;
	private PackedScene _saveSlotScene;
	#endregion

	#region State
	private MenuState _currentState;
	public bool AnimFinished { get; private set; } = true;
	private bool _movedUp;
	private bool _movedDown;
	private string _currentSavePathSelected = "";
	private NinePatchRect _currentBigBox;

	private enum MenuState
	{
		None,
		MainMenu,
		BigBox,
		SaveSlot
	}
	#endregion

	public override void _Ready()
	{
		InitializeComponents();
		LoadScenes();
	}

	private void LoadScenes()
	{
		_itemSlotScene = GD.Load<PackedScene>("res://inventory/item_slot.tscn");
		_saveSlotScene = GD.Load<PackedScene>("res://ui/save_slot.tscn");
	}

	private void InitializeComponents()
	{
		// Node references
		_gameStateManager = GetNode<GameSceneManager>("/root/mainScene/GameSceneManager");
		_inventory = GetNode("/root/mainScene/Inventory");
		_animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
		_shaderMaterial = Material as ShaderMaterial;
		_select = GetNode<AnimatedSprite2D>("select");

		// UI setup
		InitializeButtons();
		InitializeBigBoxes();
		ConnectSignals();
		UpdateShaderParameters();
		_select.Play("default");
	}

	private void InitializeButtons()
	{
		string[] buttonNames = { "status", "items", "magic", "equip", "config", "save", "quit" };
		_menuButtons = buttonNames
			.Select(name => GetNode<Button>($"MenuSelectBox/MarginContainer/CenterContainer/VBoxContainer/{name}Button"))
			.ToArray();

		foreach (var button in _menuButtons)
		{
			button.Pressed += () => HandleButtonPress(button);
			button.GuiInput += (@event) => HandleGuiInput(button, @event);
		}
	}

	private void InitializeBigBoxes()
	{
		string[] boxNames = { "Status", "Inventory", "Magic", "Equip", "Config", "Save", "Quit" };
		_bigBoxes = boxNames.Select(name => GetNode<NinePatchRect>($"{name}Box")).ToArray();
	}

	private void ConnectSignals()
	{
		_animPlayer.AnimationStarted += (name) => AnimFinished = name == "reveal_animation" ? false : AnimFinished;
		_animPlayer.AnimationFinished += (name) => AnimFinished = name == "reveal_animation" ? true : AnimFinished;

		var loadOverwriteDelete = GetNode("LoadOverwriteDeleteBack/HBoxContainer");
		string[] buttonNames = { "Load", "Delete", "Back" };
		var actions = new Action[] { HandleLoad, HandleDelete, HandleBack };

		for (int i = 0; i < buttonNames.Length; i++)
		{
			var button = loadOverwriteDelete.GetNode<Button>(buttonNames[i]);
			var action = actions[i];
			button.Pressed += () => action();
			button.GuiInput += (@event) => HandleLoadOverwriteGuiInput(button, @event);
		}

		var newSaveButton = GetNode<Button>("SaveBox/DialogueBox/Button");
		newSaveButton.Pressed += HandleNewSave;
		newSaveButton.GuiInput += (@event) => HandleSaveDialogueGuiInput(newSaveButton, @event);
	}

	private void UpdateSelectorPosition(Control control, Vector2 offset)
	{
		_select.GlobalPosition = control.GlobalPosition + offset;
		if (_currentState == MenuState.SaveSlot)
		{
			_select.Reparent(control);
		}
	}

	private void HandleButtonGuiInput(Button button, InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton && mouseButton.ButtonIndex == MouseButton.Left && mouseButton.Pressed)
		{
			button.GrabFocus();
		}
	}

	private void HandleInventorySlotGuiInput(Button button, InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed)
		{
			button.GrabFocus();
			_select.GlobalPosition = button.GlobalPosition + OFFSETS[1];
		}
	}


	private void HandleSaveSlotGuiInput(Button button, InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed)
		{
			button.GrabFocus();
			_select.GlobalPosition = button.GlobalPosition + OFFSETS[2];
			_select.Reparent(button);  // For save slots, we want to reparent the selector
		}
	}


	private void HandleLoadOverwriteGuiInput(Button button, InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed)
		{
			button.GrabFocus();
			_select.GlobalPosition = button.GlobalPosition + OFFSETS[3];
		}
	}


	private void HandleSaveDialogueGuiInput(Button button, InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed)
		{
			button.GrabFocus();
			_select.GlobalPosition = button.GlobalPosition + OFFSETS[2];
		}
	}

	#region Input Handling
	public override void _Input(InputEvent @event)
	{
		if (@event.IsActionPressed("enter"))
		{
			if (_currentState == MenuState.None)
			{
				GetNode<PlayerController>("/root/mainScene/playerController").DisableInput();
				RevealUIElement();
				return;
			}
			else  // Add this else block to handle closing
			{
				GetNode<PlayerController>("/root/mainScene/playerController").EnableInput();
				HideUIElement();
				return;
			}
		}
		else if (@event.IsActionPressed("esc"))  // Changed from ui_cancel
		{
			if (_currentState != MenuState.None)
			{
				GetNode<PlayerController>("/root/mainScene/playerController").EnableInput();
				HideUIElement();
				return;
			}
		}

		if (_currentState == MenuState.None) return;

		if (@event is InputEventJoypadMotion joypadMotion && (int)joypadMotion.Axis == 1)
		{
			HandleJoystickMovement(joypadMotion.AxisValue);
		}
		else
		{
			HandleMenuInput(@event);
		}
	}

	private void HandleJoystickMovement(float value)
	{
		if (value < -JOYSTICK_DEADZONE && !_movedUp)
			ProcessDirectionalInput(true, false);
		else if (value > JOYSTICK_DEADZONE && !_movedDown)
			ProcessDirectionalInput(false, true);
		else if (Math.Abs(value) < JOYSTICK_DEADZONE)
			(_movedUp, _movedDown) = (false, false);
	}

	private void ProcessDirectionalInput(bool up, bool down)
	{
		var action = up ? "left_up" : "left_down";
		var inputEvent = new InputEventAction { Action = action };

		switch (_currentState)
		{
			case MenuState.MainMenu:
				HandleMenuSelectInput(inputEvent);
				break;
			case MenuState.BigBox:
				HandleBigBoxInput(inputEvent);
				break;
			case MenuState.SaveSlot:
				HandleSaveSlotInput(inputEvent);
				break;
		}

		(_movedUp, _movedDown) = (up, down);
	}

	private void HandleMenuInput(InputEvent @event)
	{
		switch (_currentState)
		{
			case MenuState.MainMenu:
				HandleMenuSelectInput(@event);
				break;
			case MenuState.BigBox:
				HandleBigBoxInput(@event);
				break;
			case MenuState.SaveSlot:
				HandleSaveSlotInput(@event);
				break;
		}
	}
	#endregion

	#region Navigation
	private void MoveFocus(Node[] items, int direction, Vector2 offset, bool reparentSelect = false)
	{
		if (items.Length == 0) return;

		var currentFocus = GetViewport().GuiGetFocusOwner();
		int index = Array.IndexOf(items, currentFocus);
		index = ((index + direction) % items.Length + items.Length) % items.Length;

		if (items[index] is Control control)
		{
			control.GrabFocus();
			if (reparentSelect)
				_select.Reparent(control);
			_select.GlobalPosition = control.GlobalPosition + offset;
		}
	}

	private void SwitchMenuState(MenuState newState, Node focusTarget = null)
	{
		_currentState = newState;
		if (focusTarget is Control control)
		{
			control.GrabFocus();
			_select.GlobalPosition = control.GlobalPosition + OFFSETS[0];
		}
	}
	#endregion

	#region UI Actions
	private async void HandleNewSave()
	{
		_gameStateManager.SaveGame();
		await ToSignal(_gameStateManager, "SaveCompleted");
		PopulateSaveSlots();
	}

	private void HandleLoad()
	{
		_gameStateManager.LoadGame($"user://saves/{_currentSavePathSelected}");
		_gameStateManager.ToggleGameMenu();
	}

	private void HandleDelete()
	{
		var basePath = $"user://saves/{_currentSavePathSelected[..^5]}";
		DirAccess.RemoveAbsolute($"{basePath}.tres");
		DirAccess.RemoveAbsolute($"{basePath}.png");
		PopulateSaveSlots();
		HandleBack();
	}

	private void HandleBack()
	{
		GetNode<AnimationPlayer>("LoadOverwriteDeleteBack/AnimationPlayer")
			.PlayBackwards("reveal_animation");
		SwitchMenuState(MenuState.BigBox);
		MoveFocusToBigBox();
	}

	private void HandleButtonPress(Button button)
	{
		int index = Array.IndexOf(_menuButtons, button);
		if (index >= 0 && index < _bigBoxes.Length)
		{
			if (index == 1) PopulateInventoryItemSlots();
			else if (index == 5) PopulateSaveSlots();
			OpenBigBox(_bigBoxes[index]);
			SwitchMenuState(MenuState.BigBox); // Add this line
			MoveFocusToBigBox();
		}
	}

	private void HandleGuiInput(Button button, InputEvent @event)
	{
		if (@event is InputEventMouseButton mouseButton &&
			mouseButton.ButtonIndex == MouseButton.Left &&
			mouseButton.Pressed)
		{
			GD.Print($"Button clicked: {button.Name}");
			button.GrabFocus();
			_select.GlobalPosition = button.GlobalPosition + OFFSETS[0];
			button.EmitSignal("pressed"); // Add this line to ensure the press is handled
		}
	}
	#endregion

	#region UI Management
	private void OpenBigBox(NinePatchRect boxToOpen)
	{
		GD.Print($"Opening box: {boxToOpen.Name}");
		foreach (var box in _bigBoxes)
		{
			box.Visible = box == boxToOpen;
			GD.Print($"Box {box.Name} visibility: {box.Visible}");
		}
		_currentBigBox = boxToOpen;
	}

	public void RevealUIElement()
	{
		SwitchMenuState(MenuState.MainMenu, _menuButtons[0]);
		OpenBigBox(_bigBoxes[0]);
		_animPlayer.Play("reveal_animation");
	}

	public async void HideUIElement()
	{
		if (GetNode<NinePatchRect>("LoadOverwriteDeleteBack").Material is ShaderMaterial material
			&& (float)material.GetShaderParameter("visibility_scale") > 0)
		{
			GetNode<AnimationPlayer>("LoadOverwriteDeleteBack/AnimationPlayer")
				.PlayBackwards("reveal_animation");
		}

		_animPlayer.PlayBackwards("reveal_animation");
		await ToSignal(_animPlayer, "animation_finished");
		SwitchMenuState(MenuState.None);
	}

	private void UpdateShaderParameters()
	{
		if (_shaderMaterial != null)
		{
			_shaderMaterial.SetShaderParameter("element_size", Size);
			_shaderMaterial.SetShaderParameter("global_position", GlobalPosition);
		}
	}
	#endregion

	#region Inventory Management
	private void PopulateInventoryItemSlots()
	{
		var container = GetNode<VBoxContainer>("InventoryBox/scroll/itemSlotContainer");
		container.GetChildren().ToList().ForEach(child => child.QueueFree());

		var inventoryItems = (Godot.Collections.Array)_inventory.Call("get_items");
		foreach (Node item in inventoryItems)
		{
			CreateInventorySlot(item, container);
		}
	}

	private void CreateInventorySlot(Node item, Node container)
	{
		var itemSlot = _itemSlotScene.Instantiate();

		// Setup item slot properties
		itemSlot.GetNode<TextureRect>("texture").Texture = GetItemProperty<Texture2D>(item, "texture");
		foreach (var property in new[] { "name", "quantity", "description" })
		{
			itemSlot.GetNode<Label>(property).Text = GetItemProperty<string>(item, property);
		}

		var button = itemSlot.GetNode<Button>("Button");
		button.Pressed += () => HandleItemSlotPress(item);
		button.GuiInput += (@event) => HandleInventorySlotGuiInput(button, @event);
		container.AddChild(itemSlot);
	}



	private T GetItemProperty<T>(Node item, string property)
	{
		var value = item.Call("get_property", property).ToString();
		if (typeof(T) == typeof(Texture2D))
			return (T)(object)GD.Load<Texture2D>(value);
		return (T)(object)value;
	}

	private async void HandleItemSlotPress(Node item)
	{
		PopulateInventoryItemSlots();
		await ToSignal(GetTree(), "process_frame");

		var items = (Godot.Collections.Array)_inventory.Call("get_items");
		SwitchMenuState(items.Count == 0 ? MenuState.MainMenu : MenuState.BigBox);
		if (_currentState == MenuState.BigBox)
			MoveFocusToBigBox();
	}
	#endregion

	#region Save System
	private void PopulateSaveSlots()
	{
		var container = GetNode<VBoxContainer>("SaveBox/scroll/saveSlotContainer");
		container.GetChildren()
				.Where(child => child.Name != "newSaveSlot")
				.ToList()
				.ForEach(child => child.QueueFree());

		using var dir = DirAccess.Open("user://saves/");
		if (dir == null) return;

		foreach (var fileName in GetSaveFiles(dir))
		{
			CreateSaveSlot(fileName, container);
		}
	}

	private IEnumerable<string> GetSaveFiles(DirAccess dir)
	{
		dir.ListDirBegin();
		string fileName;
		while (!string.IsNullOrEmpty(fileName = dir.GetNext()))
		{
			if (fileName.EndsWith(".tres"))
				yield return fileName;
		}
		dir.ListDirEnd();
	}

	private void CreateSaveSlot(string fileName, Node container)
	{
		var saveSlot = _saveSlotScene.Instantiate();
		var baseName = fileName[..^5];

		saveSlot.GetNode<Label>("Label").Text = baseName;
		if (Image.LoadFromFile($"user://saves/{baseName}.png") is Image image)
		{
			saveSlot.GetNode<TextureRect>("TextureRect").Texture = ImageTexture.CreateFromImage(image);
		}

		var button = saveSlot.GetNode<Button>("Button");
		button.Pressed += () => HandleSaveSlotPress(fileName);
		button.GuiInput += (@event) => HandleSaveSlotGuiInput(button, @event);
		container.AddChild(saveSlot);
	}

	private void HandleSaveSlotPress(string savePath)
	{
		_select.Reparent(this);
		_currentSavePathSelected = savePath;

		var animator = GetNode<AnimationPlayer>("LoadOverwriteDeleteBack/AnimationPlayer");
		animator.Play("reveal_animation");

		var loadButton = GetNode<Button>("LoadOverwriteDeleteBack/HBoxContainer/Load");
		SwitchMenuState(MenuState.SaveSlot, loadButton);
	}
	#endregion

	#region Menu Navigation

	private void HandleMenuSelectInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("left_up"))
			MoveFocus(_menuButtons, -1, OFFSETS[0]);
		else if (inputEvent.IsActionPressed("left_down"))
			MoveFocus(_menuButtons, 1, OFFSETS[0]);
		else if (inputEvent.IsActionPressed("left_left"))
			MoveFocusToBigBox();
	}

	private void HandleBigBoxInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("left_up") || inputEvent.IsActionPressed("left_down"))
		{
			var direction = inputEvent.IsActionPressed("left_up") ? -1 : 1;
			if (_currentBigBox == _bigBoxes[1])
				MoveFocusInContainer("InventoryBox/scroll/itemSlotContainer", direction, OFFSETS[1]);
			else if (_currentBigBox == _bigBoxes[5])
				MoveFocusInContainer("SaveBox/scroll/saveSlotContainer", direction, OFFSETS[2], true);
		}
		else if (inputEvent.IsActionPressed("left_right"))
			SwitchMenuState(MenuState.MainMenu);
	}

	private void HandleSaveSlotInput(InputEvent inputEvent)
	{
		if (inputEvent.IsActionPressed("left_left") || inputEvent.IsActionPressed("left_down"))
		{
			var direction = inputEvent.IsActionPressed("left_left") ? -1 : 1;
			MoveFocusInContainer("LoadOverwriteDeleteBack/HBoxContainer", direction, OFFSETS[3]);
		}
	}

	private void MoveFocusInContainer(string containerPath, int direction, Vector2 offset, bool reparentSelect = false)
	{
		var container = GetNode<Control>(containerPath);
		var children = container.GetChildren().ToArray();
		MoveFocus(children, direction, offset, reparentSelect);
	}

	private void MoveFocusToBigBox()
	{
		if (_currentBigBox == null) return;

		var containerPath = _currentBigBox == _bigBoxes[1]
			? "InventoryBox/scroll/itemSlotContainer"
			: "SaveBox/scroll/saveSlotContainer";

		var children = GetNode<Control>(containerPath).GetChildren();
		if (children.Count == 0) return;

		var target = _currentBigBox == _bigBoxes[5]
			? GetNode<Control>("SaveBox/DialogueBox")
			: children[0] as Control;

		SwitchMenuState(MenuState.BigBox, target);
	}
	#endregion
}