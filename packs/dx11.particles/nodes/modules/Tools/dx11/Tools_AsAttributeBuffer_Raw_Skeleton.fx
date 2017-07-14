#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
ByteAddressBuffer RawBuffer;

#if !defined(BYTESPERELEMENT)
	#define BYTESPERELEMENT 1
#endif

/*STUB_BUFFERS*/
uint elementCount;

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS(uint3 tid : SV_DispatchThreadID)
{
	if (tid.x >= elementCount) return;
	
	uint id = tid.x;
	uint elementOffset = id * BYTESPERELEMENT;
	
	/*STUB_FUNCTIONS*/
}

technique11 WriteBuffers { pass P0 { SetComputeShader( CompileShader( cs_5_0, CS() ) );} }




