shader_type canvas_item;

// Convert RGB to HSV
vec3 rgb_to_hsv(vec3 c) {
    vec4 K = vec4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    vec4 p = mix(vec4(c.bg, K.wz), vec4(c.gb, K.xy), step(c.b, c.g));
    vec4 q = mix(vec4(p.xyw, c.r), vec4(c.r, p.yzx), step(p.x, c.r));

    float d = q.x - min(q.w, q.y);
    float e = 1.0e-10;
    return vec3(abs(q.z + (q.w - q.y) / (6.0 * d + e)), d / (q.x + e), q.x);
}

void fragment() {
    vec4 tex_color = texture(TEXTURE, UV);
    
    // Convert to HSV
    vec3 hsv = rgb_to_hsv(tex_color.rgb);
    
    // Use the Value (brightness) component as alpha
    // V = 0 (black) becomes fully transparent
    // V = 1 (bright) becomes fully opaque
    float alpha = hsv.z;
    
    // Apply the alpha while preserving original color
    COLOR = vec4(tex_color.rgb, alpha * tex_color.a);
}