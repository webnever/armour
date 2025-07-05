shader_type spatial;
render_mode unshaded, specular_schlick_ggx;

uniform vec4 albedo_color = vec4(0.0, 0.0, 0.0, 1.0); // Black color
uniform float roughness : hint_range(0, 1) = 0.1; // Low roughness for shininess
uniform float metallic : hint_range(0, 1) = 1.0; // High metallic for metal-like reflections
uniform float specular : hint_range(0, 1) = 1.0; // High specular for strong highlights

void fragment() {
    ALBEDO = albedo_color.rgb;
    ROUGHNESS = roughness;
    METALLIC = metallic;
    SPECULAR = specular;
}
