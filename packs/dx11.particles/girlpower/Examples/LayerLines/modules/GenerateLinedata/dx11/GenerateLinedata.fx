#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

StructuredBuffer<float3> Positions;
StructuredBuffer<float2> TexMinMaxDiff;
StructuredBuffer<float2> TexXY;
StructuredBuffer<uint> LinearizedIds;

RWStructuredBuffer<uint> IndexBuffer : BACKBUFFER;
RWStructuredBuffer<float3> FromBuffer : FROMBUFFER;
RWStructuredBuffer<float3> ToBuffer : TOBUFFER;

int2 Resolution;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_ClearIds(csin input)
{
	IndexBuffer[input.DTID.x] = 0;
}

[numthreads(1, 1, 1)]
void CS_LinearizeIds(csin input)
{
	uint id = input.DTID.x;

	int minX = TexMinMaxDiff[0].x;
	int minY = TexMinMaxDiff[0].y;
	int sizeX = TexMinMaxDiff[2].x;	
	uint linearId = (Resolution.x * TexXY[id].y) + TexXY[id].x;	
	IndexBuffer[linearId] = id + 1;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_ClearFromTo(csin input)
{
	FromBuffer[input.DTID.x] = float3(0,0,0);
	ToBuffer[input.DTID.x] = float3(0,0,0);
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_SetFromTo(csin input)
{
	//safeguard to prevent lines across several rows
	if ((input.DTID.x + 1) % asuint(Resolution.x) == 0) return;
	
	uint linearizedId0 = LinearizedIds[input.DTID.x];
	uint linearizedId1 = LinearizedIds[input.DTID.x + 1];
	
	//check if this slot and next slot holds a particle
	if (linearizedId0 > 0 && linearizedId1 > 0){
		uint id_out = FromBuffer.IncrementCounter();
		FromBuffer[id_out] = Positions[linearizedId0 - 1];
		ToBuffer[id_out] = Positions[linearizedId1 - 1];
	}
	
}

technique11 ClearIds { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_ClearIds() ) );} }
technique11 LinearizeIds { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_LinearizeIds() ) );} }
technique11 ClearFromTo { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_ClearFromTo() ) );} }
technique11 SetFromTo { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetFromTo() ) );} }
