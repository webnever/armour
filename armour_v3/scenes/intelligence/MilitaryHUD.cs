using Godot;
using System;
using System.Collections.Generic;

public partial class MilitaryHUD : Control
{
    // UI Elements
    private Label coordinateLabel;
    private Label statusLabel;
    private Label pointCountLabel;
    private Label instructionsLabel;
    private Panel radarPanel;
    private Control crosshair;
    private VBoxContainer pointsList;
    
    // References
    private Camera3D camera;
    private CoordinateSystemController coordinateSystem;
    private MilitaryCameraController cameraController;
    
    // HUD Data
    private float updateTimer = 0.0f;
    private float updateInterval = 0.1f; // Update 10 times per second
    
    // Custom font
    [Export] public Font CustomFont;
    
    public override void _Ready()
    {
        SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        
        // Load custom font if not set in editor
        if (CustomFont == null)
        {
            CustomFont = GD.Load<Font>("res://fonts/ModernDOS9x16.ttf"); // Replace with your font path
            if (CustomFont == null)
            {
                GD.Print("Custom font not found, using default");
            }
        }
        
        // Get references
        camera = GetNode<Camera3D>("../../CameraController/Camera3D");
        coordinateSystem = GetNode<CoordinateSystemController>("../../CoordinateSystem");
        cameraController = GetNode<MilitaryCameraController>("../../CameraController");
        
        CreateHUD();
    }
    
    public override void _Process(double delta)
    {
        updateTimer += (float)delta;
        
        if (updateTimer >= updateInterval)
        {
            UpdateHUD();
            updateTimer = 0.0f;
        }
    }
    
    private void CreateHUD()
    {
        // Set military green theme
        var bgColor = new Color(0, 0.2f, 0, 0.8f);
        var textColor = new Color(0, 1, 0, 1);
        var accentColor = new Color(0, 1, 0.5f, 1);
        
        // Main HUD background
        var background = new ColorRect();
        background.Color = new Color(0, 0, 0, 0.3f);
        background.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        AddChild(background);
        
        // Top status bar
        CreateStatusBar(textColor, bgColor);
        
        // Crosshair
        CreateCrosshair(accentColor);
        
        // Coordinate display
        CreateCoordinateDisplay(textColor, bgColor);
        
        // Points list
        CreatePointsList(textColor, bgColor);
        
        // Instructions
        CreateInstructions(textColor, bgColor);
        
        // Radar panel
        CreateRadarPanel(bgColor, accentColor);
        
        // Grid overlay
        CreateGridOverlay(textColor);
    }
    
    private void CreateStatusBar(Color textColor, Color bgColor)
    {
        var statusPanel = new Panel();
        statusPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopWide);
        statusPanel.Size = new Vector2I(0, 40);
        
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = bgColor;
        styleBox.BorderColor = textColor;
        styleBox.BorderWidthTop = 2;
        styleBox.BorderWidthBottom = 2;
        statusPanel.AddThemeStyleboxOverride("panel", styleBox);
        
        AddChild(statusPanel);
        
        statusLabel = new Label();
        statusLabel.Text = "TACTICAL DISPLAY ACTIVE";
        statusLabel.AddThemeColorOverride("font_color", textColor);
        if (CustomFont != null)
            statusLabel.AddThemeFontOverride("font", CustomFont);
        statusLabel.Position = new Vector2I(10, 10);
        statusPanel.AddChild(statusLabel);
        
        var timeLabel = new Label();
        timeLabel.Text = DateTime.Now.ToString("HH:mm:ss");
        timeLabel.AddThemeColorOverride("font_color", textColor);
        if (CustomFont != null)
            timeLabel.AddThemeFontOverride("font", CustomFont);
        timeLabel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        timeLabel.Position = new Vector2I(-100, 10);
        statusPanel.AddChild(timeLabel);
    }
    
    private void CreateCrosshair(Color color)
    {
        crosshair = new Control();
        crosshair.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.Center);
        crosshair.Size = new Vector2I(40, 40);
        
        // Horizontal line
        var hLine = new ColorRect();
        hLine.Color = color;
        hLine.Position = new Vector2I(-20, -1);
        hLine.Size = new Vector2I(40, 2);
        crosshair.AddChild(hLine);
        
        // Vertical line
        var vLine = new ColorRect();
        vLine.Color = color;
        vLine.Position = new Vector2I(-1, -20);
        vLine.Size = new Vector2I(2, 40);
        crosshair.AddChild(vLine);
        
        // Center dot
        var centerDot = new ColorRect();
        centerDot.Color = color;
        centerDot.Position = new Vector2I(-2, -2);
        centerDot.Size = new Vector2I(4, 4);
        crosshair.AddChild(centerDot);
        
        AddChild(crosshair);
    }
    
    private void CreateCoordinateDisplay(Color textColor, Color bgColor)
    {
        var coordPanel = new Panel();
        coordPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopLeft);
        coordPanel.Position = new Vector2I(10, 50);
        coordPanel.Size = new Vector2I(200, 100);
        
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = bgColor;
        styleBox.BorderColor = textColor;
        styleBox.BorderWidthLeft = 2;
        styleBox.BorderWidthRight = 2;
        styleBox.BorderWidthTop = 2;
        styleBox.BorderWidthBottom = 2;
        coordPanel.AddThemeStyleboxOverride("panel", styleBox);
        
        AddChild(coordPanel);
        
        coordinateLabel = new Label();
        coordinateLabel.Text = "COORDINATES:\nX: 0.00\nY: 0.00\nZ: 0.00";
        coordinateLabel.AddThemeColorOverride("font_color", textColor);
        if (CustomFont != null)
            coordinateLabel.AddThemeFontOverride("font", CustomFont);
        coordinateLabel.Position = new Vector2I(10, 10);
        coordPanel.AddChild(coordinateLabel);
    }
    
    private void CreatePointsList(Color textColor, Color bgColor)
    {
        var pointsPanel = new Panel();
        pointsPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.TopRight);
        pointsPanel.Position = new Vector2I(-220, 50);
        pointsPanel.Size = new Vector2I(210, 300);
        
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = bgColor;
        styleBox.BorderColor = textColor;
        styleBox.BorderWidthLeft = 2;
        styleBox.BorderWidthRight = 2;
        styleBox.BorderWidthTop = 2;
        styleBox.BorderWidthBottom = 2;
        pointsPanel.AddThemeStyleboxOverride("panel", styleBox);
        
        AddChild(pointsPanel);
        
        var title = new Label();
        title.Text = "TACTICAL POINTS";
        title.AddThemeColorOverride("font_color", textColor);
        if (CustomFont != null)
            title.AddThemeFontOverride("font", CustomFont);
        title.Position = new Vector2I(10, 10);
        pointsPanel.AddChild(title);
        
        var scrollContainer = new ScrollContainer();
        scrollContainer.Position = new Vector2I(10, 30);
        scrollContainer.Size = new Vector2I(190, 260);
        pointsPanel.AddChild(scrollContainer);
        
        pointsList = new VBoxContainer();
        scrollContainer.AddChild(pointsList);
        
        pointCountLabel = new Label();
        pointCountLabel.Text = "POINTS: 0";
        pointCountLabel.AddThemeColorOverride("font_color", textColor);
        if (CustomFont != null)
            pointCountLabel.AddThemeFontOverride("font", CustomFont);
        pointCountLabel.Position = new Vector2I(10, 275);
        pointsPanel.AddChild(pointCountLabel);
    }
    
    private void CreateInstructions(Color textColor, Color bgColor)
    {
        var instructPanel = new Panel();
        instructPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomLeft);
        instructPanel.Position = new Vector2I(10, -150);
        instructPanel.Size = new Vector2I(300, 140);
        
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = bgColor;
        styleBox.BorderColor = textColor;
        styleBox.BorderWidthLeft = 2;
        styleBox.BorderWidthRight = 2;
        styleBox.BorderWidthTop = 2;
        styleBox.BorderWidthBottom = 2;
        instructPanel.AddThemeStyleboxOverride("panel", styleBox);
        
        AddChild(instructPanel);
        
        instructionsLabel = new Label();
        instructionsLabel.Text = "CONTROLS:\n" +
                                "LEFT CLICK + DRAG - Manual camera control\n" +
                                "MOUSE WHEEL - Zoom in/out\n" +
                                "SPACE - Toggle auto-orbit";
        instructionsLabel.AddThemeColorOverride("font_color", textColor);
        if (CustomFont != null)
            instructionsLabel.AddThemeFontOverride("font", CustomFont);
        instructionsLabel.Position = new Vector2I(10, 10);
        instructPanel.AddChild(instructionsLabel);
    }
    
    private void CreateRadarPanel(Color bgColor, Color accentColor)
    {
        radarPanel = new Panel();
        radarPanel.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight);
        radarPanel.Position = new Vector2I(-160, -160);
        radarPanel.Size = new Vector2I(150, 150);
        
        var styleBox = new StyleBoxFlat();
        styleBox.BgColor = bgColor;
        styleBox.BorderColor = accentColor;
        styleBox.BorderWidthLeft = 2;
        styleBox.BorderWidthRight = 2;
        styleBox.BorderWidthTop = 2;
        styleBox.BorderWidthBottom = 2;
        radarPanel.AddThemeStyleboxOverride("panel", styleBox);
        
        AddChild(radarPanel);
        
        // Radar title
        var radarTitle = new Label();
        radarTitle.Text = "RADAR";
        radarTitle.AddThemeColorOverride("font_color", accentColor);
        if (CustomFont != null)
            radarTitle.AddThemeFontOverride("font", CustomFont);
        radarTitle.Position = new Vector2I(55, 5);
        radarPanel.AddChild(radarTitle);
    }
    
    private void CreateGridOverlay(Color color)
    {
        // Create corner brackets
        CreateCornerBracket(Control.LayoutPreset.TopLeft, color);
        CreateCornerBracket(Control.LayoutPreset.TopRight, color);
        CreateCornerBracket(Control.LayoutPreset.BottomLeft, color);
        CreateCornerBracket(Control.LayoutPreset.BottomRight, color);
    }
    
    private void CreateCornerBracket(Control.LayoutPreset preset, Color color)
    {
        var bracket = new Control();
        bracket.SetAnchorsAndOffsetsPreset(preset);
        
        var size = 30;
        var thickness = 2;
        
        // Horizontal line
        var hLine = new ColorRect();
        hLine.Color = color;
        hLine.Size = new Vector2I(size, thickness);
        
        // Vertical line  
        var vLine = new ColorRect();
        vLine.Color = color;
        vLine.Size = new Vector2I(thickness, size);
        
        switch (preset)
        {
            case Control.LayoutPreset.TopLeft:
                hLine.Position = new Vector2I(0, 0);
                vLine.Position = new Vector2I(0, 0);
                break;
            case Control.LayoutPreset.TopRight:
                hLine.Position = new Vector2I(-size, 0);
                vLine.Position = new Vector2I(-thickness, 0);
                break;
            case Control.LayoutPreset.BottomLeft:
                hLine.Position = new Vector2I(0, -thickness);
                vLine.Position = new Vector2I(0, -size);
                break;
            case Control.LayoutPreset.BottomRight:
                hLine.Position = new Vector2I(-size, -thickness);
                vLine.Position = new Vector2I(-thickness, -size);
                break;
        }
        
        bracket.AddChild(hLine);
        bracket.AddChild(vLine);
        AddChild(bracket);
    }
    
    private void UpdateHUD()
    {
        if (camera == null || coordinateSystem == null) return;
        
        // Update camera coordinates
        var pos = camera.GlobalPosition;
        coordinateLabel.Text = $"CAMERA POS:\nX: {pos.X:F2}\nY: {pos.Y:F2}\nZ: {pos.Z:F2}";
        
        // Update points list
        UpdatePointsList();
        
        // Update radar
        UpdateRadar();
        
        // Update status
        var points = coordinateSystem.GetActivePoints();
        statusLabel.Text = $"TACTICAL DISPLAY ACTIVE - {points.Count} CONTACTS";
    }
    
    private void UpdatePointsList()
    {
        var points = coordinateSystem.GetActivePoints();
        
        // Clear existing point labels
        foreach (Node child in pointsList.GetChildren())
        {
            child.QueueFree();
        }
        
        // Add current points
        for (int i = 0; i < points.Count; i++)
        {
            var point = points[i];
            var pos = point.GetWorldPosition();
            var distance = camera.GlobalPosition.DistanceTo(pos);
            
            var pointLabel = new Label();
            pointLabel.Text = $"{i + 1:D2}: {point.GetLabel()}\n    Range: {distance:F1}m";
            pointLabel.AddThemeColorOverride("font_color", new Color(0, 1, 0, 1));
            if (CustomFont != null)
                pointLabel.AddThemeFontOverride("font", CustomFont);
            
            // Highlight if close
            if (distance < 30.0f)
            {
                pointLabel.AddThemeColorOverride("font_color", new Color(1, 1, 0, 1));
            }
            
            pointsList.AddChild(pointLabel);
        }
        
        pointCountLabel.Text = $"CONTACTS: {points.Count}";
    }
    
    private void UpdateRadar()
    {
        if (radarPanel == null || coordinateSystem == null) return;
        
        // Clear existing radar blips
        foreach (Node child in radarPanel.GetChildren())
        {
            if (child.Name.ToString().StartsWith("RadarBlip"))
            {
                child.QueueFree();
            }
        }
        
        var points = coordinateSystem.GetActivePoints();
        var radarCenter = new Vector2I(75, 75); // Center of 150x150 radar
        var radarRadius = 65.0f;
        var maxRange = 100.0f; // Maximum detection range
        
        foreach (var point in points)
        {
            var pointPos = point.GetWorldPosition();
            var cameraPos = camera.GlobalPosition;
            var relativePos = pointPos - cameraPos;
            
            // Convert 3D position to 2D radar coordinates
            var distance = relativePos.Length();
            if (distance > maxRange) continue;
            
            var normalizedDistance = distance / maxRange;
            var angle = Mathf.Atan2(relativePos.Z, relativePos.X);
            
            var radarX = radarCenter.X + (int)(normalizedDistance * radarRadius * Mathf.Cos(angle));
            var radarY = radarCenter.Y + (int)(normalizedDistance * radarRadius * Mathf.Sin(angle));
            
            // Create radar blip
            var blip = new ColorRect();
            blip.Name = "RadarBlip" + point.GetLabel();
            blip.Color = new Color(0, 1, 0, 0.8f);
            blip.Size = new Vector2I(4, 4);
            blip.Position = new Vector2I(radarX - 2, radarY - 2);
            
            radarPanel.AddChild(blip);
        }
        
        // Add radar sweep line (rotating)
        var sweepAngle = Time.GetUnixTimeFromSystem() * 2.0; // Rotate every 2 seconds
        var sweepEndX = radarCenter.X + (int)(radarRadius * Mathf.Cos(sweepAngle));
        var sweepEndY = radarCenter.Y + (int)(radarRadius * Mathf.Sin(sweepAngle));
        
        var sweepLine = new Control();
        sweepLine.Name = "RadarBlipSweepLine";
        sweepLine.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.FullRect);
        
        // This would need custom drawing - simplified for now
        radarPanel.AddChild(sweepLine);
    }
}