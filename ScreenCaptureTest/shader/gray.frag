uniform sampler2D tex;
varying vec2 uv;
varying vec4 diffuseColor;
void main(void)
{	
	vec4 texel = texture2D(tex, uv);
	float lum = (0.299 * texel.r) + (0.587 * texel.g) + (0.114 * texel.b);
//	gl_FragColor = vec4(
//		gl_Color[0]*lum,
//		gl_Color[1]*lum,
//		gl_Color[2]*lum,
//		gl_Color[3]);
//	gl_FragColor = vec4(texel.r, texel.g, texel.b, 1);
	gl_FragColor = vec4(lum, lum, lum, 1);
}
