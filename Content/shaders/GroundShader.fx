matrix View;
matrix Projection;
texture2D Texture;
sampler TextureSampler = sampler_state {
	Texture = <Texture>;
};

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
	float3 TextureCoordinate : TEXCOORD0;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = mul(mul(input.Position, View), Projection);
	output.Color = input.Color;
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
	return tex2D(TextureSampler, NegModulo(input.TextureCoordinate, 256) / (256, 256)) * input.Color;
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
};