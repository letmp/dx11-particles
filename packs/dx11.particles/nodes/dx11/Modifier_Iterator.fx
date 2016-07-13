#include "../fxh/Defines.fxh"
#include "../fxh/Functions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 acceleration;
		float lifespan;
		float age;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;

RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

cbuffer cbuf
{
	float VelDampen = 1.0;
    bool AddForce = true;
};

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Iterate(csin input)
{
	/*uint slotIndex = getSlotIndex(	input.DTID.x,
									FlagBuffer,
									SelectionIndexBuffer,
									SelectionCounterBuffer,
									AliveIndexBuffer,
									AliveCounterBuffer);
	if (slotIndex < 0 ) return;*/
	
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	uint slotIndex = input.DTID.x;

	ParticleBuffer[slotIndex].age += psTime.y;
	ParticleBuffer[slotIndex].lifespan -= psTime.y;

	if(AddForce)
	{
		ParticleBuffer[slotIndex].velocity *= VelDampen;
		ParticleBuffer[slotIndex].velocity += ParticleBuffer[slotIndex].acceleration * psTime.y;
	}
	
	ParticleBuffer[slotIndex].position += ParticleBuffer[slotIndex].velocity * psTime.y;
}

technique11 Iterate { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Iterate() ) );} }
