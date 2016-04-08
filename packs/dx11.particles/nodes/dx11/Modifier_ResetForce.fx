#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 force;
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
void CSMain(csin input)
{
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	uint slotIndex = input.DTID.x;
	ParticleBuffer[slotIndex].force = float3(0,0,0);
}

technique11 csmain { pass P0{SetComputeShader( CompileShader( cs_5_0, CSMain() ) );} }
