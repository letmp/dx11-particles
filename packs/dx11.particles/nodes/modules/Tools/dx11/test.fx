#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
ByteAddressBuffer RawBuffer;

#if !defined(BYTESPERELEMENT)
	#define BYTESPERELEMENT 1
#endif

RWStructuredBuffer<float3> positionBuffer : POSITIONBUFFER;
RWStructuredBuffer<float4> colorBuffer : COLORBUFFER;
RWStructuredBuffer<uint> CounterBuffer : COUNTERBUFFER;
uint elementCount;

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS(uint3 tid : SV_DispatchThreadID)
{
	if (tid.x >= elementCount) return;
	
	uint elementOffset = tid.x * BYTESPERELEMENT;
	uint id = CounterBuffer.IncrementCounter();
	
	positionBuffer[id][0] = asfloat(RawBuffer.Load(elementOffset + 0));
positionBuffer[id][1] = asfloat(RawBuffer.Load(elementOffset + 4));
positionBuffer[id][2] = asfloat(RawBuffer.Load(elementOffset + 8));
colorBuffer[id][0] = asfloat(RawBuffer.Load(elementOffset + 12));
colorBuffer[id][1] = asfloat(RawBuffer.Load(elementOffset + 16));
colorBuffer[id][2] = asfloat(RawBuffer.Load(elementOffset + 20));
colorBuffer[id][3] = asfloat(RawBuffer.Load(elementOffset + 24));
}

technique11 WriteBuffers { pass P0 { SetComputeShader( CompileShader( cs_5_0, CS() ) );} }




