#include "../../Core/fxh/Core.fxh"
#include "../../Core/fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 acceleration;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

float4x4 tW: WORLD;
float4x4 Rotation;
float BounceMultiplicator = 1.0f;
int mode; // 0 = inside, 1 = outside

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Bounce(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	float3 pointCoord = mul(float4(ParticleBuffer[particleIndex].position,1), tW).xyz;
	
	bool conditionX = pointCoord.x < -0.5 || pointCoord.x > 0.5;
	bool conditionY = pointCoord.y < -0.5 || pointCoord.y > 0.5;
	bool conditionZ = pointCoord.z < -0.5 || pointCoord.z > 0.5;
	bool condition = conditionX || conditionY || conditionZ;
	if(mode ==1) { conditionX = !conditionX; conditionY = !conditionY; conditionZ = !conditionZ; condition = !condition; }
	
	if(	condition){
		if( conditionX){
			ParticleBuffer[particleIndex].velocity.x *= -BounceMultiplicator;
			ParticleBuffer[particleIndex].acceleration.x *= 0;
		} 
		
		if( conditionY){
			ParticleBuffer[particleIndex].velocity.y *= -BounceMultiplicator;
			ParticleBuffer[particleIndex].acceleration.y *= 0;
		} 
		
		if( conditionZ){
			ParticleBuffer[particleIndex].velocity.z *= -BounceMultiplicator;
			ParticleBuffer[particleIndex].acceleration.z *= 0;
		}		
		ParticleBuffer[particleIndex].velocity = mul(float4(ParticleBuffer[particleIndex].velocity,1), Rotation).xyz;
		ParticleBuffer[particleIndex].position += ParticleBuffer[particleIndex].velocity * psTime.y;
	}	
}

technique11 Bounce { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Bounce() ) );} }
