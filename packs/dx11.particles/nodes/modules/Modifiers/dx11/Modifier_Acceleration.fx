#include "../../Core/fxh/Defines.fxh"
#include "../../Core/fxh/IndexFunctions.fxh"
#include "../../Core/fxh/AlgebraFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 acceleration;
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> AccelerationBuffer <string uiname="Acceleration Buffer";>;
bool UseSelectionIndex <String uiname="Use SelectionId";> = 0;

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
	if (particleIndex < 0 ) return;
	
	uint size, stride;
	AccelerationBuffer.GetDimensions(size,stride);
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	
	ParticleBuffer[particleIndex].acceleration = AccelerationBuffer[bufferIndex];	
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAddGlobal(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex < 0 ) return;
	
	uint size, stride;
	AccelerationBuffer.GetDimensions(size,stride);
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	ParticleBuffer[particleIndex].acceleration += AccelerationBuffer[bufferIndex] ;	
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAddLocal(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex < 0 ) return;
	
	uint size, stride;
	AccelerationBuffer.GetDimensions(size,stride);
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	
	float4x4 rotM = MatrixRotation(RotateByVector(ParticleBuffer[particleIndex].velocity));
	float4 locA = float4(AccelerationBuffer[bufferIndex]* float3(-1,1,-1) , 1);	
	float4 direction = mul(locA, rotM);

	ParticleBuffer[particleIndex].acceleration += direction.xyz;
	/*
	float l = length(ParticleBuffer[particleIndex].acceleration);
	float3 a = ParticleBuffer[particleIndex].acceleration + direction.xyz;
	ParticleBuffer[particleIndex].acceleration = 1 * normalize(a);
	*/
	
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
technique11 AddGlobal { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAddGlobal() ) );} }
technique11 AddLocal { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAddLocal() ) );} }