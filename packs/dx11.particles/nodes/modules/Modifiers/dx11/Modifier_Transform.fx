#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
StructuredBuffer<float4x4> TransformBuffer <string uiname="Transform Buffer";>;
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
	TransformBuffer.GetDimensions(size,stride);
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	
	ParticleBuffer[particleIndex].position = mul(float4(ParticleBuffer[particleIndex].position,1),TransformBuffer[bufferIndex]).xyz;
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
