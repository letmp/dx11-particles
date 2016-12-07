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
	float4x4 tWVP: WORLDVIEWPROJECTION;
	float4x4 tVP : VIEWPROJECTION;	
	float4x4 tP: PROJECTION;
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
	float4x4 tWV: WORLDVIEW;
	float4x4 tWIT: WORLDINVERSETRANSPOSE;
	
	float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
};

cbuffer cbLightData : register(b3)
{
	float3 lDir <string uiname="Light Direction";> = {0, -5, 2}; 
	float4 lAmb  <bool color=true; String uiname="Ambient Color";>  = {0.15, 0.15, 0.15, 1};
	float4 lDiff <bool color=true;String uiname="Diffuse Color";>  = {0.85, 0.85, 0.85, 1};
	float4 lSpec <bool color=true; String uiname="Specular Color";> = {0.35, 0.35, 0.35, 1};
	float lPower <String uiname="Power"; float uimin=3.0;> = 25.0;     	
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
	
	float4 Diffuse: COLOR0;
    float4 Specular: COLOR1;
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
    float3 LightDirV = normalize(-mul(float4(lDir,0.0f), tV).xyz);

    //normal in view space
	float3 norm = In.NormO;
	#if defined(KNOW_SCALE)
		norm = mul(float4(norm,1),MatrixScaling(ParticleBuffer[particleIndex].scale)).xyz;
 	#endif
	#if defined(KNOW_ROTATION)
		norm = mul(float4(norm,1),MatrixRotation(ParticleBuffer[particleIndex].rotation)).xyz;
 	#endif
    float3 NormV = normalize(mul(mul(norm, (float3x3)tWIT),(float3x3)tV).xyz);
	
    //view direction = inverse vertexposition in viewspace
    float4 PosV = mul(p, tWV);
    float3 ViewDirV = normalize(-PosV.xyz);

    //halfvector
    float3 H = normalize(ViewDirV + LightDirV);

    //compute blinn lighting
    float3 shades = lit(dot(NormV, LightDirV), dot(NormV, H), lPower).xyz;

    float4 diff = lDiff * shades.y;
    float4 spec = lSpec * shades.z;

    //normal in view space
    Out.Diffuse = diff + lAmb;
    Out.Specular = spec;
	
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
	
	col.xyz *= In.Diffuse.xyz + In.Specular.xyz;
	
    return col;
}

/* ===================== TECHNIQUE ===================== */

technique10 TGouraudDirectional
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
