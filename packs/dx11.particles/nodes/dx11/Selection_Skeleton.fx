#include "../fxh/Defines.fxh"
#include "../fxh/Functions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
  		/*SKELETON_VARS_STRUCT*/
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;

RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

cbuffer name : register(b0){
   /*SKELETON_VARS_CBUF*/
};

/*SKELETON_FUNCTION_DEF*/

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Select(csin input)
{
	
	uint slotIndex = getSlotIndex(	input.DTID.x,
				FlagBuffer,
				SelectionIndexBuffer,
				SelectionCounterBuffer,
				AliveIndexBuffer,
				AliveCounterBuffer);
	if (slotIndex == -1 ) return;
	
	bool isSelected = true /*SKELETON_FUNCTION_CALL*/;
	
		
	if (isSelected){
		uint selectionCounter = SelectionCounterBuffer.IncrementCounter();
		SelectionIndexBuffer[selectionCounter] = slotIndex;
	}
}

technique11 Select { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Select() ) );} }