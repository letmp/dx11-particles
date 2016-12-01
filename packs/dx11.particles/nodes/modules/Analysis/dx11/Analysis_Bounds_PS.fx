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
StructuredBuffer<uint> GroupIndexBuffer;

float2 R:TARGETSIZE;

struct vsInput
{
	uint vi :  SV_VertexID;
};

struct vs2ps
{
    float4 pos: SV_POSITION;
	float4 col: COLOR;
};

/* ===================== VERTEX SHADER ===================== */

vs2ps VS(vsInput input)
{
    vs2ps output = (vs2ps)0;
    
	uint particleIndex = AlivePointerBuffer[input.vi];
	uint groupId = GroupIndexBuffer[particleIndex];
		
	float halfPixel = (1.0f / R.x) * 0.5f;
	
	float pixPos = (groupId * (1.0f / R.x)) * 2 - 1.0f;
	pixPos += halfPixel;
	
	output.pos  = float4(pixPos , 0.0 ,0.0 ,1.0);
	output.col = float4(ParticleBuffer[particleIndex].position, 1.0f );
	if (groupId == -1 ) output.col = float4(0,0,0,0);
	
	return output;
}

/* ===================== PIXEL SHADER ===================== */

float4 PS(vs2ps input): SV_Target
{
    return input.col;
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