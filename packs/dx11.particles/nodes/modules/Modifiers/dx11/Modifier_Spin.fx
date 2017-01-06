#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\AlgebraFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 force;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> OrbitPositionBuffer <string uiname="Orbit Position Buffer";>;
StructuredBuffer<float3> OrbitDirectionBuffer <string uiname="Orbit Direction Buffer";>;

StructuredBuffer<float> RadiusBuffer <string uiname="Radius Buffer";>;

StructuredBuffer<float> RadialStrengthBuffer <string uiname="Radial Strength Buffer";>;
StructuredBuffer<float> GravityStrengthBuffer <string uiname="Gravity Strength Buffer";>;
StructuredBuffer<float> DirectionStrengthBuffer <string uiname="Direction Strength Buffer";>;


bool UseSelectionIndex <String uiname="Use SelectionId";> = 0;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	float3 position = ParticleBuffer[particleIndex].position;
	
	uint size, stride;
	
	OrbitPositionBuffer.GetDimensions(size,stride);
	uint bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	float3 posOrbit = OrbitPositionBuffer[bufferIndex];
	
	OrbitDirectionBuffer.GetDimensions(size,stride);
	bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	float3 dirOrbit = OrbitDirectionBuffer[bufferIndex];
	
	float3 gravityCenter = posOrbit + (dot(position - posOrbit, dirOrbit) / dot(dirOrbit,dirOrbit)) * dirOrbit;
	float3 gravity = gravityCenter - position;
	
	float3 radialForce = normalize(cross( gravity, dirOrbit));
	float3 gravityAttraction = normalize(gravity);
	
	float dist = length(gravity);
	
	RadiusBuffer.GetDimensions(size,stride);
	bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
	float radius = RadiusBuffer[bufferIndex];
	
	if( dist < radius){
		
		RadialStrengthBuffer.GetDimensions(size,stride);
		bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
		float radialStrength = RadialStrengthBuffer[bufferIndex];
		
		GravityStrengthBuffer.GetDimensions(size,stride);
		bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
		float gravStrength = GravityStrengthBuffer[bufferIndex];
		
		DirectionStrengthBuffer.GetDimensions(size,stride);
		bufferIndex = GetDynamicBufferIndex( particleIndex, input.DTID.x , size, UseSelectionIndex);
		float dirStrength = DirectionStrengthBuffer[bufferIndex];
		
		ParticleBuffer[particleIndex].force = (radialForce * radialStrength) + (gravity * gravStrength) + (dirOrbit * dirStrength);
	}
	
}

technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
