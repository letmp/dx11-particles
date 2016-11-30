#include "../../Core/fxh/Defines.fxh"
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

StructuredBuffer<float3> PositionBuffer <string uiname="Position Buffer";>;
bool UseSelectionIndex <String uiname="Use SelectionId";> = 0;

float MaxSpeed = 10;
float MaxForce = 1;
float LandingRadius = 0.1;

float3 Limit(in float3 v, float3 Maximum)
{
	if (length(v) > length(Maximum)) {
		v = normalize(v) * Maximum;
		return v;
	}	
	else return v;	
}

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
	
	float3 p = ParticleBuffer[particleIndex].position;
	float3 v = ParticleBuffer[particleIndex].velocity;
	float3 a = ParticleBuffer[particleIndex].acceleration;
	
	uint size,stride;
	PositionBuffer.GetDimensions(size,stride);	
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	
	float3 target = PositionBuffer[bufferIndex];
	
	float3 desired = target - p;
	float d = length(desired);
	if (d != 0) desired = normalize(desired);
	
	if (d < LandingRadius && d >= 0.0)
	{
		float map = saturate(d);
		desired *= map;
	}
	//else
	desired *= MaxSpeed;
	
	float3 steer = desired - v;
	steer = Limit(steer,MaxForce);
	ParticleBuffer[particleIndex].velocity += steer;
}

technique11 SetTarget { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
