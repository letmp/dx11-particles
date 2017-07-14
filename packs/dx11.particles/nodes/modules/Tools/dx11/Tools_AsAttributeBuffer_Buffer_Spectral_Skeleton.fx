#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
		/*STUB_STRUCT*/
	#endif
};

struct NewParticle {	
	#if defined(NEWCOMPOSITESTRUCT)
  		NEWCOMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AlivePointerBuffer;
StructuredBuffer<uint> AliveCounterBuffer;

AppendStructuredBuffer<NewParticle> NewParticleBuffer : BACKBUFFER;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_WriteAttributes(csin input)
{
	if (input.DTID.x >= MAXPARTICLECOUNT || input.DTID.x >= AliveCounterBuffer[0]) return;
	
	uint particleIndex = AlivePointerBuffer[input.DTID.x];
	
	NewParticle np = (NewParticle) 0;

	/*STUB_NEWPARTICLE*/
	
	NewParticleBuffer.Append(np);
}

technique11 WriteAttributes { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_WriteAttributes() ) );} }