#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>
#include <packs\dx11.particles\nodes\modules\Modifiers\fxh\NoiseFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 force;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> NoiseAmountBuffer <String uiname="NoiseAmount Buffer";>;
StructuredBuffer<float> NoiseTimeBuffer <String uiname="NoiseTime Buffer";>;
StructuredBuffer<int> NoiseOctBuffer <String uiname="NoiseOct Buffer";>;
StructuredBuffer<float> NoiseFreqBuffer <String uiname="NoiseFrequency Buffer";>;
StructuredBuffer<float> NoiseLacunBuffer <String uiname="NoiseLacunarity Buffer";>;
StructuredBuffer<float> NoisePersBuffer <String uiname="NoisePersistence Buffer";>;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSUpdate(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	float3 position = ParticleBuffer[particleIndex].position;
	
	uint size, stride;
	NoiseAmountBuffer.GetDimensions(size,stride);
	float3 noiseAmount = NoiseAmountBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	NoiseTimeBuffer.GetDimensions(size,stride);
	float noiseTime = NoiseTimeBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	NoiseOctBuffer.GetDimensions(size,stride);
	int noiseOct = NoiseOctBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	NoiseFreqBuffer.GetDimensions(size,stride);
	float noiseFreq = NoiseFreqBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	NoiseLacunBuffer.GetDimensions(size,stride);
	float noiseLacun = NoiseLacunBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	NoisePersBuffer.GetDimensions(size,stride);
	float noisePers = NoisePersBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	// Noise Force
	float3 noiseForce = float3(
	fBm(float4( position + float3(51,2.36,-5),noiseTime),noiseOct,noiseFreq,noiseLacun,noisePers),
	fBm(float4( position + float3(98.2,-9,-36),noiseTime),noiseOct,noiseFreq,noiseLacun,noisePers),
	fBm(float4( position + float3(0,10.69,6),noiseTime),noiseOct,noiseFreq,noiseLacun,noisePers)
	);
	noiseForce *= noiseAmount;
	
	ParticleBuffer[particleIndex].force += noiseForce;
	
}

technique11 Turbulence { pass P0{SetComputeShader( CompileShader( cs_5_0, CSUpdate() ) );} }
