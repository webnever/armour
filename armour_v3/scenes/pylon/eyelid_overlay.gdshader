shader_type canvas_item;

uniform float eye_openness : hint_range(0.0, 1.0) = 1.0;
uniform float eyelid_softness : hint_range(0.0, 0.2) = 0.05;
uniform vec4 eyelid_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);

void fragment() {
    vec2 uv = UV;

    float distance_from_top = uv.y;
    float distance_from_bottom = 1.0 - uv.y;
    float min_distance = min(distance_from_top, distance_from_bottom);

    // visible_area: 0 (closed) -> 0.5 open; invert eye_openness logic here
    float visible_area = (1.0 - eye_openness) * 0.5;

    // softness scales *up* proportionally with eye_openness
    float proportional_softness = eyelid_softness * eye_openness;

    // Use smoothstep with proportional softness around visible_area
    float alpha = 1.0 - smoothstep(visible_area - proportional_softness, visible_area + proportional_softness, min_distance);

    COLOR = vec4(eyelid_color.rgb, eyelid_color.a * alpha);
}
