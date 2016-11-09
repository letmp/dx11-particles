#include "../fxh/Defines.fxh"
#include "../fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		int oneVariableNeeded;
  		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> GroupIndexBuffer : GROUPINDEXBUFFER_mySelection;

cbuffer name : register(b0){
   float4x4  tFilter_37667799_0 : TFILTER_37667799_0;
	float4 test : TEST;
};

bool LocalityInsideTransform(uint particleIndex, float4x4 tFilter) {
	float3 coord = mul(float4(ParticleBuffer[particleIndex].position,1), tFilter).xyz;
	return (!(	coord.x < -0.5 || coord.x > 0.5 || coord.y < -0.5 || coord.y > 0.5 || coord.z < -0.5 || coord.z > 0.5));
 };
bool LocalityOutsideTransform(uint particleIndex, float4x4 tFilter) {
	float3 coord = mul(float4(ParticleBuffer[particleIndex].position,1), tFilter).xyz;
	return ((	coord.x < -0.5 || coord.x > 0.5 || coord.y < -0.5 || coord.y > 0.5 || coord.z < -0.5 || coord.z > 0.5));
 };

void SetGroupId(uint particleIndex, uint selectionIndex){
	GroupIndexBuffer[particleIndex] = selectionIndex;
}

void SetSelection(uint particleIndex, uint selectionIndex){
	uint selectionCounter = SelectionCounterBuffer.IncrementCounter();
	SelectionPointerBuffer[selectionCounter] = particleIndex;
	SelectionIndexBuffer[selectionCounter] = selectionIndex;
}

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Select(csin input)
{
	
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	SetGroupId(particleIndex, -1); // resets groupId
	
	uint selectionIndex = 0;
	
	selectionIndex= 0 ;
	if (LocalityInsideTransform(particleIndex,tFilter_37667799_0)) {
		SetSelection(particleIndex, selectionIndex);  SetGroupId(particleIndex, selectionIndex); return;
	}
	
}

technique11 Select { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Select() ) );} }