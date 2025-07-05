using Godot;

[GlobalClass]
public partial class NebulaAnimator : Node
{
    [ExportGroup("Animation Settings")]
    [Export] public float animationSpeed = 1.0f;
    [Export] public bool enableColorShift = true;
    [Export] public bool enableDensityPulse = true;
    [Export] public bool enableStarFlicker = true;
    [Export] public bool enableNoiseMovement = true;
    [Export] public bool enableScaleBreathing = true;
    
    [ExportGroup("Color Animation")]
    [Export] public Color baseColor = new Color(1.0f, 0.5f, 0.3f, 1.0f);
    [Export] public Color secondaryColor = new Color(0.3f, 0.7f, 1.0f, 1.0f);
    [Export] public float colorShiftIntensity = 0.3f;
    [Export] public float colorShiftSpeed = 0.5f;
    
    [ExportGroup("Density Animation")]
    [Export] public float baseDensityAccumulation = 0.014286f;
    [Export] public float densityPulseIntensity = 0.005f;
    [Export] public float densityPulseSpeed = 0.8f;
    
    [ExportGroup("Star Animation")]
    [Export] public float baseStarIntensity = 1.0f;
    [Export] public float starFlickerIntensity = 0.4f;
    [Export] public float starFlickerSpeed = 2.0f;
    
    [ExportGroup("Noise Movement")]
    [Export] public float baseLargeScaleOffset = 100.0f;
    [Export] public float noiseMovementSpeed = 0.2f;
    [Export] public float noiseMovementRange = 50.0f;
    
    [ExportGroup("Scale Breathing")]
    [Export] public float baseNebulaScale = 0.5f;
    [Export] public float scaleBreathingIntensity = 0.1f;
    [Export] public float scaleBreathingSpeed = 0.3f;
    
    private ShaderMaterial shaderMaterial;
    private float time = 0.0f;
    
    // Store original values
    private Color originalAlbedo;
    private float originalDensityAccumulation;
    private float originalStarIntensity;
    private float originalLargeScaleOffset;
    private float originalNebulaScale;
    
    public override void _Ready()
    {
        // Get the CSG parent node (since this script is attached as a child of CSGCylinder3D)
        var csgParent = GetParent<CsgShape3D>();
        
        if (csgParent == null)
        {
            GD.PrintErr("NebulaAnimator: Parent is not a CSG node!");
            return;
        }
        
        // Get the shader material directly from the CSG parent
        shaderMaterial = csgParent.MaterialOverride as ShaderMaterial;
        
        if (shaderMaterial == null)
        {
            GD.PrintErr("NebulaAnimator: CSG parent doesn't have a shader material assigned to MaterialOverride!");
            return;
        }
        
        // Store original values
        StoreOriginalValues();
    }
    
    private void StoreOriginalValues()
    {
        Vector4 albedoVec = shaderMaterial.GetShaderParameter("albedo").AsVector4();
        originalAlbedo = new Color(albedoVec.X, albedoVec.Y, albedoVec.Z, albedoVec.W);
        originalDensityAccumulation = shaderMaterial.GetShaderParameter("density_accumulation").AsSingle();
        originalStarIntensity = shaderMaterial.GetShaderParameter("star_intensity").AsSingle();
        originalLargeScaleOffset = shaderMaterial.GetShaderParameter("large_scale_offset").AsSingle();
        originalNebulaScale = shaderMaterial.GetShaderParameter("nebula_scale").AsSingle();
    }
    
    public override void _Process(double delta)
    {
        if (shaderMaterial == null) return;
        
        time += (float)delta * animationSpeed;
        
        // Animate color shifting
        if (enableColorShift)
        {
            AnimateColorShift();
        }
        
        // Animate density pulsing
        if (enableDensityPulse)
        {
            AnimateDensityPulse();
        }
        
        // Animate star flickering
        if (enableStarFlicker)
        {
            AnimateStarFlicker();
        }
        
        // Animate noise movement
        if (enableNoiseMovement)
        {
            AnimateNoiseMovement();
        }
        
        // Animate scale breathing
        if (enableScaleBreathing)
        {
            AnimateScaleBreathing();
        }
    }
    
    private void AnimateColorShift()
    {
        float colorLerp = (Mathf.Sin(time * colorShiftSpeed) + 1.0f) * 0.5f;
        Color currentColor = baseColor.Lerp(secondaryColor, colorLerp * colorShiftIntensity);
        shaderMaterial.SetShaderParameter("albedo", currentColor);
    }
    
    private void AnimateDensityPulse()
    {
        float densityOffset = Mathf.Sin(time * densityPulseSpeed) * densityPulseIntensity;
        float newDensity = baseDensityAccumulation + densityOffset;
        shaderMaterial.SetShaderParameter("density_accumulation", newDensity);
    }
    
    private void AnimateStarFlicker()
    {
        // Use multiple sine waves for more complex flickering
        float flicker1 = Mathf.Sin(time * starFlickerSpeed) * 0.5f;
        float flicker2 = Mathf.Sin(time * starFlickerSpeed * 1.7f) * 0.3f;
        float flicker3 = Mathf.Sin(time * starFlickerSpeed * 2.3f) * 0.2f;
        
        float totalFlicker = (flicker1 + flicker2 + flicker3) * starFlickerIntensity;
        float newIntensity = baseStarIntensity + totalFlicker;
        shaderMaterial.SetShaderParameter("star_intensity", Mathf.Max(0.1f, newIntensity));
    }
    
    private void AnimateNoiseMovement()
    {
        float movement = Mathf.Sin(time * noiseMovementSpeed) * noiseMovementRange;
        float newOffset = baseLargeScaleOffset + movement;
        shaderMaterial.SetShaderParameter("large_scale_offset", newOffset);
    }
    
    private void AnimateScaleBreathing()
    {
        float breathing = Mathf.Sin(time * scaleBreathingSpeed) * scaleBreathingIntensity;
        float newScale = baseNebulaScale + breathing;
        shaderMaterial.SetShaderParameter("nebula_scale", Mathf.Max(0.1f, newScale));
    }
    
    // Public methods to control animation
    public void SetAnimationSpeed(float speed)
    {
        animationSpeed = speed;
    }
    
    public void ResetToOriginalValues()
    {
        if (shaderMaterial == null) return;
        
        shaderMaterial.SetShaderParameter("albedo", originalAlbedo);
        shaderMaterial.SetShaderParameter("density_accumulation", originalDensityAccumulation);
        shaderMaterial.SetShaderParameter("star_intensity", originalStarIntensity);
        shaderMaterial.SetShaderParameter("large_scale_offset", originalLargeScaleOffset);
        shaderMaterial.SetShaderParameter("nebula_scale", originalNebulaScale);
    }
    
    public void PauseAnimation()
    {
        SetProcessMode(ProcessModeEnum.Disabled);
    }
    
    public void ResumeAnimation()
    {
        SetProcessMode(ProcessModeEnum.Inherit);
    }
}