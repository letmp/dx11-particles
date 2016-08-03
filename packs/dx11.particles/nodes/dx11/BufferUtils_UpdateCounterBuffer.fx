#include "../fxh/Defines.fxh"

RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CS_UpdateEmitterAliveBuffer(csin input)
{
	uint emitterCounter = EmitterCounterBuffer.IncrementCounter();
	EmitterCounterBuffer[0] = emitterCounter;
	if (emitterCounter > 0){
		uint aliveCounter = AliveCounterBuffer.IncrementCounter();
		AliveCounterBuffer[0] = AliveCounterBuffer[0] + aliveCounter;
	}
}

[numthreads(1, 1, 1)]
void CS_UpdateSelectionBuffer(csin input)
{
	uint selectionCounter = SelectionCounterBuffer.IncrementCounter();
	SelectionCounterBuffer[0] = selectionCounter;
}

technique11 EmitterAlive { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_UpdateEmitterAliveBuffer() ) );} }
technique11 Selection { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_UpdateSelectionBuffer() ) );} }