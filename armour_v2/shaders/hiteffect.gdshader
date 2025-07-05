shader_type spatial;

uniform int start_frame = 0;
uniform int current_frame = 0;
uniform float mix_ratio = 1.0; // Controls the strength of the mix between original and new color

const vec3 colors[6] = vec3[] (
    vec3(1.0, 0.0, 0.0), // Red
    vec3(0.5, 0.0, 0.0), // Dark red
    vec3(0.0, 0.0, 0.0), // Black
    vec3(0.0, 0.0, 1.0), // Blue
    vec3(0.0, 0.0, 0.5), // Dark blue
    vec3(0.0, 0.0, 0.0)  // Black
);

void fragment() {
    // Calculate the brightness based on the dot product of the ALBEDO and the luma coefficients
    float brightness = dot(ALBEDO, vec3(0.2126, 0.7152, 0.0722));

    // Calculate the starting frame based on brightness
    int offset = 0;
    if (brightness > 0.75) {
        offset = 2;
    } else if (brightness > 0.25) {
        offset = 1;
    }

    // Get the color, wrapping around at the end of the array
    int color_index = (start_frame + current_frame + offset) % 6; // Length of colors array is 6
    vec3 color = colors[color_index];

    // Blend the original color with the new color based on the mix ratio
    ALBEDO = mix(ALBEDO, color, mix_ratio);
}
