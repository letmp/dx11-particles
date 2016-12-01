#include "../../Core/fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AlivePointerBuffer;
StructuredBuffer<int> LabelBuffer;
StructuredBuffer<int> MaskIdBuffer;

float2 R:TARGETSIZE;

cbuffer cbPerDraw : register( b0 )
{
    float4x4 tVP : VIEWPROJECTION;
};

struct VSIn
{
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct vs2ps
{
    float4 pos: SV_POSITION;
	float4 col: COLOR;
	uint particleIndex : VID;
};

/* ===================== VERTEX SHADER ===================== */

vs2ps VS(VSIn In)
{
    vs2ps Out = (vs2ps)0;
	
    uint particleIndex = AlivePointerBuffer[In.iv];
	Out.particleIndex = particleIndex;
	
	float3 p = ParticleBuffer[particleIndex].position;
    Out.pos = mul(float4(p, 1), tVP);

	float2 uv = Out.pos.xy/Out.pos.w;
	
	float x = uv.x * 0.5 + 0.5;
	float y = uv.y * -0.5 + 0.5;

	int count = y * R.y;
	int id = (x * R.x) + (count * R.x);
	if (id > 0 && id < R.x * R.y){
		int label = LabelBuffer[id];
		int maskId = MaskIdBuffer[label - 1];
		if( label > 0 && maskId != -1){
			Out.col = float4(maskId,0,0,1);

		}
	}
	
    return Out;
}

/* ===================== PIXEL SHADER ===================== */

float4 PS(vs2ps In): SV_Target
{
    return In.col;
}

/* ===================== TECHNIQUE ===================== */

technique10 Process
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_5_0, VS() ) );
		SetPixelShader( CompileShader( ps_5_0, PS() ) );
	}
}