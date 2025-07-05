shader_type spatial;
render_mode unshaded;

uniform sampler2D spritesheet_texture : source_color;
uniform int frames_horizontal : hint_range(1, 20) = 4;
uniform int frames_vertical : hint_range(1, 20) = 4;
uniform float animation_speed : hint_range(0.1, 10.0) = 1.0;
uniform bool auto_animate = true;
uniform int manual_frame : hint_range(0, 100) = 0;
uniform float black_threshold : hint_range(0.0, 1.0) = 0.1;
uniform float border_clamp : hint_range(0.0, 0.1) = 0.01;

varying vec2 uv;

void vertex() {
    uv = UV;
}

void fragment() {
    float frame_width = 1.0 / float(frames_horizontal);
    float frame_height = 1.0 / float(frames_vertical);
    
    int current_frame;
    if (auto_animate) {
        int total_frames = frames_horizontal * frames_vertical;
        current_frame = int(TIME * animation_speed) % total_frames;
    } else {
        current_frame = manual_frame;
    }
    
    int frame_x = current_frame % frames_horizontal;
    int frame_y = current_frame / frames_horizontal;
    
    // Clamp UV coordinates to avoid bleeding from neighboring frames
    vec2 clamped_uv = clamp(uv, border_clamp, 1.0 - border_clamp);
    
    vec2 frame_uv = vec2(
        clamped_uv.x * frame_width + float(frame_x) * frame_width,
        clamped_uv.y * frame_height + float(frame_y) * frame_height
    );
    
    vec4 tex_color = texture(spritesheet_texture, frame_uv);
    
    // Calculate brightness of the pixel
    float brightness = dot(tex_color.rgb, vec3(0.299, 0.587, 0.114));
    
    // Make black (and near-black) pixels transparent
    float alpha = tex_color.a;
    if (brightness < black_threshold) {
        alpha = 0.0;
    }
    
    ALBEDO = tex_color.rgb;
    ALPHA = alpha;
}