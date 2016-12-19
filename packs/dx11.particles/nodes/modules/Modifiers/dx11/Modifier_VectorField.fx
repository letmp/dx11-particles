#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 acceleration;
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

float4x4 tW : WORLD;
float3 fieldPower = 1;
Texture3D VolumeTexture3D;
SamplerState volumeSampler
{
	Filter  = MIN_MAG_MIP_LINEAR;
	AddressU = Border;
	AddressV = Border;
	AddressW = Border;
};

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSetVelocity(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;	
	float4 p = mul(float4(ParticleBuffer[particleIndex].position,1), tW);
	float4 force =  VolumeTexture3D.SampleLevel(volumeSampler,((p.xyz) + 0.5 ),0) * float4(fieldPower,1);
	ParticleBuffer[particleIndex].velocity = force.xyz;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAddVelocity(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;	
	float4 p = mul(float4(ParticleBuffer[particleIndex].position,1), tW);
	float4 force =  VolumeTexture3D.SampleLevel(volumeSampler,((p.xyz) + 0.5 ),0) * float4(fieldPower,1);
	ParticleBuffer[particleIndex].velocity += force.xyz;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSetAcceleration(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;	
	float4 p = mul(float4(ParticleBuffer[particleIndex].position,1), tW);
	float4 force =  VolumeTexture3D.SampleLevel(volumeSampler,((p.xyz) + 0.5 ),0) * float4(fieldPower,1);
	ParticleBuffer[particleIndex].acceleration = force.xyz;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAddAcceleration(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;	
	float4 p = mul(float4(ParticleBuffer[particleIndex].position,1), tW);
	float4 force =  VolumeTexture3D.SampleLevel(volumeSampler,((p.xyz) + 0.5 ),0) * float4(fieldPower,1);
	ParticleBuffer[particleIndex].acceleration += force.xyz;
}

technique11 SetVelocity { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSetVelocity() ) );} }
technique11 AddVelocity { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAddVelocity() ) );} }
technique11 SetAcceleration { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSetAcceleration() ) );} }
technique11 AddAcceleration { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAddAcceleration() ) );} }