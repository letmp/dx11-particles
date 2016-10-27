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
RWStructuredBuffer<uint> SelectionGroupBuffer : SELECTIONGROUPBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

StructuredBuffer<float3> PositionBuffer <string uiname="Position Buffer";>;
bool UseSelectionGroupId <String uiname="Use SelectionGroupId";> = 0;

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
	uint slotIndex = GetSlotIndex( input.DTID.x );
	if (slotIndex == -1 ) return;
	
	float3 p = ParticleBuffer[slotIndex].position;
	float3 v = ParticleBuffer[slotIndex].velocity;
	float3 a = ParticleBuffer[slotIndex].acceleration;
	
	uint cnt,stride;
	PositionBuffer.GetDimensions(cnt,stride);	
	
	uint bufferIndex = 0;
	if(UseSelectionGroupId)
		bufferIndex = SelectionGroupBuffer[input.DTID.x];
	else bufferIndex = slotIndex % cnt;
	
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
	ParticleBuffer[slotIndex].velocity += steer;
}

technique11 SetTarget { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
