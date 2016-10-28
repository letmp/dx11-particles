#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 acceleration;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> AccelerationBuffer <string uiname="Acceleration Buffer";>;

#include "../fxh/IndexFunctions.fxh"
#include "../fxh/NoiseFunctions.fxh"

//NOISE FORCE:
float3 noiseAmount = float3(1.0f,1.0f,1.0f);
float noiseTime;
int noiseOct;
float noiseFreq = 1;
float noiseLacun = 1.666;
float noisePers = 0.666;

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
	if (particleIndex < 0 ) return;
	
	float3 position = ParticleBuffer[particleIndex].position;
	
	// Noise Force
	float3 noiseForce = float3(
	fBm(float4( position + float3(51,2.36,-5),noiseTime),noiseOct,noiseFreq,noiseLacun,noisePers),
	fBm(float4( position + float3(98.2,-9,-36),noiseTime),noiseOct,noiseFreq,noiseLacun,noisePers),
	fBm(float4( position + float3(0,10.69,6),noiseTime),noiseOct,noiseFreq,noiseLacun,noisePers)
	);
	noiseForce *= noiseAmount;
	
	ParticleBuffer[particleIndex].acceleration += noiseForce;
	
}

technique11 Turbulence { pass P0{SetComputeShader( CompileShader( cs_5_0, CSUpdate() ) );} }
