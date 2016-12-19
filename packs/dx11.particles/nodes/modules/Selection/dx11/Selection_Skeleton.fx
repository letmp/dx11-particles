#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
  		/*STUB_STRUCT*/
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> GroupIndexBuffer : GROUP_/*STUB_GROUPNAME*/;

/*STUB_RESOURCE_SEMANTICS*/

/*STUB_CUSTOM_SEMANTICS*/

/*STUB_FUNCTIONS*/

void SetGroupId(uint particleIndex, uint selectionIndex){
	GroupIndexBuffer[particleIndex] = selectionIndex;
}

void SetSelection(uint particleIndex, uint selectionIndex){
	uint selectionCounter = SelectionCounterBuffer.IncrementCounter();
	SelectionPointerBuffer[selectionCounter] = particleIndex;
	SelectionIndexBuffer[selectionCounter] = selectionIndex;
}

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Select(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	//SetGroupId(particleIndex, -1); // resets groupId
	
	uint selectionIndex = 0;
	
	/*STUB_FUNCTIONCALLS*/
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Reset(csin input)
{
	SetGroupId(input.DTID.x, -1); // resets groupId
}

technique11 Select { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Select() ) );} }
technique11 Reset { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Reset() ) );} }