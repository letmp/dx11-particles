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
	uint2 bin = GetDynamicBufferBin(input.DTID.x, size);
	
	float phase = ParticleBuffer[particleIndex].age / (ParticleBuffer[particleIndex].age + ParticleBuffer[particleIndex].lifespan);
	float phase_restricted = (1 - 1.0/bin.y) * phase;
	
	uint slice = (uint) floor(phase_restricted * bin.y);
	
	float3 scaleCurrent = ScaleBuffer[bin.x + slice];
	float3 scaleNext = ScaleBuffer[bin.x + slice + 1];
	
	float position = (phase_restricted % (1.0/bin.y)) / (1.0/bin.y);
	
	float3 diff = scaleNext - scaleCurrent;
	float3 scale = scaleCurrent + (diff * position);
	
	ParticleBuffer[particleIndex].scale = scale;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	uint size, stride;
	ScaleBuffer.GetDimensions(size,stride);
	uint2 bin = GetDynamicBufferBin(input.DTID.x, size);
	
	float phase = ParticleBuffer[particleIndex].age / (ParticleBuffer[particleIndex].age + ParticleBuffer[particleIndex].lifespan);
	float phase_restricted = (1 - 1.0/bin.y) * phase;
	
	uint slice = (uint) floor(phase_restricted * bin.y);
	
	float3 scaleCurrent = ScaleBuffer[bin.x + slice];
	float3 scaleNext = ScaleBuffer[bin.x + slice + 1];
	
	float position = (phase_restricted % (1.0/bin.y)) / (1.0/bin.y);
	
	float3 diff = scaleNext - scaleCurrent;
	float3 scale = scaleCurrent + (diff * position);
	
	ParticleBuffer[particleIndex].scale += scale;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSMul(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	uint size, stride;
	ScaleBuffer.GetDimensions(size,stride);
	uint2 bin = GetDynamicBufferBin(input.DTID.x, size);
	
	float phase = ParticleBuffer[particleIndex].age / (ParticleBuffer[particleIndex].age + ParticleBuffer[particleIndex].lifespan);
	float phase_restricted = (1 - 1.0/bin.y) * phase;
	
	uint slice = (uint) floor(phase_restricted * bin.y);
	
	float3 scaleCurrent = ScaleBuffer[bin.x + slice];
	float3 scaleNext = ScaleBuffer[bin.x + slice + 1];
	
	float position = (phase_restricted % (1.0/bin.y)) / (1.0/bin.y);
	
	float3 diff = scaleNext - scaleCurrent;
	float3 scale = scaleCurrent + (diff * position);
	
	ParticleBuffer[particleIndex].scale *= scale;
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
technique11 Mul { pass P0{SetComputeShader( CompileShader( cs_5_0, CSMul() ) );} }
