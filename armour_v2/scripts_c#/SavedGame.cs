using Godot;
using System;
using System.Collections.Generic;

public partial class SavedGame : Resource
{
    public string time;

    public Image screenshot;

    public Vector3 playerPosition;

    public Vector3 playerRotation;

    public Godot.Collections.Dictionary inventory = new Godot.Collections.Dictionary();

    public Godot.Collections.Dictionary levelStates = new Godot.Collections.Dictionary();

    public string currentLevel = "";
}
