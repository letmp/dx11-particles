#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> TestBuffer : TESTBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};
float x;

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSMain(csin input)
{
	//if(input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0]) return;
	//uint slotIndex = AliveIndexBuffer[input.DTID.x];
	if(input.DTID.x >= MAXPARTICLECOUNT) return;
	uint slotIndex = input.DTID.x;
	
	if (ParticleBuffer[slotIndex].position.x > x){
		TestBuffer.IncrementCounter();
	}
	
}

[numthreads(1, 1, 1)]
void CSSet(csin input)
{
	uint cnt = TestBuffer.IncrementCounter();
	TestBuffer[0] = cnt;	
}


technique11 csmain { pass P0{SetComputeShader( CompileShader( cs_5_0, CSMain() ) );} }
technique11 set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }