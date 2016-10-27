#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;
RWStructuredBuffer<uint> SelectionGroupBuffer : SELECTIONGROUPBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

StructuredBuffer<float3> VelocityBuffer <string uiname="Velocity Buffer";>;
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
	if (slotIndex == -1 ) return;
	
	uint size, stride;
	VelocityBuffer.GetDimensions(size,stride);
	
	uint bufferIndex = 0;
	if(UseSelectionGroupId)
		bufferIndex = SelectionGroupBuffer[input.DTID.x];
	else bufferIndex = slotIndex % size;
	
	ParticleBuffer[slotIndex].velocity = VelocityBuffer[bufferIndex];
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	uint size, stride;
	VelocityBuffer.GetDimensions(size,stride);
	
	uint bufferIndex = 0;
	if(UseSelectionGroupId)
		bufferIndex = SelectionGroupBuffer[input.DTID.x];
	else bufferIndex = slotIndex % size;
	
	ParticleBuffer[slotIndex].velocity += VelocityBuffer[bufferIndex];
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
