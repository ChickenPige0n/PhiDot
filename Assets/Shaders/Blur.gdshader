shader_type canvas_item;
render_mode blend_mix;

uniform float screen_blur: hint_range(0, 10) = 2;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;
void fragment() {
    vec4 col = texture(SCREEN_TEXTURE, SCREEN_UV, screen_blur);
	vec2 ps = SCREEN_PIXEL_SIZE;
    COLOR = col;
}