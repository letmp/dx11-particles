#include <packs\dx11.particles\nodes\modules\Core\fxh\AlgebraFunctions.fxh>

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
	float4 cAmb <bool color=true;String uiname="Default Color";> = { 1.0f,1.0f,1.0f,1.0f };
};

cbuffer cbTextureData : register(b2)
{
	float4x4 tTex <string uiname="Texture Transform"; bool uvspace=true; >;
};

Texture2D inputTexture <string uiname="Texture";>;

SamplerState linearSampler <string uiname="Sampler State";>
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

/* ===================== STRUCTURES ===================== */

struct VSIn
{
	float4 pos : POSITION;
	float4 uv: TEXCOORD0;
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSInNoTexture
{
	float4 pos : POSITION;
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSOut
{
    float4 pos: SV_POSITION;
	float4 uv: TEXCOORD0;
	uint particleIndex : VID;
};

struct VSOutNoTexture
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
	
	Out.uv = mul(In.uv, tTex);
	
	return Out;
}

VSOutNoTexture VS_NoTexture(VSInNoTexture In)
{
    VSOutNoTexture Out = (VSOutNoTexture)0;
    
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

	float4 col = inputTexture.Sample(linearSampler,In.uv.xy) * cAmb;
	 #if defined(KNOW_COLOR)
       col = inputTexture.Sample(linearSampler,In.uv.xy) * ParticleBuffer[In.particleIndex].color;
    #endif
	if (col.a == 0.0f) discard;
    return col;
}

float4 PS_NoTexture(VSOutNoTexture In): SV_Target
{

	float4 col = cAmb;
	 #if defined(KNOW_COLOR)
       col = ParticleBuffer[In.particleIndex].color;
    #endif
	if (col.a == 0.0f) discard;
    return col;
}

/* ===================== TECHNIQUE ===================== */

technique10 Constant <string noTexCdFallback="ConstantNoTexture"; >
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}

technique10 ConstantNoTexture
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS_NoTexture() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_NoTexture() ) );
	}
}
