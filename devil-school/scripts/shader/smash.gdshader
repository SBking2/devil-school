shader_type spatial;
render_mode unshaded, cull_disabled, depth_draw_never;

uniform sampler2D screen_texture : hint_screen_texture, repeat_enable, filter_linear_mipmap;

// 透明度纹理
uniform sampler2D mask_tex;
// UV方向扭曲
uniform sampler2D noise_tex;
uniform float strength_scale = 1;

void fragment() {
	vec2 offset = texture(noise_tex, UV).rg;
	offset = offset * strength_scale;
	ALBEDO = texture(screen_texture, SCREEN_UV + offset).rgb;
	
    float mask = texture(mask_tex, UV).r;
    ALPHA = mask;
}