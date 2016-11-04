#include "../fxh/Defines.fxh"

Texture2D texMin;
Texture2D texMax;
SamplerState sPoint : IMMUTABLE
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Border;
    AddressV = Border;
};

RWStructuredBuffer<float3> BoundsBuffer : BACKBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Bounds(csin input)
{
	if (input.DTID.x >= MAXGROUPCOUNT) return;
	
	float pixPos = ((1.0f / MAXGROUPCOUNT) * input.DTID.x) ;
	float halfPixel = (1.0f / MAXGROUPCOUNT) * 0.5f;
	pixPos += halfPixel;
	
	float3 minCoords = texMin.SampleLevel(sPoint, float2(pixPos,0),0).xyz;
	float3 maxCoords = texMax.SampleLevel(sPoint, float2(pixPos,0),0).xyz;
	
	if ( minCoords.x == 99999999 && minCoords.y == 99999999 && minCoords.z == 99999999) minCoords = float3(0,0,0);
	if ( maxCoords.x == -99999999 && maxCoords.y == -99999999 && maxCoords.z == -99999999) maxCoords = float3(0,0,0);
	
	BoundsBuffer[input.DTID.x * 4 + 0] = (minCoords+maxCoords) / 2;
	BoundsBuffer[input.DTID.x * 4 + 1] = maxCoords - minCoords;
	BoundsBuffer[input.DTID.x * 4 + 2] = minCoords;
	BoundsBuffer[input.DTID.x * 4 + 3] = maxCoords;
	
}

technique11 Bounds { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Bounds() ) );} }
