#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float4 color;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float4> ColorBuffer <string uiname="Color Buffer";>;
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
	ColorBuffer.GetDimensions(size,stride);
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
	
	float4 colorCurrent = ColorBuffer[bin.x + slice];
	float4 colorNext = ColorBuffer[bin.x + slice + 1];
	
	float position = (phase_restricted % (1.0/bin.y)) / (1.0/bin.y);
	
	float4 diff = colorNext - colorCurrent;
	float4 color = colorCurrent + (diff * position);
	
	ParticleBuffer[particleIndex].color = color;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	uint size, stride;
	ColorBuffer.GetDimensions(size,stride);
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
	
	float4 colorCurrent = ColorBuffer[bin.x + slice];
	float4 colorNext = ColorBuffer[bin.x + slice + 1];
	
	float position = (phase_restricted % (1.0/bin.y)) / (1.0/bin.y);
	
	float4 diff = colorNext - colorCurrent;
	float4 color = colorCurrent + (diff * position);

	ParticleBuffer[particleIndex].color += color;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSubtract(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	uint size, stride;
	ColorBuffer.GetDimensions(size,stride);
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
	
	float4 colorCurrent = ColorBuffer[bin.x + slice];
	float4 colorNext = ColorBuffer[bin.x + slice + 1];
	
	float position = (phase_restricted % (1.0/bin.y)) / (1.0/bin.y);
	
	float4 diff = colorNext - colorCurrent;
	float4 color = colorCurrent + (diff * position);

	ParticleBuffer[particleIndex].color -= color;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSMul(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	uint size, stride;
	ColorBuffer.GetDimensions(size,stride);
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
	
	float4 colorCurrent = ColorBuffer[bin.x + slice];
	float4 colorNext = ColorBuffer[bin.x + slice + 1];
	
	float position = (phase_restricted % (1.0/bin.y)) / (1.0/bin.y);
	
	float4 diff = colorNext - colorCurrent;
	float4 color = colorCurrent + (diff * position);

	ParticleBuffer[particleIndex].color *= color;
}


technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
technique11 Subtract { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSubtract() ) );} }
technique11 Mul { pass P0{SetComputeShader( CompileShader( cs_5_0, CSMul() ) );} }