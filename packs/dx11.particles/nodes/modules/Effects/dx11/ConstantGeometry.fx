#include "../../Core/fxh/AlgebraFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AlivePointerBuffer;

cbuffer cbPerDraw : register( b0 )
{
	float4x4 tV: VIEW;
	float4x4 tWV: WORLDVIEW;
	float4x4 tWVP: WORLDVIEWPROJECTION;
	float4x4 tVP : VIEWPROJECTION;	
	float4x4 tP: PROJECTION;
	float4x4 tWIT: WORLDINVERSETRANSPOSE;
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
};

/* ===================== STRUCTURES ===================== */

struct VSIn
{
	float4 pos : POSITION;
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSOut
{
    float4 pos: SV_POSITION;
	uint particleIndex : VID;
};

/* ===================== VERTEX SHADER ===================== */

VSOut VS(VSIn In)
{
    VSOut Out = (VSOut)0;
    
	uint particleIndex = AlivePointerBuffer[In.ii];
	Out.particleIndex = particleIndex;
	
	float4 p = In.pos;
	#if defined(KNOW_SCALE)
		p = mul(p,MatrixScaling(ParticleBuffer[particleIndex].scale));
 	#endif	
	#if defined(KNOW_ROTATION)
		p = mul(p,MatrixRotation(ParticleBuffer[particleIndex].rotation));
 	#endif
	
	p.xyz += ParticleBuffer[particleIndex].position;
	Out.pos = mul(p,mul(tW,tVP));
	
	return Out;
}

/* ===================== PIXEL SHADER ===================== */

float4 PS(VSOut In): SV_Target
{

	float4 col = float4(1,1,1,1);
	 #if defined(KNOW_COLOR)
       col *= ParticleBuffer[In.particleIndex].color;
    #endif
	if (col.a == 0.0f) discard;
    return col;
}

/* ===================== TECHNIQUE ===================== */

technique10 TPhongDirectional
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
