#include "../fxh/Defines.fxh"
#include "../fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
  		/*STUB_STRUCT*/
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

/*STUB_RESOURCE_SEMANTICS*/

cbuffer name : register(b0){
   /*STUB_CUSTOM_SEMANTICS*/
};

/*STUB_FUNCTIONS*/

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Set(csin input)
{
	
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	/*STUB_FUNCTIONCALLS*/
	
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Set() ) );} }