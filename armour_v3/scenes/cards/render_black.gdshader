shader_type spatial;

void fragment() {
    ALBEDO = vec3(1.0, 1.0, 1.0);  // Set the color to black
    EMISSION = vec3(0.0, 0.0, 0.0);  // Ensure there's no emission
    SPECULAR = 0.0;  // Disable specular reflections
    METALLIC = 0.0;  // Set material as non-metallic
    ROUGHNESS = 1.0;  // Maximum roughness
    ALPHA = 1.0;  // Fully opaque
}

//void light() {
//    DIFFUSE_LIGHT = vec3(0.0, 0.0, 0.0);
//    SPECULAR_LIGHT = vec3(0.0, 0.0, 0.0);
//}