#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
		/*STUB_STRUCT*/
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

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_WriteAttributes(csin input)
{
	if (input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0]) return;
	
	uint particleIndex = AlivePointerBuffer[input.DTID.x];
	uint bufferIndex = IndexBuffer.IncrementCounter();
	
	IndexBuffer[input.DTID.x] = particleIndex;
	
	/*STUB_RWBUFFERUPDATES*/
}

technique11 WriteAttributes { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_WriteAttributes() ) );} }