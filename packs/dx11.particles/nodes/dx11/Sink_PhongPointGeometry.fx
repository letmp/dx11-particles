//#include "../fxh/Defines.fxh"
#include "../fxh/PhongPoint.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AlivePointerBuffer;
StructuredBuffer<uint> AliveCounterBuffer;

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
	p.xyz += ParticleBuffer[particleIndex].position;
	
	Out.pos = mul(p,mul(tW,tVP));
	
	
	 //inverse light direction in view space
	Out.PosW = mul(p, tW).xyz;
	float3 LightDirW = normalize(lPos - Out.PosW);
    Out.LightDirV = mul(float4(LightDirW,0.0f), tV).xyz;
	
    //normal in view space
    Out.NormV = normalize(mul(mul(In.NormO, (float3x3)tWIT),(float3x3)tV).xyz);
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
    return col * PhongPoint(In.PosW, In.NormV, In.ViewDirV, In.LightDirV);
}

/* ===================== TECHNIQUE ===================== */

technique10 PhongDirectional
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
