#if OPENGL
	#define SV_POSITION POSITION
	#define VS_SHADERMODEL vs_3_0
	#define PS_SHADERMODEL ps_3_0
#else
	#define VS_SHADERMODEL vs_4_0_level_9_1
	#define PS_SHADERMODEL ps_4_0_level_9_1
#endif

matrix View;
matrix Projection;
texture2D Texture;
sampler TextureSampler = sampler_state {
	Texture = <Texture>;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float3 TextureCoordinate : TEXCOORD0;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(mul(input.Position, View), Projection);
	output.TextureCoordinate = input.Position;
	return output;
}

float2 NegModulo(float2 f, float m)
{
	float2 result = f % (m, m);
	if (f.x < 0)
		result.x += m;
	if (f.y < 0)
		result.y += m;
	return result;
}

float4 MainPS(VertexShaderOutput input) : COLOR0
{
	return tex2D(TextureSampler, NegModulo(input.TextureCoordinate, 256) / (256, 256));
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile VS_SHADERMODEL MainVS();
		PixelShader = compile PS_SHADERMODEL MainPS();
	}
};