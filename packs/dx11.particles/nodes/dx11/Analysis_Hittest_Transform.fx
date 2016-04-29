#include "../fxh/Defines.fxh"
#include "../fxh/Functions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AliveIndexBuffer;
StructuredBuffer<uint> AliveCounterBuffer;



float4x4 tFilter : WORLD;
int drawIndex : DRAWINDEX;
RWStructuredBuffer<uint> HitBuffer : BACKBUFFER;


struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1,1,1)]
void CS_Clear(uint3 i : SV_DispatchThreadID)
{
	HitBuffer[drawIndex] = 0;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_HitTest(csin input)
{
	if (input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0]) return;
	
	uint slotIndex = AliveIndexBuffer[input.DTID.x];
	
	float3 pointCoord = mul(float4(ParticleBuffer[slotIndex].position,1), tFilter).xyz;
	if(	!(pointCoord.x < -0.5 || pointCoord.x > 0.5 ||
		pointCoord.y < -0.5 || pointCoord.y > 0.5 ||
		pointCoord.z < -0.5 || pointCoord.z > 0.5
		)){
			uint oldval;
			InterlockedAdd(HitBuffer[drawIndex],1,oldval);
		}
	
}

technique11 Clear { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Clear() ) );} }
technique11 HitTest { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_HitTest() ) );} }