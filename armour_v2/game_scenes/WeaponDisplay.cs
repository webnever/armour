using Godot;

public partial class WeaponDisplay : Node3D
{
    [Export] private Node3D sword;
    [Export] private Node3D handgun;
    [Export] private Node3D smg;  // Add SMG node

    private ShaderMaterial swordMaterial;
    private ShaderMaterial gunMaterial;
    private ShaderMaterial smgMaterial;  // Add SMG material

    private float selectedAlpha = 1.0f;
    private float unselectedAlpha = 0.5f;
    private float transitionDuration = 0.1f;
    private Tween currentTween;

    public override void _Ready()
    {
        // Get shader materials
        if (sword != null)
        {
            var meshInstance = sword.GetNode<MeshInstance3D>("mesh");
            if (meshInstance != null)
            {
                swordMaterial = meshInstance.GetActiveMaterial(0) as ShaderMaterial;
            }
        }

        if (handgun != null)
        {
            var meshInstance = handgun.GetNode<MeshInstance3D>("mesh");
            if (meshInstance != null)
            {
                gunMaterial = meshInstance.GetActiveMaterial(0) as ShaderMaterial;
            }
        }

        if (smg != null)
        {
            var meshInstance = smg.GetNode<MeshInstance3D>("mesh");
            if (meshInstance != null)
            {
                smgMaterial = meshInstance.GetActiveMaterial(0) as ShaderMaterial;
            }
        }

        // Set initial state (sword selected by default)
        UpdateWeaponVisibility(WeaponType.Sword);
    }

    public void UpdateWeaponVisibility(WeaponType selectedWeapon)
    {
        if (currentTween != null && currentTween.IsValid())
        {
            currentTween.Kill();
        }

        currentTween = CreateTween();

        // Update sword opacity
        if (swordMaterial != null)
        {
            float targetSwordAlpha = selectedWeapon == WeaponType.Sword ? selectedAlpha : unselectedAlpha;
            currentTween.TweenMethod(
                Callable.From((float v) => swordMaterial.SetShaderParameter("alpha", v)),
                swordMaterial.GetShaderParameter("alpha").AsDouble(),
                targetSwordAlpha,
                transitionDuration
            );
        }

        // Update gun opacity
        if (gunMaterial != null)
        {
            float targetGunAlpha = selectedWeapon == WeaponType.Gun ? selectedAlpha : unselectedAlpha;
            currentTween.TweenMethod(
                Callable.From((float v) => gunMaterial.SetShaderParameter("alpha", v)),
                gunMaterial.GetShaderParameter("alpha").AsDouble(),
                targetGunAlpha,
                transitionDuration
            );
        }

        // Update SMG opacity
        if (smgMaterial != null)
        {
            float targetSMGAlpha = selectedWeapon == WeaponType.SMG ? selectedAlpha : unselectedAlpha;
            currentTween.TweenMethod(
                Callable.From((float v) => smgMaterial.SetShaderParameter("alpha", v)),
                smgMaterial.GetShaderParameter("alpha").AsDouble(),
                targetSMGAlpha,
                transitionDuration
            );
        }

        currentTween.Play();
    }
}