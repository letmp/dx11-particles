#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float lifespan;
		float age;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;

StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;

cbuffer cbuf
{
	uint EmitterSize = 0;
	uint EmitCountPerParticle = 1;
	bool ResetAge = 0;
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
	
	uint particleIndex = EMITTEROFFSET + input.DTID.x;

	float currentLifespan = ParticleBuffer[particleIndex].lifespan;
	
	if (currentLifespan <= 0.0f){
		// this counter is just for checking if we already copied all particles
		uint emitterCounter = EmitterCounterBuffer.IncrementCounter(); 
		if ( 	(FlagBuffer[0] == true && emitterCounter >= SelectionCounterBuffer[0] * EmitCountPerParticle) ||
				(FlagBuffer[0] == false && emitterCounter >= AliveCounterBuffer[0] * EmitCountPerParticle) ) return;
		
		// update AlivePointerBuffer
		uint aliveIndex = AliveCounterBuffer[0] + AliveCounterBuffer.IncrementCounter();
		AlivePointerBuffer[aliveIndex] = particleIndex;
		
		// copy particle
		Particle p = (Particle) 0;
		
		uint particleIndexToCopy = -1;
		if( FlagBuffer[0] == true) particleIndexToCopy = GetParticleIndex( emitterCounter % SelectionCounterBuffer[0]);
		else if( FlagBuffer[0] == false) particleIndexToCopy = GetParticleIndex( emitterCounter % AliveCounterBuffer[0]);
		
		if (particleIndexToCopy == -1 ) return;
		
		p = ParticleBuffer[particleIndexToCopy];
		
		// update lifespan
		uint size, stride;
		LifespanBuffer.GetDimensions(size,stride);
		p.lifespan = LifespanBuffer[emitterCounter % size];
		
		if (ResetAge) p.age = 0;
		
		ParticleBuffer[particleIndex] = p;
	}
	
	
}

technique11 EmitParticles { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Emit() ) );} }