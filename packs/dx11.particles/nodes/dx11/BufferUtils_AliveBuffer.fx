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
RWStructuredBuffer<uint> AliveSwapBuffer : ALIVESWAPBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

bool HasSelection = false;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_CopyToSwap(csin input)
{
	if(input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0]) return;
	
	uint slotIndex = AliveIndexBuffer[input.DTID.x];

	if(ParticleBuffer[slotIndex].lifespan >= 0){
		uint particleCounter = AliveCounterBuffer.IncrementCounter();
		AliveSwapBuffer[particleCounter] = slotIndex;
	}
	
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_CopyFromSwap(csin input)
{
	if(input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0]) return;
	AliveIndexBuffer[input.DTID.x] = AliveSwapBuffer[input.DTID.x];
}

[numthreads(1, 1, 1)]
void CS_SetCounter(csin input)
{
	uint particleCounter = AliveCounterBuffer.IncrementCounter();
	AliveCounterBuffer[0] = particleCounter;
}

technique11 CopyToSwap { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_CopyToSwap() ) );} }
technique11 CopyFromSwap { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_CopyFromSwap() ) );} }
technique11 SetCounter { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetCounter() ) );} }
