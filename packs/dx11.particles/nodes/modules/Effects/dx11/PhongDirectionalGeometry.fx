#include "../fxh/PhongDirectional.fxh"
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
	float3 NormO: NORMAL;
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSOut
{
    float4 pos: SV_POSITION;
	uint particleIndex : VID;
	
	float3 LightDirV: TEXCOORD4;
    float3 NormV: TEXCOORD5;
    float3 ViewDirV: TEXCOORD6;
	float3 PosW: TEXCOORD7;
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
	
	
	 //inverse light direction in view space
	Out.PosW = mul(p, tW).xyz;
	
	Out.LightDirV = normalize(-mul(float4(lDir,0.0f), mul(tW,tV)).xyz);
	
	float3 norm = In.NormO;
	#if defined(KNOW_SCALE)
		norm = mul(float4(norm,1),MatrixScaling(ParticleBuffer[particleIndex].scale)).xyz;
 	#endif
	#if defined(KNOW_ROTATION)
		norm = mul(float4(norm,1),MatrixRotation(ParticleBuffer[particleIndex].rotation)).xyz;
 	#endif

    //normal in view space
    Out.NormV = normalize(mul(mul(norm, (float3x3)tWIT),(float3x3)tV).xyz);
	Out.ViewDirV = -normalize(mul(p, tWV).xyz);
	
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
    return col * PhongDirectional(In.NormV, In.ViewDirV, In.LightDirV);
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
