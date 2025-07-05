using Godot;
using System;

public partial class FPS : Label
{
	public override void _Process(double delta)
	{
		double fps = Engine.GetFramesPerSecond();
        Text = "FPS " + fps.ToString();
	}
}
