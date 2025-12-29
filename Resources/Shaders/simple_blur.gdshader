shader_type canvas_item;

uniform float lod: hint_range(0.0, 5) = 0.0;
uniform float darkness : hint_range(0.0, 10.0) = 0.8;
uniform sampler2D SCREEN_TEXTURE : hint_screen_texture, filter_linear_mipmap;

void fragment(){
	vec4 color = texture(SCREEN_TEXTURE, SCREEN_UV, lod);
	color.rgb *= darkness;
	COLOR = color;
}