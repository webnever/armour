shader_type spatial;

// Define a uniform for color, including alpha
uniform vec4 myColor : source_color = vec4(1.0, 0.0, 0.0, 1.0); // Default to red with full opacity

void fragment() {
    // Set the base color and ignore lighting
    ALBEDO = myColor.rgb;
    ALPHA = myColor.a;
}

void light() {
    // Ignore lighting by doing nothing in this function
}
