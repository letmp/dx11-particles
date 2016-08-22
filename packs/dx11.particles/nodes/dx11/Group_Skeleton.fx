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

RWStructuredBuffer<GroupLink> GroupLinkBuffer : GROUPLINKBUFFER_/*STUB_GROUPNAME*/;
RWStructuredBuffer<uint> GroupCounterBuffer : GROUPCOUNTERBUFFER_/*STUB_GROUPNAME*/;

cbuffer name : register(b0){
   /*STUB_VARS_CBUF*/
};

#include "../fxh/Functions.fxh"

/*STUB_FUNCTION_DEF*/

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_ClearGroup(csin input)
{
	if (input.DTID.x > MAXGROUPELEMENTCOUNT) return;
	GroupLink link = { -1, -1 };
	GroupLinkBuffer[input.DTID.x] = link; // resets all links
	
	GroupCounterBuffer[input.DTID.x] = 0;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_SetGroup(csin input)
{
	
	uint slotIndex = getSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	uint groupIndex = 0;
	
	/*STUB_FUNCTION_CALL*/
	
}

technique11 ClearGroup { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_ClearGroup() ) );} }
technique11 SetGroup { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetGroup() ) );} }