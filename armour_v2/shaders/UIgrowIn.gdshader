shader_type canvas_item;

uniform float visibility_scale : hint_range(0.0, 1.0) = 0.0;
uniform vec2 element_size;
uniform vec2 global_position;

void fragment() {
    vec2 centered_uv = (FRAGCOORD.xy - global_position) / element_size;
    vec2 dist = abs(centered_uv - vec2(0.5, 0.5));

    float max_dist = max(dist.x, dist.y);

    // Get the original alpha value from the texture
    float original_alpha = texture(TEXTURE, UV).a;

    if (max_dist < visibility_scale) {
        COLOR.a = original_alpha;
    } else {
        COLOR.a = 0.0;
    }
}