using Godot;
using System;

public partial class CameraPivot : Node3D
{
    [Export] private Vector3 offset;
    private Node3D target;
    private Viewport bgViewport;
    private Viewport fgViewport;
    private Camera3D bgCamera;
    private Camera3D fgCamera;
    private Camera3D cameraMain;

    public override void _Ready()
    {
        target = GetTree().GetFirstNodeInGroup("Player") as Node3D;
        
        cameraMain = GetNode<Camera3D>("cameraMain");

        bgCamera = GetNode<Camera3D>("cameraMain/bg_viewport_container/bg_viewport/bg_camera");
        fgCamera = GetNode<Camera3D>("cameraMain/fg_viewport_container/fg_viewport/fg_camera");

        bgViewport = GetNode<Viewport>("cameraMain/bg_viewport_container/bg_viewport");
        fgViewport = GetNode<Viewport>("cameraMain/fg_viewport_container/fg_viewport");
    }

    public override void _Process(double delta)
    {
        Position = target.GlobalTransform.Origin + offset;

        bgCamera.GlobalTransform = cameraMain.GlobalTransform;
        bgCamera.Size = cameraMain.Size;

        fgCamera.GlobalTransform = cameraMain.GlobalTransform;
        fgCamera.Size = cameraMain.Size;
	}
}
