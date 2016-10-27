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
RWStructuredBuffer<uint> SelectionGroupBuffer : SELECTIONGROUPBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

StructuredBuffer<float3> AccelerationBuffer <string uiname="Acceleration Buffer";>;
bool UseSelectionGroupId <String uiname="Use SelectionGroupId";> = 0;

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
	if (slotIndex < 0 ) return;
	
	uint size, stride;
	AccelerationBuffer.GetDimensions(size,stride);
	
	uint bufferIndex = 0;
	if(UseSelectionGroupId)
		bufferIndex = SelectionGroupBuffer[input.DTID.x];
	else bufferIndex = slotIndex % size;
	
	ParticleBuffer[slotIndex].acceleration = AccelerationBuffer[bufferIndex];	
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex < 0 ) return;
	
	uint size, stride;
	AccelerationBuffer.GetDimensions(size,stride);
	
	uint bufferIndex = 0;
	if(UseSelectionGroupId)
		bufferIndex = SelectionGroupBuffer[input.DTID.x];
	else bufferIndex = slotIndex % size;
	
	ParticleBuffer[slotIndex].acceleration += AccelerationBuffer[bufferIndex];	
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }