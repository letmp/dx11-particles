#include "../fxh/Defines.fxh"

RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CS_SetCounter(csin input)
{
	uint emitterCounter = EmitterCounterBuffer.IncrementCounter();
	EmitterCounterBuffer[0] = emitterCounter;
	if (emitterCounter > 0){
		uint aliveCounter = AliveCounterBuffer.IncrementCounter();
		AliveCounterBuffer[0] = AliveCounterBuffer[0] + aliveCounter;
	}
}

technique11 SetCounter { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetCounter() ) );} }