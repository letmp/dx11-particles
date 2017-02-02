#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

StructuredBuffer<uint> AlivePointerBuffer;
StructuredBuffer<uint> AliveCounterBuffer;
StructuredBuffer<uint> GroupIndexBuffer;

RWStructuredBuffer<uint> NewAlivePointerBuffer : ALIVEPOINTERBUFFER;
RWStructuredBuffer<uint> NewAliveCounterBuffer : ALIVECOUNTERBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Filter(csin input)
{
	if (input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0] ) return;
	
	uint particleIndex = AlivePointerBuffer[input.DTID.x];
	
	if (GroupIndexBuffer[particleIndex] != -1) {
		uint newAliveIndex = NewAliveCounterBuffer[0] + NewAliveCounterBuffer.IncrementCounter();
		NewAlivePointerBuffer[newAliveIndex] = particleIndex;
	}
}

[numthreads(1, 1, 1)]
void CS_ResetCounter(csin input)
{
	NewAliveCounterBuffer[0] = 0;
}

[numthreads(1, 1, 1)]
void CS_SetCounter(csin input)
{
	NewAliveCounterBuffer[0] = NewAliveCounterBuffer.IncrementCounter();
}

technique11 Filter { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Filter() ) );} }
technique11 Reset { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_ResetCounter() ) );} }
technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetCounter() ) );} }