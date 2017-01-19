#include <packs\dx11.particles\nodes\modules\Effects\fxh\MultiGouraud.fxh>
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
	float4x4 tWVP: WORLDVIEWPROJECTION;
	float4x4 tVP : VIEWPROJECTION;	
	float4x4 tP: PROJECTION;
	float4x4 tWV: WORLDVIEW;
	float4x4 tWIT: WORLDINVERSETRANSPOSE;
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
	float4 cAmb <bool color=true;String uiname="Default Color";> = { 1.0f,1.0f,1.0f,1.0f };
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
	
	float3 PosW: TEXCOORD1;
	float3 NormV: TEXCOORD2;
	float3 ViewDirV: TEXCOORD3;
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

/* ===================== PIXEL SHADER ===================== */

float4 PS(VSOut In): SV_Target
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
				colLight += MultiGouraudPoint(i, In.PosW, In.NormV, In.ViewDirV, tV);
				break;
			case 1:
				colLight += MultiGouraudDirectional(i, In.NormV, In.ViewDirV, tV);
				break;
		}
	}

    return col * colLight;
}

/* ===================== TECHNIQUE ===================== */

technique10 MultiGouraud
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_5_0, PS() ) );
	}
}
