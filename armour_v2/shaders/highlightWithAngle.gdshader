shader_type spatial;
render_mode unshaded, depth_draw_never;

uniform vec4 shine_color : source_color = vec4(1.0, 1.0, 1.0, 1.0);
uniform float cycle_interval : hint_range(0.5, 5.0) = 1.0;
uniform float shine_speed : hint_range(1.0, 5.0) = 3.0;
uniform float shine_width : hint_range(1.0, 100.0) = 3.0;
uniform float angle : hint_range(0.0, 360.0) = 45.0; // Angle in degrees

void fragment() {
    // Convert angle to radians
    float angle_rad = radians(angle);

    // Get vertex position
    vec3 vertex = (INV_VIEW_MATRIX * vec4(VERTEX, 1.0)).xyz;

    // Rotate the z component
    float rotated_z = vertex.z * cos(angle_rad) - vertex.x * sin(angle_rad);

    // Existing highlight calculation with rotated_z
    float width = shine_width * 0.001 * cycle_interval;
    float frequency = floor(sin(rotated_z * cycle_interval + TIME * shine_speed * cycle_interval) + width);
    ALBEDO = shine_color.rgb;
    ALPHA = clamp((1.0 - dot(NORMAL, VIEW)) * frequency * shine_color.a, 0.0, 1.0);
}
