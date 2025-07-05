shader_type canvas_item;

// Uniform variables
uniform float wave_frequency : hint_range(0, 10) = 1.0;
uniform float wave_amplitude : hint_range(0, 100) = 10.0;
uniform float wave_speed : hint_range(0, 10) = 1.0;
uniform float alpha : hint_range(0, 1) = 1.0; // Uniform variable for alpha

void vertex() {
    // Wave effect on the y-coordinate of the vertex
    VERTEX.y += sin(TIME * wave_speed + VERTEX.x * wave_frequency) * wave_amplitude;
}

void fragment() {
    // Sample the texture
    COLOR = texture(TEXTURE, UV);
    COLOR.a *= alpha; // Apply the alpha uniform to the alpha component of COLOR
}
