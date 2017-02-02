#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
	#endif
};

StructuredBuffer<Particle> ParticleBufferIn;
StructuredBuffer<uint> AlivePointerBufferIn;
StructuredBuffer<uint> AliveCounterBufferIn;

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_CopyToRW(csin input)
{
	if (input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x > AliveCounterBufferIn[0]) return;
	
	uint particleIndex = AlivePointerBufferIn[input.DTID.x];
	uint newParticleIndex = ParticleBuffer.IncrementCounter();
	
	ParticleBuffer[newParticleIndex] = ParticleBufferIn[particleIndex];
}

[numthreads(1, 1, 1)]
void CS_SetCounter(csin input)
{
	AliveCounterBuffer[0] = AliveCounterBufferIn[0];
}

technique11 CopyToRW { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_CopyToRW() ) );} }
technique11 SetCounter { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetCounter() ) );} }