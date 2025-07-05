shader_type canvas_item;

// Uniform variables
uniform float wave_frequency : hint_range(0, 10) = 1.0;
uniform float wave_amplitude : hint_range(0, 100) = 10.0;
uniform float wave_speed : hint_range(0, 10) = 1.0;
uniform float alpha : hint_range(0, 1) = 1.0;
uniform vec4 outline_color : source_color = vec4(0.0, 0.0, 0.0, 1.0);
uniform float outline_size : hint_range(0, 10) = 1.0;

void vertex() {
    VERTEX.y += sin(TIME * wave_speed + VERTEX.x * wave_frequency) * wave_amplitude;
}

void fragment() {
    vec4 color = texture(TEXTURE, UV);
    vec4 sum = vec4(0.0);

    // Sample the texture at multiple points to create the outline
    for (int x = -1; x <= 1; x++) {
        for (int y = -1; y <= 1; y++) {
            vec2 offset = vec2(float(x), float(y)) * outline_size * TEXTURE_PIXEL_SIZE;
            sum += texture(TEXTURE, UV + offset);
        }
    }

    // Determine if the current pixel is at the edge
    if (sum.a < 9.0 && color.a > 0.0) {
        COLOR = outline_color;
    } else {
        COLOR = color;
        COLOR.a *= alpha;
    }
}
