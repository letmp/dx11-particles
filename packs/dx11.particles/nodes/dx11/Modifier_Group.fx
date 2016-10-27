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
RWStructuredBuffer<uint> SelectionGroupBuffer : SELECTIONGROUPBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

RWStructuredBuffer<uint> GroupIndexBuffer : GROUPINDEXBUFFER_/*STUB_GROUPNAME*/;

#include "../fxh/IndexFunctions.fxh"

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSClear(csin input)
{
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	if (FlagBuffer[0] == true){ // there is a selection
		GroupIndexBuffer[input.DTID.x] = -1;
	} // there is no selection -> all particles 
	else GroupIndexBuffer[input.DTID.x] = 0;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSet(csin input)
{
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	
	uint slotIndex = 0;
	uint selectionGroupIndex = 0;
	
	if (FlagBuffer[0] == true){
		if( input.DTID.x >= SelectionCounterBuffer[0]) return;
		slotIndex = SelectionIndexBuffer[input.DTID.x];
		selectionGroupIndex = SelectionGroupBuffer[input.DTID.x];
	}
	else {
		slotIndex = input.DTID.x;
		selectionGroupIndex = 0;
	}
}

technique11 Clear { pass P0{SetComputeShader( CompileShader( cs_5_0, CSClear() ) );} }
technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }