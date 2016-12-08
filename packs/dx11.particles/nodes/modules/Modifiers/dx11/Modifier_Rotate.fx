#include "../../Core/fxh/Core.fxh"
#include "../../Core/fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 rotation;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> RotationBuffer <string uiname="Rotation Buffer";>;
bool UseSelectionIndex <String uiname="Use SelectionId";> = 0;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSet(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	uint size, stride;
	RotationBuffer.GetDimensions(size,stride);
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	
	ParticleBuffer[particleIndex].rotation = RotationBuffer[bufferIndex];
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	uint size, stride;
	RotationBuffer.GetDimensions(size,stride);
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	
	ParticleBuffer[particleIndex].rotation += RotationBuffer[bufferIndex];
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
