#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

#ifndef GROUPSIZE
	#define GROUPSIZE 64
#endif

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AlivePointerBuffer;
StructuredBuffer<uint> AliveCounterBuffer;

/*STUB_RWBUFFERS*/
RWStructuredBuffer<uint> IndexBuffer : INDEXBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};
uint threadCount;
[numthreads(GROUPSIZE, 1, 1)]
void CS_WriteAttributes(csin input)
{
	if (input.DTID.x >= threadCount) return; 
	
	uint particleIndex = AlivePointerBuffer[input.DTID.x];
	uint bufferIndex = IndexBuffer.IncrementCounter();
	
	IndexBuffer[input.DTID.x] = particleIndex;
	
	/*STUB_RWBUFFERUPDATES*/
}

technique11 WriteAttributes { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_WriteAttributes() ) );} }