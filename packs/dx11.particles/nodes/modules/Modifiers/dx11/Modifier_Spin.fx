#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>
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
	
	
	uint cntPos, stride;
	OrbitPositionBuffer.GetDimensions(cntPos,stride);
	int maxCount = cntPos; 
	
	uint cntDir;
	OrbitDirectionBuffer.GetDimensions(cntDir,stride);
	maxCount = max(maxCount, cntDir);
	
	uint cntRad;
	RadiusBuffer.GetDimensions(cntRad,stride);
	maxCount = max(maxCount, cntRad);
	
	uint cntRadStr;
	RadialStrengthBuffer.GetDimensions(cntRadStr,stride);
	maxCount = max(maxCount, cntRadStr);
	
	uint cntGravStr;
	GravityStrengthBuffer.GetDimensions(cntGravStr,stride);
	maxCount = max(maxCount, cntGravStr);

	uint cntDirStr;
	DirectionStrengthBuffer.GetDimensions(cntDirStr,stride);
	maxCount = max(maxCount, cntDirStr);
	
	uint2 bin = GetDynamicBufferBin(input.DTID.x, maxCount);
	
	for (uint i = 0; i < bin.y; i++)
	{
		float3 posOrbit = OrbitPositionBuffer[GetDynamicBufferIndex(i, bin, cntPos)];
		float3 dirOrbit = OrbitDirectionBuffer[GetDynamicBufferIndex(i, bin, cntDir)];
		
		float3 gravityCenter = posOrbit + (dot(position - posOrbit, dirOrbit) / dot(dirOrbit,dirOrbit)) * dirOrbit;
		float3 gravity = gravityCenter - position;
		
		float3 radialForce = normalize(cross( gravity, dirOrbit));
		float3 gravityAttraction = normalize(gravity);
		
		float dist = length(gravity);
		
		float radius = RadiusBuffer[GetDynamicBufferIndex(i, bin, cntRad)];
		
		if( dist < radius){
			float radialStrength = RadialStrengthBuffer[GetDynamicBufferIndex(i, bin, cntRadStr)];
			float gravStrength = GravityStrengthBuffer[GetDynamicBufferIndex(i, bin, cntGravStr)];
			float dirStrength = DirectionStrengthBuffer[GetDynamicBufferIndex(i, bin, cntDirStr)];
			
			ParticleBuffer[particleIndex].force += (radialForce * radialStrength) + (gravity * gravStrength) + (dirOrbit * dirStrength);
		}	
	}
	
}

technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
