#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 acceleration;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

StructuredBuffer<float3> AccelerationBuffer <string uiname="Acceleration Buffer";>;

int UpdateMode;

#include "../fxh/IndexFunctions.fxh"

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSUpdate(csin input)
{
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex < 0 ) return;
	
	uint size, stride;
	AccelerationBuffer.GetDimensions(size,stride);
	
	float3 acceleration = AccelerationBuffer[slotIndex % size];
	if (UpdateMode == 0)
			ParticleBuffer[slotIndex].acceleration = acceleration;
	else if (UpdateMode == 1)
			ParticleBuffer[slotIndex].acceleration += acceleration;

	
	
}

technique11 Acceleration { pass P0{SetComputeShader( CompileShader( cs_5_0, CSUpdate() ) );} }
