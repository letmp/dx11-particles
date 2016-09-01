#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float lifespan;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;

#include "../fxh/IndexFunctions.fxh"

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSet(csin input)
{
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	uint size, stride;
	LifespanBuffer.GetDimensions(size,stride);
	ParticleBuffer[slotIndex].lifespan = LifespanBuffer[input.DTID.x % size];
}

technique11 SetMass { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
