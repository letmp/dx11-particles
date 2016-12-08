#include "../../Core/fxh/Core.fxh"

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

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Iterate(csin input)
{
	
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	uint particleIndex = input.DTID.x;

	ParticleBuffer[particleIndex].age += psTime.y;
	ParticleBuffer[particleIndex].lifespan -= psTime.y;

	/*
	ParticleBuffer[particleIndex].velocity += ParticleBuffer[particleIndex].acceleration * psTime.y;
	ParticleBuffer[particleIndex].position += ParticleBuffer[particleIndex].velocity * psTime.y;
	*/
	
	float3 velOld = ParticleBuffer[particleIndex].velocity;
	ParticleBuffer[particleIndex].velocity += ParticleBuffer[particleIndex].acceleration * psTime.y;	
	ParticleBuffer[particleIndex].position += ( ParticleBuffer[particleIndex].velocity + velOld ) / 2 * psTime.y;
	
	
}

technique11 Iterate { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Iterate() ) );} }
