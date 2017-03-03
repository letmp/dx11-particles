#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

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
RWStructuredBuffer<float3> IteratorBuffer : ITERATORBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_IterateSimple(csin input)
{
	
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	uint particleIndex = input.DTID.x;

	ParticleBuffer[particleIndex].age += psTime.y;
	ParticleBuffer[particleIndex].lifespan -= psTime.y;

	float3 acceleration = ParticleBuffer[particleIndex].force;
	#if defined(KNOW_MASS)
		float m = ParticleBuffer[particleIndex].mass;
		if(m > 0) acceleration /= m;
 	#endif
	ParticleBuffer[particleIndex].velocity += acceleration * psTime.y;

	ParticleBuffer[particleIndex].velocity += acceleration * psTime.y;
	ParticleBuffer[particleIndex].position += ParticleBuffer[particleIndex].velocity * psTime.y;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_IterateAverageVelocities(csin input)
{
	
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	uint particleIndex = input.DTID.x;

	ParticleBuffer[particleIndex].age += psTime.y;
	ParticleBuffer[particleIndex].lifespan -= psTime.y;

	float3 velOld = ParticleBuffer[particleIndex].velocity;
	
	float3 acceleration = ParticleBuffer[particleIndex].force;
	#if defined(KNOW_MASS)
		float m = ParticleBuffer[particleIndex].mass;
		if(m > 0) acceleration /= m;
 	#endif
	ParticleBuffer[particleIndex].velocity += acceleration * psTime.y;

	ParticleBuffer[particleIndex].position += ( ParticleBuffer[particleIndex].velocity + velOld ) / 2 * psTime.y;
		
}

float fixedTimeStep;
[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_IterateFixedTimestep(csin input)
{
	
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	uint particleIndex = input.DTID.x;

	ParticleBuffer[particleIndex].age += psTime.y;
	ParticleBuffer[particleIndex].lifespan -= psTime.y;

	float3 acceleration = ParticleBuffer[particleIndex].force;
	#if defined(KNOW_MASS)
		float m = ParticleBuffer[particleIndex].mass;
		if(m > 0) acceleration /= m;
 	#endif

	for ( float te = psTime.z; te > fixedTimeStep ; te -= fixedTimeStep){
		ParticleBuffer[particleIndex].velocity += acceleration * fixedTimeStep;
		ParticleBuffer[particleIndex].position += ParticleBuffer[particleIndex].velocity * fixedTimeStep;
	}
}

technique11 Simple { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_IterateSimple() ) );} }
technique11 AvgVelocities { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_IterateAverageVelocities() ) );} }
technique11 FixedTimestep { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_IterateFixedTimestep() ) );} }
