#include "../fxh/Defines.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

#include "../fxh/IndexFunctions.fxh"

float4x4 tW: WORLD;
float4x4 Rotation;
float BounceMultiplicator = 1.0f;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Inside(csin input)
{
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	float3 pointCoord = mul(float4(ParticleBuffer[slotIndex].position,1), tW).xyz;
	if(	(pointCoord.x < -0.5 || pointCoord.x > 0.5 ||
		pointCoord.y < -0.5 || pointCoord.y > 0.5 ||
		pointCoord.z < -0.5 || pointCoord.z > 0.5
	)){
		if( (pointCoord.x < -0.5 || pointCoord.x > 0.5)) ParticleBuffer[slotIndex].velocity *= float3(-BounceMultiplicator,BounceMultiplicator,BounceMultiplicator);
		if( (pointCoord.y < -0.5 || pointCoord.y > 0.5)) ParticleBuffer[slotIndex].velocity *= float3(BounceMultiplicator,-BounceMultiplicator,BounceMultiplicator);
		if( (pointCoord.z < -0.5 || pointCoord.z > 0.5)) ParticleBuffer[slotIndex].velocity *= float3(BounceMultiplicator,BounceMultiplicator,-BounceMultiplicator);
		ParticleBuffer[slotIndex].velocity = mul(float4(ParticleBuffer[slotIndex].velocity,1), Rotation).xyz;
	}	
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Outside(csin input)
{
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	float3 pointCoord = mul(float4(ParticleBuffer[slotIndex].position,1), tW).xyz;
	if(	!(pointCoord.x < -0.5 || pointCoord.x > 0.5 ||
		pointCoord.y < -0.5 || pointCoord.y > 0.5 ||
		pointCoord.z < -0.5 || pointCoord.z > 0.5
	)){
		if( !(pointCoord.x < -0.5 || pointCoord.x > 0.5)) ParticleBuffer[slotIndex].velocity *= float3(-BounceMultiplicator,BounceMultiplicator,BounceMultiplicator);
		if( !(pointCoord.y < -0.5 || pointCoord.y > 0.5)) ParticleBuffer[slotIndex].velocity *= float3(BounceMultiplicator,-BounceMultiplicator,BounceMultiplicator);
		if( !(pointCoord.z < -0.5 || pointCoord.z > 0.5)) ParticleBuffer[slotIndex].velocity *= float3(BounceMultiplicator,BounceMultiplicator,-BounceMultiplicator);
		ParticleBuffer[slotIndex].velocity = mul(float4(ParticleBuffer[slotIndex].velocity,1), Rotation).xyz;
	}	
}

technique11 Inside { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Inside() ) );} }
technique11 Outside { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Outside() ) );} }
