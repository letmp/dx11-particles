ByteAddressBuffer RawBuffer;

RWStructuredBuffer<float3> PositionBuffer : POSITIONBUFFER;
RWStructuredBuffer<float4> ColorBuffer : COLORBUFFER;
RWStructuredBuffer<uint> CounterBuffer : COUNTERBUFFER;

uint elementCount;

[numthreads(64,1,1)]
void CS(uint3 tid : SV_DispatchThreadID)
{
	if (tid.x >= elementCount) return;
	
	float x = 0; float y = 0; float z = 0;
	float r = 1; float g = 1; float b = 1; float a = 1;
	
	#if defined(OFFSET_X)
  		x = asfloat(RawBuffer.Load(tid.x * BYTESPERELEMENT + OFFSET_X));
	#endif
	#if defined(OFFSET_Y)
  		y = asfloat(RawBuffer.Load(tid.x * BYTESPERELEMENT + OFFSET_Y));
	#endif
	#if defined(OFFSET_Z)
  		z = asfloat(RawBuffer.Load(tid.x * BYTESPERELEMENT + OFFSET_Z));
	#endif
	
	#if defined(OFFSET_R)
  		r = asfloat(RawBuffer.Load(tid.x * BYTESPERELEMENT + OFFSET_R));
	#endif
	#if defined(OFFSET_G)
  		g = asfloat(RawBuffer.Load(tid.x * BYTESPERELEMENT + OFFSET_G));
	#endif
	#if defined(OFFSET_B)
  		b = asfloat(RawBuffer.Load(tid.x * BYTESPERELEMENT + OFFSET_B));
	#endif
	#if defined(OFFSET_A)
  		a = asfloat(RawBuffer.Load(tid.x * BYTESPERELEMENT + OFFSET_A));
	#endif

	// update counterbuffer
	uint id = CounterBuffer.IncrementCounter();
	
	// write position
	PositionBuffer[id] = float3(x,y,z);
	ColorBuffer[id] = float4(r,g,b,a);

}

technique11 WriteBuffers { pass P0 { SetComputeShader( CompileShader( cs_5_0, CS() ) );} }




