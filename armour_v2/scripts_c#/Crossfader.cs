using Godot;
using System;

public partial class Crossfader : ColorRect
{
    private AnimationPlayer animPlayer;

    public override void _Ready()
    {
        animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
    }

    public void Crossfade()
    {
        animPlayer.Play("fadeInAndOut");
    }

    public void FadeIn()
    {
        animPlayer.Play("fadeIn");
    }

    public void FadeOut()
    {
        animPlayer.Play("fadeOut");
    }
}
