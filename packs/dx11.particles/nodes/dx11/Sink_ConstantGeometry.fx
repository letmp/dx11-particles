//@author: tmp 
//@help: Draws a PointCloud
//@tags: DX11.Pointcloud
//@credits: vvvv

#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 force;
		float lifespan;
		float age;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;

cbuffer cbPerDraw : register( b0 )
{
	float4x4 tVP : VIEWPROJECTION;
	float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
};

struct vsInput
{
	float4 pos : POSITION;
	uint ii : SV_InstanceID;
};

struct vs2ps
{
    float4 position: SV_POSITION;
	float4 color : COLOR;
};

/* ===================== VERTEX SHADER ===================== */

vs2ps VS(vsInput input)
{
    vs2ps output = (vs2ps)0;
    
	uint ii = input.ii;
	
	// set position of geometry
	float4 p = input.pos;
	p.xyz += ParticleBuffer[ii].position;
	// don't draw dead particles
	if (ParticleBuffer[ii].lifespan <= 0.0f) p.w = 0;
	output.position = mul(p,mul(tW,tVP));
	
	// set color
	#if defined(KNOW_COLOR)
		output.color = ParticleBuffer[ii].color;
 	#else
		output.color = float4(1,1,1,1);
	#endif
	
	return output;
}

/* ===================== PIXEL SHADER ===================== */

float4 PS_COLOR(vs2ps input): SV_Target
{
    return (cAmb * input.color);
}


/* ===================== TECHNIQUE ===================== */
technique10 Color
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_COLOR() ) );
	}
}
