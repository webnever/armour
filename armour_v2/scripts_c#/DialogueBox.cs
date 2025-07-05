using Godot;
using System.Threading.Tasks;

public partial class DialogueBox : NinePatchRect
{
    private ShaderMaterial shaderMaterial;
    public bool animCompleted = true;
    private AnimationPlayer animPlayer;
    private bool isClosing = false;

    public override void _Ready()
    {
        shaderMaterial = Material as ShaderMaterial;
        UpdateShaderParameters();
        animPlayer = GetNode<AnimationPlayer>("AnimationPlayer");
        animPlayer.AnimationFinished += (animationName) => OnAnimationPlayerAnimationFinished(animationName);
    }

    private void UpdateShaderParameters()
    {
        shaderMaterial.SetShaderParameter("element_size", Size);
        shaderMaterial.SetShaderParameter("global_position", GlobalPosition);
    }

    public async Task RevealUIElement()
    {
        // Wait if we're currently closing
        while (isClosing)
        {
            await ToSignal(GetTree(), "process_frame");
        }
        
        UpdateShaderParameters();
        animCompleted = false;
        GetNode<AnimationPlayer>("AnimationPlayer").Play("reveal_animation");
        GlobalTerminal.TurnOff();
    }

    public async Task HideUIElement()
    {
        UpdateShaderParameters();
        animCompleted = false;
        isClosing = true;
        GetNode<AnimationPlayer>("AnimationPlayer").PlayBackwards("reveal_animation");
        
        // Wait for the animation to complete
        while (!animCompleted)
        {
            await ToSignal(GetNode<AnimationPlayer>("AnimationPlayer"), "animation_finished");
        }
        isClosing = false;
    }

    private void OnAnimationPlayerAnimationFinished(string animationName)
    {
        if (animationName == "reveal_animation")
        {
            animCompleted = true;
        }
    }

    public bool HasPoint(Vector2 point)
    {
        bool doesHavePoint = point.X >= GlobalPosition.X &&
            point.X <= GlobalPosition.X + Size.X &&
            point.Y >= GlobalPosition.Y &&
            point.Y <= GlobalPosition.Y + Size.Y;
        return doesHavePoint;
    }
}