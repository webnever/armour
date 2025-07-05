using Godot;
using System;

public partial class DialogueBoxPopOnReady : NinePatchRect
{
    private ShaderMaterial shaderMaterial;

    public override void _Ready()
    {
        shaderMaterial = Material as ShaderMaterial;
        UpdateShaderParameters();
        RevealUIElement();
    }

    public void UpdateShaderParameters()
    {
        shaderMaterial.SetShaderParameter("element_size", Size);
        shaderMaterial.SetShaderParameter("global_position", GlobalPosition);
    }

    public void RevealUIElement()
    {
        GetNode<AnimationPlayer>("AnimationPlayer").Play("reveal_animation");
    }

    public void HideUIElement()
    {
        GetNode<AnimationPlayer>("AnimationPlayer").PlayBackwards("reveal_animation");
    }
}
