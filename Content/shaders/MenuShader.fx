#define iterations 17
#define formuparam 0.53

#define volsteps 20
#define stepsize 0.1

#define zoom   0.800
#define tile   0.850
#define speed  0.010 

#define brightness 0.0015
#define darkmatter 0.300
#define distfading 0.730
#define saturation 0.850

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
    float2 uv = input.TexCoord.xy / 2.0;
    
    /*float timer=fmod(Time, 3.0);
    float radius=2.0/timer;
    float width=timer*3.0;
    float ring=length(float2(uv.x, uv.y))*radius*width-width;//(timer/4.0)-3.0;
    ring=min(2.0, abs(1.0/(10.0*ring)));
    ring=max(0.0, ring-timer);
    return float4(ring*0.3, ring+.05, ring-0.05, 1.0);*/

	uv.y*=Resolution.y/Resolution.x;
	float3 dir=float3(uv*zoom,1.);
	float time=Time*speed+.25;
	
	//volumetric rendering
	float s=0.1,fade=1.;
	float3 v=float3(0.0,0.0,0.0);
	for (int r=0; r<volsteps; r++) {
		float3 p=float3(1.+time*2.,.5+time,0.5-2.)+s*dir*.5;
		p = abs(float3(tile,tile,tile)-fmod(p,float3(tile*2.,tile*2.,tile*2.))); // tiling fold
		float pa,a=pa=0.;
		for (int i=0; i<iterations; i++) { 
			p=abs(p)/dot(p,p)-formuparam; // the magic formula
			a+=abs(length(p)-pa); // absolute sum of average change
			pa=length(p);
		}
		float dm=max(0.,darkmatter-a*a*.001); //dark matter
		a*=a*a; // add contrast
		if (r>6) fade*=1.-dm; // dark matter, don't render near
		//v+=float3(dm,dm*.5,0.);
		v+=fade;
		v+=float3(s,s*s,s*s*s*s)*a*brightness*fade; // coloring based on distance
		fade*=distfading; // distance fading
		s+=stepsize;
	}
	v=lerp(float3(length(v),length(v),length(v)),v,saturation); //color adjust
	return float4(v*.01,1.);	

}

technique BasicColorDrawing
{
	pass P0
	{
		VertexShader = compile vs_3_0 MainVS();
		PixelShader = compile ps_3_0 MainPS();
	}
};