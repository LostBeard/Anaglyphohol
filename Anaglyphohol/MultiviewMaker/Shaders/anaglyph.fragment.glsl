// Multiview to many by Todd Tanner

#include<multiview.fragment.glsl>

uniform float agdata[21];

vec4 anaglyphMix(vec4 l, vec4 r){
	//
	float brightness	= agdata[0];
	float contrast		= agdata[1];
	float gamma			= agdata[2];
	// red channel
	float rlr = agdata[3];
	float rlg = agdata[4];
	float rlb = agdata[5];
	float rrr = agdata[6];
	float rrg = agdata[7];
	float rrb = agdata[8];
	// green channel
	float glr = agdata[9];
	float glg = agdata[10];
	float glb = agdata[11];
	float grr = agdata[12];
	float grg = agdata[13];
	float grb = agdata[14];
	// blue channel
	float blr = agdata[15];
	float blg = agdata[16];
	float blb = agdata[17];
	float brr = agdata[18];
	float brg = agdata[19];
	float brb = agdata[20];
	// gamma correction
	if (gamma > 0.1)
	{
		//r.rgb = pow(r.rgb, 1.0 / gamma);
		l.r = pow(l.r, 1.0 / gamma);
		l.g = pow(l.g, 1.0 / gamma);
		l.b = pow(l.b, 1.0 / gamma);
		r.r = pow(r.r, 1.0 / gamma);
		r.g = pow(r.g, 1.0 / gamma);
		r.b = pow(r.b, 1.0 / gamma);
	}
	//
	float red	= l.r * rlr	+ l.g * rlg	+ l.b * rlb	+ r.r * rrr	+ r.g * rrg	+ r.b * rrb;
	float green = l.r * glr	+ l.g * glg	+ l.b * glb	+ r.r * grr	+ r.g * grg	+ r.b * grb;
	float blue	= l.r * blr	+ l.g * blg	+ l.b * blb	+ r.r * brr	+ r.g * brg	+ r.b * brb;
	vec4 o = vec4(red, green, blue, 1.0);
	o.rgb = (o.rgb - 0.5) * (contrast + 1.0) + 0.5;
	o.rgb = o.rgb + brightness;
	// gamma correction
	if (gamma > 0.1)
	{
		o.r = pow(o.r, gamma);
		o.g = pow(o.g, gamma);
		o.b = pow(o.b, gamma);
	} 
    return o;   
}

void main(void)
{
    vec4 uiColor = uiColor(vUV);
    vec4 l = viewColor(0.0, vUV);
    vec4 r = viewColor(1.0, vUV);
    vec4 ret = anaglyphMix(l, r);
    //ret.rgb = mix(ret.rgb, uiColor.rgb, uiColor.a);
	ret.a = 1.0;
    gl_FragColor = ret;
}
