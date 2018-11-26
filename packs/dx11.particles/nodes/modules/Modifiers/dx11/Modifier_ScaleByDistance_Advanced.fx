#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 scale;
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> ScaleBuffer <string uiname="Scale Buffer";>;
StructuredBuffer<float3> PositionBuffer <string uiname="Position Buffer";>;
StructuredBuffer<float> RadiusBuffer <string uiname="Radius Buffer";>;

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
	
	uint sizePos, sizeRadius;
	PositionBuffer.GetDimensions(sizePos,stride);
	RadiusBuffer.GetDimensions(sizeRadius,stride);
	
	float phase_restricted = 1;
	for (uint i = 0; i < sizePos; ++i){
		float phase = saturate(length(ParticleBuffer[particleIndex].position - PositionBuffer[i]) / RadiusBuffer[i % sizeRadius]);
		float phase_restricted_tmp = (1 - 1.0/bin.y) * phase;
		if (phase_restricted_tmp < phase_restricted) phase_restricted = phase_restricted_tmp;
	}
		
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
	
	uint sizePos, sizeRadius;
	PositionBuffer.GetDimensions(sizePos,stride);
	RadiusBuffer.GetDimensions(sizeRadius,stride);
	
	float phase_restricted = 1;
	for (uint i = 0; i < sizePos; ++i){
		float phase = saturate(length(ParticleBuffer[particleIndex].position - PositionBuffer[i]) / RadiusBuffer[i % sizeRadius]);
		float phase_restricted_tmp = (1 - 1.0/bin.y) * phase;
		if (phase_restricted_tmp < phase_restricted) phase_restricted = phase_restricted_tmp;
	}
		
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
	
	uint sizePos, sizeRadius;
	PositionBuffer.GetDimensions(sizePos,stride);
	RadiusBuffer.GetDimensions(sizeRadius,stride);
	
	float phase_restricted = 1;
	for (uint i = 0; i < sizePos; ++i){
		float phase = saturate(length(ParticleBuffer[particleIndex].position - PositionBuffer[i]) / RadiusBuffer[i % sizeRadius]);
		float phase_restricted_tmp = (1 - 1.0/bin.y) * phase;
		if (phase_restricted_tmp < phase_restricted) phase_restricted = phase_restricted_tmp;
	}
		
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
