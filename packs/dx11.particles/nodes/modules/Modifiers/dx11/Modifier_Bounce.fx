#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 force;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

float4x4 tW: WORLD;
float4x4 tWI: WORLDINVERSE;
//float4x4 Rotation;
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
	
	float4 tPos = mul(float4(ParticleBuffer[particleIndex].position,1), tWI); // tranfrom pos to box space
	float3 absTPos = abs(tPos).xyz; 
	bool conditionX = absTPos.x >= 0.5;
	bool conditionY = absTPos.y >= 0.5;
	bool conditionZ = absTPos.z >= 0.5;
	bool condition = conditionX || conditionY || conditionZ;
	
	if(mode ==1) { conditionX = !conditionX; conditionY = !conditionY; conditionZ = !conditionZ; condition = !condition; }
	
	//Outside
	if(	condition & mode == 1)
	{
		float4 tVel = mul(float4(ParticleBuffer[particleIndex].velocity,0), tWI); // tranfrom vel to box space

		float nearWall = max(max(absTPos.x, absTPos.y), absTPos.z); // the axis we want to bounce off
		if (absTPos.x == nearWall) 
		{
			tPos.x = sign(tPos.x)*.5;
			tVel.x *= -BounceMultiplicator;
		}
		else if (absTPos.y == nearWall)
		{
			tPos.y = sign(tPos.y)*.5;
			tVel.y *= -BounceMultiplicator;
		}
		else if (absTPos.z == nearWall)
		{
			tPos.z = sign(tPos.z)*.5;
			tVel.z *= -BounceMultiplicator;
		} 

		ParticleBuffer[particleIndex].velocity = mul(tVel, tW).xyz; // tranfrom vel back to world space
		ParticleBuffer[particleIndex].position = mul(tPos, tW).xyz; // tranfrom pos back to world space
	}	
	
	//Inside
	else if(condition)
	{
		float4 tVel = mul(float4(ParticleBuffer[particleIndex].velocity,0), tWI); // tranfrom vel to box space
		
		if( conditionX)
		{
			tPos.x = clamp(tPos.x, -.5, .5);
			tVel.x *= -BounceMultiplicator;
		} 
		
		if( conditionY)
		{
			tPos.y = clamp(tPos.y, -.5, .5);
			tVel.y *= -BounceMultiplicator;
		} 
		
		if( conditionZ)
		{
			tPos.z = clamp(tPos.z, -.5, .5);
			tVel.z *= -BounceMultiplicator;
		} 	
		ParticleBuffer[particleIndex].velocity = mul(tVel, tW).xyz; // tranfrom vel back to world space
		ParticleBuffer[particleIndex].position = mul(tPos, tW).xyz; // tranfrom pos back to world space
	}	
}

technique11 Bounce { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Bounce() ) );} }
