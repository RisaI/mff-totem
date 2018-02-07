float Time;
float2 Resolution;

struct VertexShaderInput
{
	float4 Position : POSITION0;
	float4 Color : COLOR0;
};

struct VertexShaderOutput
{
	float4 Color : COLOR0;
	float4 Position : POSITION0;
	float3 TexCoord : TEXCOORD0;
};

VertexShaderOutput MainVS(VertexShaderInput input)
{
	VertexShaderOutput output;
	output.Position = input.Position;
	output.Color = input.Color;
	output.TexCoord = input.Position.xyz;
	return output;
}


float4 MainPS(VertexShaderOutput input) : COLOR0
{
    float2 uv = input.TexCoord.xy * Resolution.xy / Resolution.y;
    
    //return float4(length(uv), 0, 1, 1.0);
    float timer=fmod(Time, 3.0);
    float radius=2.0/timer;
    float width=timer*3.0;
    float ring=length(float2(uv.x, uv.y))*radius*width-width;//(timer/4.0)-3.0;
    ring=min(2.0, abs(1.0/(10.0*ring)));
    ring=max(0.0, ring-timer);
    return float4(ring*0.3, ring+.05, ring-0.05, 1.0);
}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
};