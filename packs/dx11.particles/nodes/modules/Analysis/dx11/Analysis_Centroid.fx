#include "../../Core/fxh/Core.fxh"

Texture2D tex;
SamplerState sPoint : IMMUTABLE
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Border;
    AddressV = Border;
};

RWStructuredBuffer<float4> PositionBuffer : BACKBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSCentroid(csin input)
{
	if (input.DTID.x >= MAXGROUPCOUNT) return;
	
	float pixPos = ((1.0f / MAXGROUPCOUNT) * input.DTID.x) ;
	float halfPixel = (1.0f / MAXGROUPCOUNT) * 0.5f;
	pixPos += halfPixel;
	
	float4 accum = tex.SampleLevel(sPoint, float2(pixPos,0),0);
	if (accum.w > 0.0f){
		PositionBuffer[input.DTID.x] = float4(accum.x/accum.w, accum.y/accum.w, accum.z/accum.w, accum.w);
	}
	else PositionBuffer[input.DTID.x] = float4(0,0,0,0);
	
}

technique11 Centroid { pass P0{SetComputeShader( CompileShader( cs_5_0, CSCentroid() ) );} }
