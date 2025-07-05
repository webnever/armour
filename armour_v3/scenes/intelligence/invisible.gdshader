shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_opaque, depth_test_disabled, skip_vertex_transform;

void vertex() {
    // Don't render the vertex at all
    VERTEX = vec3(0.0);
}

void fragment() {
    // Complete transparency
    ALPHA = 0.0;
    discard;
}