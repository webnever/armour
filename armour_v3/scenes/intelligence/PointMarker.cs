using Godot;

public partial class PointMarker : Node3D
{
    private Label3D label;
    private MeshInstance3D marker;
    private MeshInstance3D pulseRing;
    private string pointLabel;
    private Color baseColor;
    private StandardMaterial3D markerMaterial;
    private StandardMaterial3D pulseMaterial;
    
    [Export] public Font CustomFont;
    
    public override void _Ready()
    {
        // Load custom font if not set in editor
        if (CustomFont == null)
        {
            CustomFont = GD.Load<Font>("res://fonts/ModernDOS9x16.ttf");
            if (CustomFont == null)
            {
                GD.Print("font not found!");
            }
        }
        
        CreateMarker();
        CreateLabel();
        CreatePulseRing();
    }
    
    private void CreateMarker()
    {
        marker = new MeshInstance3D();
        var sphereMesh = new SphereMesh();
        sphereMesh.Radius = 0.8f;
        sphereMesh.Height = 1.6f;
        marker.Mesh = sphereMesh;
        
        // Load chrome material
        var chromeMaterial = GD.Load<Material>("res://scenes/intelligence/chrome.tres");
        if (chromeMaterial != null)
        {
            marker.MaterialOverride = chromeMaterial;
        }
        else
        {
            GD.PrintErr("chrome.tres material not found!");
        }
        
        AddChild(marker);
    }
    
    private void CreatePulseRing()
    {
        pulseRing = new MeshInstance3D();
        var sphereMesh = new SphereMesh();
        sphereMesh.Radius = 1.5f;
        sphereMesh.Height = 3.0f;
        pulseRing.Mesh = sphereMesh;
        
        pulseMaterial = new StandardMaterial3D();
        pulseMaterial.Transparency = BaseMaterial3D.TransparencyEnum.Alpha;
        pulseMaterial.ShadingMode = BaseMaterial3D.ShadingModeEnum.Unshaded;
        pulseRing.MaterialOverride = pulseMaterial;
        
        AddChild(pulseRing);
    }
    
    private void CreateLabel()
    {
        label = new Label3D();
        label.Text = pointLabel ?? "Unknown";
        label.Position = new Vector3(0, 2.5f, 0);
        label.Billboard = BaseMaterial3D.BillboardModeEnum.Enabled;
        label.OutlineSize = 2;
        label.OutlineModulate = Colors.Black;
        
        // Set custom font and larger size
        if (CustomFont != null)
        {
            label.Font = CustomFont;
        }
        label.FontSize = 258; // Bigger text size
        
        AddChild(label);
    }
    
    public void SetLabel(string newLabel)
    {
        pointLabel = newLabel;
        if (label != null)
        {
            label.Text = newLabel;
            // Reapply font in case label was created before font was loaded
            if (CustomFont != null && label.Font != CustomFont)
            {
                label.Font = CustomFont;
                label.FontSize = 24;
            }
        }
    }
    
    public void SetColors(Color pointColor)
    {
        baseColor = pointColor;
        
        // Don't override chrome material for main marker
        // Chrome material should remain as-is
        
        if (label != null)
        {
            label.Modulate = pointColor;
        }
    }
    
    public void UpdatePulse(float time)
    {
        if (pulseRing != null && pulseMaterial != null)
        {
            var pulse = (Mathf.Sin(time * 2.0f) + 1.0f) * 0.5f;
            var alpha = pulse * 0.3f;
            
            pulseMaterial.AlbedoColor = new Color(baseColor.R, baseColor.G, baseColor.B, alpha);
            pulseMaterial.EmissionEnabled = true;
            pulseMaterial.Emission = baseColor * alpha;
            
            // Scale the pulse ring
            var scale = 1.0f + pulse * 0.5f;
            pulseRing.Scale = Vector3.One * scale;
        }
        
        // Don't modify chrome material - let it remain as-is
    }
    
    public Vector3 GetWorldPosition()
    {
        return GlobalPosition;
    }
    
    public string GetLabel()
    {
        return pointLabel;
    }
}