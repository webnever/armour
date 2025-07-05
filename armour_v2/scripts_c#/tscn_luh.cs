using Godot;
using System;

public partial class tscn_luh : Node3D
{
	public override void _Ready()
	{
		GetNode<AnimationPlayer>("blockbench_export/AnimationPlayer").Play("idle");
	}

	public override void _Process(double delta)
	{
	}
}
