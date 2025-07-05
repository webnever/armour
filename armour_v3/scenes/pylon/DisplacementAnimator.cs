using Godot;
using System;

public partial class DisplacementAnimator : Node
{
	[Export] public ShaderMaterial TargetMaterial { get; set; }
	[Export] public float LoopDuration { get; set; } = 2.0f;

	private float _time = 0.0f;

	public override void _Ready()
	{
		if (TargetMaterial == null)
		{
			GD.PrintErr("DisplacementAnimator: No TargetMaterial assigned.");
			return;
		}
	}

	public override void _Process(double delta)
	{
		if (TargetMaterial == null)
			return;

		_time += (float)delta;

		// Generate sawtooth value: ranges from 0 to 1, then resets
float normalizedTime = (_time % LoopDuration) / LoopDuration;
float baseValue = normalizedTime; // 0→1 sawtooth

TargetMaterial.SetShaderParameter("displacement_strength", baseValue);
	}

	public void ResetLoop()
	{
		_time = 0.0f;
	}

	public float GetLoopProgress()
	{
		return (_time % LoopDuration) / LoopDuration;
	}

	public void SetLoopProgress(float progress)
	{
		progress = Mathf.Clamp(progress, 0.0f, 1.0f);
		_time = progress * LoopDuration;
	}

	public float GetCurrentDisplacement()
	{
		float normalizedTime = (_time % LoopDuration) / LoopDuration;
		return -1.0f + 2.0f * normalizedTime;
	}
}
