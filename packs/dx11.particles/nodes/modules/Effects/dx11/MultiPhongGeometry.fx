#include <packs\dx11.particles\nodes\modules\Effects\fxh\MultiPhong.fxh>
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
	float4 cAmb <bool color=true;String uiname="Default Color";> = { 0.2f,0.2f,0.2f,1.0f };
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
	float3 NormO: NORMAL;
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSInNoTexture
{
	float4 pos : POSITION;
	float3 NormO: NORMAL;
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSOut
{
    float4 pos: SV_POSITION;
	float4 uv: TEXCOORD0;
	uint particleIndex : VID;
	
	float3 PosW: TEXCOORD1;
    float3 NormV: TEXCOORD2;
    float3 ViewDirV: TEXCOORD3;
};

struct VSOutNoTexture
{
    float4 pos: SV_POSITION;
	uint particleIndex : VID;
	
	float3 PosW: TEXCOORD1;
    float3 NormV: TEXCOORD2;
    float3 ViewDirV: TEXCOORD3;
};

/* ===================== VERTEX SHADER ===================== */

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
	
	 //inverse light direction in view space
	Out.PosW = mul(p, tW).xyz;
	
	//normal in view space
	float3 norm = In.NormO;
	#if defined(KNOW_SCALE)
		norm = mul(float4(norm,1),MatrixScaling(ParticleBuffer[particleIndex].scale)).xyz;
 	#endif
	#if defined(KNOW_ROTATION)
		norm = mul(float4(norm,1),MatrixRotation(ParticleBuffer[particleIndex].rotation)).xyz;
 	#endif
    Out.NormV = normalize(mul(mul(norm, (float3x3)tWIT),(float3x3)tV).xyz);
	
	//position (projected)
	Out.ViewDirV = -normalize(mul(p, tWV).xyz);
	
	return Out;
}

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
	
	//normal in view space
	float3 norm = In.NormO;
	#if defined(KNOW_SCALE)
		norm = mul(float4(norm,1),MatrixScaling(ParticleBuffer[particleIndex].scale)).xyz;
 	#endif
	#if defined(KNOW_ROTATION)
		norm = mul(float4(norm,1),MatrixRotation(ParticleBuffer[particleIndex].rotation)).xyz;
 	#endif
    Out.NormV = normalize(mul(mul(norm, (float3x3)tWIT),(float3x3)tV).xyz);
	
	//position (projected)
	Out.ViewDirV = -normalize(mul(p, tWV).xyz);
	
	Out.uv = mul(In.uv, tTex);
	
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
	
	uint lPosDirCount, dummy;
    lPosDirBuffer.GetDimensions(lPosDirCount, dummy);
	uint lTypeCount;
    lTypeBuffer.GetDimensions(lTypeCount, dummy);
	
	float4 colLight = 0;
	for(int i = 0; i < lPosDirCount; i++){
		switch (lTypeBuffer[i % lTypeCount]){
			case 0:
				colLight += MultiPhongPoint(i, In.PosW, In.NormV, In.ViewDirV, tV, In.particleIndex);
				break;
			case 1:
				colLight += MultiPhongDirectional(i, In.NormV, In.ViewDirV, tV, In.particleIndex);
				break;
		}
	}

    return col * colLight;
}

float4 PS_NoTexture(VSOutNoTexture In): SV_Target
{

	float4 col = cAmb;
	#if defined(KNOW_COLOR)
       col = ParticleBuffer[In.particleIndex].color;
    #endif
	if (col.a == 0.0f) discard;
	
	uint lPosDirCount, dummy;
    lPosDirBuffer.GetDimensions(lPosDirCount, dummy);
	uint lTypeCount;
    lTypeBuffer.GetDimensions(lTypeCount, dummy);
	
	float4 colLight = 0;
	for(int i = 0; i < lPosDirCount; i++){
		switch (lTypeBuffer[i % lTypeCount]){
			case 0:
				colLight += MultiPhongPoint(i, In.PosW, In.NormV, In.ViewDirV, tV, In.particleIndex);
				break;
			case 1:
				colLight += MultiPhongDirectional(i, In.NormV, In.ViewDirV, tV, In.particleIndex);
				break;
		}
	}

    return col * colLight;
}

/* ===================== TECHNIQUE ===================== */

technique10 MultiPhong <string noTexCdFallback="MultiPhongNoTexture"; >
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_5_0, VS() ) );
		SetPixelShader( CompileShader( ps_5_0, PS() ) );
	}
}

technique10 MultiPhongNoTexture
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_5_0, VS_NoTexture() ) );
		SetPixelShader( CompileShader( ps_5_0, PS_NoTexture() ) );
	}
}
