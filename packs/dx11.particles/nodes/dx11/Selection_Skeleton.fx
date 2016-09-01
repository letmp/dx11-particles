#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
  		/*STUB_VARS_STRUCT*/
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

cbuffer name : register(b0){
   /*STUB_VARS_CBUF*/
};

#include "../fxh/IndexFunctions.fxh"

/*STUB_FUNCTION_DEF*/

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Select(csin input)
{
	
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	bool isSelected = true;
	/*STUB_FUNCTION_CALL*/
	
		
	if (isSelected){
		uint selectionCounter = SelectionCounterBuffer.IncrementCounter();
		SelectionIndexBuffer[selectionCounter] = slotIndex;
	}
}

technique11 Select { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Select() ) );} }