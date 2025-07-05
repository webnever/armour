using Godot;
using System;

public partial class CandleLight : OmniLight3D
{
    // Declare member variables here
    private float baseEnergy = 0.1f; // Base energy of the light
    private float baseRange = 0.25f;
    private float energyVariation = 0.05f; // How much the energy can vary
    private Color baseColor = new Color(1, 0.612f, 0.255f); // Base color of the light (warm yellow/orange)
    private float hueVariation = 0.025f; // How much the hue can vary
    private float valueVariation = 0.1f; // How much the value can vary
    private float rangeVariation = 0.05f;
    private float flickerSpeed = 0.1f;

    private double timePassed = 0.0;

    public override void _Process(double delta)
    {
        timePassed += delta;
        if (timePassed >= flickerSpeed)
        {
            timePassed = 0;

            // Randomly vary the energy
            LightEnergy = baseEnergy + (float)GD.RandRange(-energyVariation, energyVariation);

            // Convert RGB to HSV for hue and value variation
            float h, s, v;
            baseColor.ToHsv(out h, out s, out v);

            // Randomly vary the hue and value
            h = Mathf.Clamp(h + (float)GD.RandRange(-hueVariation, hueVariation), 0, 1);
            v = Mathf.Clamp(v + (float)GD.RandRange(-valueVariation, valueVariation), 0, 1);

            // Convert back to RGB and apply to the light color
            LightColor = Color.FromHsv(h, s, v);

            float rangeFlicker = baseRange;
            rangeFlicker = Mathf.Clamp(rangeFlicker + (float)GD.RandRange(-rangeVariation, rangeVariation), baseRange - rangeVariation, baseRange + rangeVariation);

            OmniRange = rangeFlicker;
        }
    }
}
