#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 force;
		float lifespan;
		float age;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;

RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

StructuredBuffer<float3> PositionBuffer <string uiname="Position Buffer";>;
StructuredBuffer<float3> VelocityBuffer <string uiname="Velocity Buffer";>;
StructuredBuffer<float3> ForceBuffer <string uiname="Force Buffer";>;
StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;

cbuffer cbuf
{
    uint EmitCount = 0;
	uint EmitterSize = 0;
}

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Main(csin input)
{
	
	if(input.DTID.x >= EmitterSize) return;
	
	uint slotIndex = EMITTEROFFSET + input.DTID.x;

	float currentLifespan = ParticleBuffer[slotIndex].lifespan;
	if ( currentLifespan <= 0.0f){
		
		// this counter is just for checking if we already emitted enough particles
		uint emitterCounter = EmitterCounterBuffer.IncrementCounter(); 
		if (emitterCounter >= EmitCount )return;
		
		uint aliveIndex = AliveCounterBuffer.IncrementCounter();
		
		uint size, stride;
		Particle p = (Particle) 0;

		PositionBuffer.GetDimensions(size,stride);
		p.position = PositionBuffer[emitterCounter % size];

		VelocityBuffer.GetDimensions(size,stride);
		p.velocity = VelocityBuffer[emitterCounter % size];
		
		ForceBuffer.GetDimensions(size,stride);
		p.force = ForceBuffer[emitterCounter % size];

		LifespanBuffer.GetDimensions(size,stride);
		p.lifespan = LifespanBuffer[emitterCounter % size];

		ParticleBuffer[slotIndex] = p;
		AliveIndexBuffer[aliveIndex] = slotIndex;
	}
}

[numthreads(1, 1, 1)]
void CS_SetCounter(csin input)
{
	uint emitterCounter = EmitterCounterBuffer.IncrementCounter();
	EmitterCounterBuffer[0] = emitterCounter;
	
	uint aliveCounter = AliveCounterBuffer.IncrementCounter();
	AliveCounterBuffer[0] = aliveCounter;
}

technique11 EmitParticles { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Main() ) );} }
technique11 SetCounter { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetCounter() ) );} }
