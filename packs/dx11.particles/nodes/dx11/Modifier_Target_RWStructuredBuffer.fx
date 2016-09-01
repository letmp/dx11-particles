#include "../fxh/Defines.fxh"

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
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

cbuffer name : register(b0){
   float2 fRegion : FREGION;
};

float MaxSpeed = 10;
float MaxForce = 1;
float LandingRadius = 0.1;

#include "../fxh/IndexFunctions.fxh"

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
	float slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	if(! (slotIndex > fRegion[0]) && slotIndex < fRegion[1]){
		float3 p = ParticleBuffer[slotIndex].position;
		float3 v = ParticleBuffer[slotIndex].velocity;
		float3 a = ParticleBuffer[slotIndex].acceleration;
		
		uint targetSlotIndex = (slotIndex % (fRegion[0] - fRegion[1])) + fRegion[0];
		float3 target = ParticleBuffer[targetSlotIndex].position;
	
		float3 desired = target - p;
		
		float d = length(desired);
		desired = normalize(desired);
		
		if (d < LandingRadius)
		{
			float map = saturate(d);
			desired *= map;
		}
		//else
		desired *= MaxSpeed;
		
		float3 steer = desired - v;
		steer = Limit(steer,MaxForce);
		ParticleBuffer[slotIndex].velocity += steer;
	}
}

technique11 SetTarget { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
