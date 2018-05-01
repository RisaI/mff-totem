#include "Macros.fxh"

Texture2D DiffuseMap : register(t0);
Texture2D Lightmap : register(t1);
sampler DiffuseSampler = sampler_state {
	Texture = <DiffuseMap>;
};
sampler LightmapSampler = sampler_state {
	Texture = <Lightmap>;
};

struct VertexIn
{
	float2 Position : SV_POSITION0;
	float2 TexCoord : TEXCOORD0;
};

struct VertexOut
{
	float4 Position : SV_POSITION;
	float2 TexCoord : TEXCOORD0;
};

VertexOut VS(VertexIn vin)
{
	VertexOut vout;

	vout.Position = float4(vin.Position.x, vin.Position.y, 0.0, 1.0);
	vout.TexCoord = vin.TexCoord;

	return vout;
}

float4 PS(VertexOut pin) : SV_TARGET
{
	float4 diffuse = tex2D(DiffuseSampler, pin.TexCoord);
	float4 light = tex2D(LightmapSampler, pin.TexCoord);
	return diffuse * light;
}

TECHNIQUE(Main, VS, PS);
