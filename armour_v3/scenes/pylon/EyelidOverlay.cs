using Godot;
using System;

public partial class EyelidOverlay : ColorRect
{
    [Export] public float SawtoothFrequency = 2.0f; // Cycles per second
    [Export] public float HoldDuration = 2.0f; // Hold time at full openness
    
    private ShaderMaterial shaderMaterial;
    private float sawtoothTime = 0f;
    private float holdTimer = 0f;
    private bool isHolding = false;

    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        shaderMaterial = (ShaderMaterial)Material;
        SetProcess(true);
    }

    public override void _Process(double delta)
    {
        if (isHolding)
        {
            // Hold at 1.0 for the specified duration
            holdTimer += (float)delta;
            if (holdTimer >= HoldDuration)
            {
                // Reset and start sawtooth again
                isHolding = false;
                holdTimer = 0f;
                sawtoothTime = 0f;
            }
            SetEyeOpenness(1.0f);
        }
        else
        {
            // Normal sawtooth behavior
            sawtoothTime += (float)(delta * SawtoothFrequency);
            float openness = sawtoothTime - (float)Math.Floor(sawtoothTime);
            
            // Check if we've reached 1.0 (or very close to it)
            if (openness >= 0.99f || sawtoothTime >= 1.0f / SawtoothFrequency)
            {
                isHolding = true;
                openness = 1.0f;
            }
            
            SetEyeOpenness(openness);
        }
    }

    private void SetEyeOpenness(float openness)
    {
        shaderMaterial?.SetShaderParameter("eye_openness", openness);
    }
}