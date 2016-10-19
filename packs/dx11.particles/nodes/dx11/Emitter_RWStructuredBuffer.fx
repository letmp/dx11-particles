#include "../fxh/Defines.fxh"

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
RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;
StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;

#include "../fxh/IndexFunctions.fxh"

cbuffer cbuf
{
	uint EmitterSize = 0;
}

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Emit(csin input)
{	
	if(input.DTID.x >= EmitterSize) return;
	
	uint slotIndex = EMITTEROFFSET + input.DTID.x;

	float currentLifespan = ParticleBuffer[slotIndex].lifespan;
	if ( currentLifespan <= 0.0f){
		
		// this counter is just for checking if we already copied all particles
		uint emitterCounter = EmitterCounterBuffer.IncrementCounter(); 
		if ( 	(FlagBuffer[0] == true && emitterCounter >= SelectionCounterBuffer[0]) ||
				(FlagBuffer[0] == false && emitterCounter >= AliveCounterBuffer[0])) return;
		
		// update AliveIndexBuffer
		uint aliveIndex = AliveCounterBuffer[0] + AliveCounterBuffer.IncrementCounter();
		AliveIndexBuffer[aliveIndex] = slotIndex;
		
		// copy particle
		Particle p = (Particle) 0;
		
		uint slotIndexToCopy = GetSlotIndex( emitterCounter );
		if (slotIndexToCopy == -1 ) return;
		
		p = ParticleBuffer[slotIndexToCopy];
		
		// update lifespan
		uint size, stride;
		LifespanBuffer.GetDimensions(size,stride);
		p.lifespan = LifespanBuffer[emitterCounter % size];
		
		ParticleBuffer[slotIndex] = p;
	}
}

technique11 EmitParticles { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Emit() ) );} }