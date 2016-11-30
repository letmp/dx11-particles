
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
	#if defined(KNOW_ROTATION)
		p = mul(p,VRotate(ParticleBuffer[particleIndex].rotation));
 	#endif	
	p.xyz += ParticleBuffer[particleIndex].position;
	Out.pos = mul(p,mul(tW,tVP));
	
	//inverse light direction in view space
    float3 LightDirV = normalize(-mul(float4(lDir,0.0f), tV).xyz);

    //normal in view space
	float3 norm = In.NormO;
	#if defined(KNOW_ROTATION)
		norm = mul(float4(norm,1),VRotate(ParticleBuffer[particleIndex].rotation)).xyz;
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
