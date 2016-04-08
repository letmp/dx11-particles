#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float4 color;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;

RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

StructuredBuffer<float4> ColorBuffer <string uiname="Color Buffer";>;


struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSMain(csin input)
{
	uint slotIndex = 0;
	
	if (FlagBuffer[0] == true){ // Apply to selected particles only
		if(input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= SelectionCounterBuffer[0]) return;
		slotIndex = SelectionIndexBuffer[input.DTID.x];
	}
	else { // apply to all (alive) particles
		if(input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0]) return;
		slotIndex = AliveIndexBuffer[input.DTID.x];
	}
	
	uint size, stride;
	ColorBuffer.GetDimensions(size,stride);
	ParticleBuffer[slotIndex].color =  ColorBuffer[input.DTID.x % size];
	
}

technique11 csmain { pass P0{SetComputeShader( CompileShader( cs_5_0, CSMain() ) );} }
