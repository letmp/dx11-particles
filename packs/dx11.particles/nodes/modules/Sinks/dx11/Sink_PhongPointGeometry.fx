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

/* ===================== HELPER FUNCTIONS ===================== */

#if !defined(PI)
	#define PI 3.1415926535897932
	#define TWOPI 6.283185307179586;
#endif

	float4x4 VRotate(float3 rot)
  {
   rot.x *= TWOPI;
   rot.y *= TWOPI;
   rot.z *= TWOPI;
   float sx = sin(rot.x);
   float cx = cos(rot.x);
   float sy = sin(rot.y);
   float cy = cos(rot.y);
   float sz = sin(rot.z);
   float cz = cos(rot.z);
 
   return float4x4( cz*cy+sz*sx*sy, sz*cx, cz*-sy+sz*sx*cy, 0,
                   -sz*cy+cz*sx*sy, cz*cx, sz*sy+cz*sx*cy , 0,
                    cx * sy       ,-sx   , cx * cy        , 0,
                    0             , 0    , 0              , 1);
  }

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
	
	#if defined(KNOW_ROTATION)
		p = mul(p,VRotate(ParticleBuffer[particleIndex].rotation));
 	#endif
	
	p.xyz += ParticleBuffer[particleIndex].position;

	Out.pos = mul(p,mul(tW,tVP));
	
	 //inverse light direction in view space
	Out.PosW = mul(p, tW).xyz;
	float3 LightDirW = normalize(lPos - Out.PosW);
    Out.LightDirV = mul(float4(LightDirW,0.0f), tV).xyz;
	
	//normal in view space
	float3 norm = In.NormO;
	#if defined(KNOW_ROTATION)
		norm = mul(float4(norm,1),VRotate(ParticleBuffer[particleIndex].rotation)).xyz;
 	#endif
    Out.NormV = normalize(mul(mul(norm, (float3x3)tWIT),(float3x3)tV).xyz);
	
	//position (projected)
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

technique10 TPhongPoint
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
