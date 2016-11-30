#include "../../Core/fxh/Defines.fxh"

StructuredBuffer<uint> AlivePointerBuffer;
StructuredBuffer<uint> AliveCounterBuffer;
StructuredBuffer<uint> GroupIndexBuffer;

RWStructuredBuffer<uint> CounterBuffer : BACKBUFFER;

int groupCount = 0;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CS_Clear(csin input)
{
	CounterBuffer[input.DTID.x] = 0;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Count(csin input)
{
	if (input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0] ) return;
	
	uint particleIndex = AlivePointerBuffer[input.DTID.x];
	uint groupId = GroupIndexBuffer[particleIndex];
	
	if ( groupId != -1) {
		uint oldval;
		InterlockedAdd(CounterBuffer[groupId],1,oldval);		
	}
	
}

technique11 Clear { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Clear() ) );} }
technique11 Count { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Count() ) );} }
