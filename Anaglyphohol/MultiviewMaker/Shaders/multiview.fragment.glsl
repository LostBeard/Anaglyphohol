// Multiview to many by Todd Tanner

#ifdef GL_ES
precision highp float;
#endif

// uniforms required for PostProcess
varying vec2 vUV;
// overlay texture
uniform sampler2D textureSampler;
// custom uniforms
uniform vec2 screenSize;
// 1 or more 2D views tiled, or 2D+Z
uniform sampler2D videoSampler;
// input layout type - 0 = tiled views, 1 = 2D+Z, 2 = 2D+ZD
uniform int inputLayout;	
// tiled input info (used by all input even single view)
uniform vec2 cols_rows_in; // = vec2(4.0, 2.0);
uniform bool views_index_invert_x; // false 0x is left, true 0x is right
uniform bool views_index_invert_y; // false 0y is top, true 0y is bottom
uniform float views_in_cnt; // = cols_rows_in.x * cols_rows_in.y;
uniform float views_in_max_index; // = views_in_cnt - 1.0;
uniform float primaryViewIndex;
uniform vec2 view_size_in; // = 1.0 / cols_rows_in;
// if 2d+z below uniforms must be set
uniform float rC0[4];
uniform int rI0[1];

uniform vec2 uv_scale; // = vec2(1.0, 1.2);
uniform vec2 uv_padding; // = vec2(0.0, 0.1);

const vec4 colorBlack = vec4(0.0, 0.0, 0.0, 1.0);

vec4 viewColorTiled(float view_index, vec2 view_uv) {
	vec2 view_uv_corrected_aspect = view_uv * uv_scale - uv_padding;
	if (view_uv_corrected_aspect.x < 0.0 || view_uv_corrected_aspect.x > 1.0 || view_uv_corrected_aspect.y < 0.0 || view_uv_corrected_aspect.y > 1.0) {
		return colorBlack;
	}
	if (view_index > views_in_max_index) view_index = views_in_max_index;
	vec2 view_indexes_in = vec2(mod(view_index, cols_rows_in.x), floor(view_index / cols_rows_in.x));
	if (inputLayout == 0) {
		if (views_index_invert_x) {
			view_indexes_in.x = cols_rows_in.x - view_indexes_in.x - 1.0;
		}
		if (!views_index_invert_y) {
			view_indexes_in.y = cols_rows_in.y - view_indexes_in.y - 1.0;
		}
	}
	vec2 uv = (view_indexes_in * view_size_in) + (view_uv_corrected_aspect * view_size_in);
	return texture2D(videoSampler, uv);
}

vec4 viewColor2DZ(float view_index, vec2 view_uv) {
	vec4 o = vec4(0.0, 0.0, 0.0, 0.5);
	if (views_index_invert_x) {
		view_index *= -1.0;
	}
	float sep_max_x = rC0[0] * abs(view_index);
	float pixel_width = rC0[1];
	float pixel_width_half = rC0[2];
	float offset_f = rC0[3];
	if (view_index == 0.0 || sep_max_x < pixel_width) {
		o = viewColorTiled(0.0, view_uv);
	}
	else {
		//vec4 d = viewColorTiled(1.0, vUV);
		// compute r using l and d
		float pDepth;
		vec2 uv = view_uv;
		vec2 uvNext = view_uv;
		float cur_depth = -2.0;
		float dest_x = 0.0;
		float diff_x = 0.0;
		float cur_coord_x = 0.0;
		float lowestDepthDiff = 1.0;
		float lowestDepth = 1.0;
		float lowestDepthX = 0.0;
		float shiftMode = view_index > 0.0 ? -1.0 : 1.0;	// ++-+
		float pixel_width_signed = pixel_width * shiftMode;
		float sep_max_x_signed = sep_max_x * shiftMode;
		float offset_f_signed = offset_f * sep_max_x_signed;
		float start_x = uv.x - sep_max_x_signed + offset_f_signed;
		start_x = start_x - mod(start_x, pixel_width);
		//uvNext.x = start_x;
		//uvNext.x = uv.x - sep_max_x; // + (pixel_width * 2.0);
		//uvNext.x += pixel_width * shiftMode * 2.0;
		for (int n = 0; n < 100; n++)
		{
			if (n >= rI0[0]) break;
			uvNext.x = start_x + (pixel_width_signed * float(n));
			pDepth = viewColorTiled(1.0, uvNext.xy).r;
			//pDepth = clamp(pDepth, 0.0, 1.0);
			dest_x = uvNext.x + (pDepth * sep_max_x_signed) - offset_f_signed;
			diff_x = abs(uv.x - dest_x);
			if (diff_x <= pixel_width && cur_depth <= pDepth)
			{
				cur_depth = pDepth;
				cur_coord_x = uvNext.x;
				o.a = 1.0;
			}
			if (pDepth <= lowestDepth && diff_x <= lowestDepthDiff + pixel_width) {
				lowestDepthDiff = diff_x;
				lowestDepth = pDepth;
				lowestDepthX = uvNext.x;
			}
			//uvNext.x += pixel_width; //
			//uvNext.x = (uv.x - sep_max_x) + (pixel_width * n);
			//if (uvNext.x >= uv.x + sep_max_x) test += 1.0;
		}
		if (o.a == 1.0) {
			//o = texture2D(mapToShift, vec2(cur_coord_x - (pixel_width * shiftMode), uvNext.y));
			o = viewColorTiled(0.0, vec2(cur_coord_x, uvNext.y));
			//frag_out.c1.r = cur_depth;
		}
		else
		{
			// fill
			o = viewColorTiled(0.0, vec2(lowestDepthX, uvNext.y));
			//frag_out.c1.r = lowestDepth; // vec4(lowestDepth, lowestDepth, lowestDepth, 1.0);
		}
	}
	return o;
}

vec4 viewColor(float view_index, vec2 view_uv) {
	if (inputLayout == 0) {
		return viewColorTiled(view_index, view_uv);
	}
	else {
		return viewColor2DZ(view_index, view_uv);
	}
}

vec4 pseudoSBS(vec2 view_uv) {
	if (view_uv.x > 0.5) {
		return viewColorTiled(1.0, vec2((view_uv.x - 0.5) * 2.0, view_uv.y));
	}
	else {
		return viewColorTiled(0.0, vec2(view_uv.x * 2.0, view_uv.y));
	}
}

vec4 uiColor(vec2 uv) {
	return texture2D(textureSampler, uv);
}