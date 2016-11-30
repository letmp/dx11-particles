#include "../../Core/fxh/Defines.fxh"
#include "../../Core/fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

//NOISE FORCE:
float S = 10;
float B = 2.666;
float R = 28;
float Speed = 0.1;
struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSUpdate(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex < 0 ) return;
	
	float3 position = ParticleBuffer[particleIndex].position;
	
	position.x = position.x + S * (position.y - position.x) * psTime.y * Speed;
	position.y = position.y + ( (R * position.x) - position.y - (position.x * position.z) ) * psTime.y * Speed;
    position.z = position.z + ( (position.x * position.y) - (B * position.z) ) * psTime.y * Speed;
	
	ParticleBuffer[particleIndex].position = position;
}

technique11 LorenzAttractor { pass P0{SetComputeShader( CompileShader( cs_5_0, CSUpdate() ) );} }
