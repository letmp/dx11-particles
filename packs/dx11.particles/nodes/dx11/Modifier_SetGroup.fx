#include "../fxh/Defines.fxh"
#include "../fxh/Functions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float mass;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;

RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

StructuredBuffer<float> MassBuffer <string uiname="Mass Buffer";>;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSet(csin input)
{
	uint slotIndex = getSlotIndex(	input.DTID.x,
									FlagBuffer,
									SelectionIndexBuffer,
									SelectionCounterBuffer,
									AliveIndexBuffer,
									AliveCounterBuffer);
	if (slotIndex == -1 ) return;
	
	uint size, stride;
	MassBuffer.GetDimensions(size,stride);
	ParticleBuffer[slotIndex].mass = MassBuffer[input.DTID.x % size];
}

technique11 SetMass { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
