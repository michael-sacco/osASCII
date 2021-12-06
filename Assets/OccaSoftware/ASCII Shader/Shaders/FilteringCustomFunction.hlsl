#ifndef BOXFILTER_INCLUDE
#define BOXFILTER_INCLUDE

void BoxFilter_float(float2 UV, Texture2D Texture, SamplerState Sampler, float UVOffset, out float3 Out)
{
	#ifdef SHADERGRAPH_PREVIEW
	Out = float3(0,0,0);
	
	#else
	float2 offset = _MainTex_TexelSize.xy * float2(UVOffset, UVOffset);
	float3 sample1 = Texture.SampleLevel(Sampler, UV + float2(offset.x, offset.y), 0).rgb;
	float3 sample2 = Texture.SampleLevel(Sampler, UV + float2(-offset.x, -offset.y), 0).rgb;
	float3 sample3 = Texture.SampleLevel(Sampler, UV + float2(-offset.x, offset.y), 0).rgb;
	float3 sample4 = Texture.SampleLevel(Sampler, UV + float2(offset.x, -offset.y), 0).rgb;
	
	float3 sample = (sample1 + sample2 + sample3 + sample4) * 0.25;
	Out = sample;
	
	#endif
}
#endif