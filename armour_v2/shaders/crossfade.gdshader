shader_type canvas_item;

uniform float alpha : hint_range(0.0, 1.0) = 1.0; // Alpha value for fade

void fragment() {
    COLOR = vec4(COLOR.rgb, alpha);
}
