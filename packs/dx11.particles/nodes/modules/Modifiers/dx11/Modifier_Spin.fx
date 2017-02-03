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
	
	uint cntPosition, stride;
	OrbitPositionBuffer.GetDimensions(cntPosition,stride);
	uint cntDirection;
	OrbitDirectionBuffer.GetDimensions(cntDirection,stride);
	int spinCount = max(cntPosition,cntDirection);
	uint cnt;
	
	for (int i = 0; i < spinCount; i++){
		float3 posOrbit = OrbitPositionBuffer[i % cntPosition];
		float3 dirOrbit = OrbitDirectionBuffer[i % cntDirection];
		
		float3 gravityCenter = posOrbit + (dot(position - posOrbit, dirOrbit) / dot(dirOrbit,dirOrbit)) * dirOrbit;
		float3 gravity = gravityCenter - position;
		
		float3 radialForce = normalize(cross( gravity, dirOrbit));
		float3 gravityAttraction = normalize(gravity);
		
		float dist = length(gravity);
		
		RadiusBuffer.GetDimensions(cnt,stride);
		float radius = RadiusBuffer[i % cnt];
		
		if( dist < radius){
			
			RadialStrengthBuffer.GetDimensions(cnt,stride);
			float radialStrength = RadialStrengthBuffer[i % cnt];
			
			GravityStrengthBuffer.GetDimensions(cnt,stride);
			float gravStrength = GravityStrengthBuffer[i % cnt];
			
			DirectionStrengthBuffer.GetDimensions(cnt,stride);
			float dirStrength = DirectionStrengthBuffer[i % cnt];
			
			ParticleBuffer[particleIndex].force += (radialForce * radialStrength) + (gravity * gravStrength) + (dirOrbit * dirStrength);
		}
	}
	
	
	
}

technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
