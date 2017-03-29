#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 scale;
		float age;
		float lifespan;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> ScaleBuffer <string uiname="Scale Buffer";>;
StructuredBuffer<float> FadeInEndBuffer <string uiname="FadeInEnd Buffer";>;
StructuredBuffer<float> FadeOutStartBuffer <string uiname="FadeOutStart Buffer";>;

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
	ScaleBuffer.GetDimensions(size,stride);
	float3 scale = ScaleBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	FadeInEndBuffer.GetDimensions(size,stride);
	float fadeInEnd = FadeInEndBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	FadeOutStartBuffer.GetDimensions(size,stride);
	float fadeOutStart = FadeOutStartBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	float phase = ParticleBuffer[particleIndex].age / (ParticleBuffer[particleIndex].age + ParticleBuffer[particleIndex].lifespan);
	
	scale *= smoothstep(0, fadeInEnd, phase)* 1-smoothstep(fadeOutStart, 1, phase);
	
	ParticleBuffer[particleIndex].scale = scale;
}



technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }

