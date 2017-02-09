#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 force;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> AttrPosition <string uiname="Position Buffer";>;
StructuredBuffer<float> AttrStrength <string uiname="Strength Buffer";>;
StructuredBuffer<float> AttrPower <string uiname="Power Buffer";>;
StructuredBuffer<float> AttrRadius <string uiname="Radius Buffer";>;

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
	
	float3 position = ParticleBuffer[particleIndex].position;
	
	uint cntPos, stride;
	AttrPosition.GetDimensions(cntPos,stride);
	int maxCount = cntPos; 
	
	uint cntRad;
	AttrRadius.GetDimensions(cntRad,stride);
	maxCount = max(maxCount, cntRad);
	
	uint cntStr;
	AttrStrength.GetDimensions(cntStr,stride);
	maxCount = max(maxCount, cntStr);
	
	uint cntPow;
	AttrPower.GetDimensions(cntPow,stride);
	maxCount = max(maxCount, cntPow);
	
	uint2 bin = GetDynamicBufferBin(input.DTID.x, maxCount);
	
	for (uint i = 0; i < bin.y; i++)
	{
		float3 dist = AttrPosition[GetDynamicBufferIndex(i, bin, cntPos)] - position;	
		float radius = AttrRadius[GetDynamicBufferIndex(i, bin, cntRad)];		
		float strength = AttrStrength[GetDynamicBufferIndex(i, bin, cntStr)];
		float power = AttrPower[GetDynamicBufferIndex(i, bin, cntPow)];
		
		float force = length(dist) / radius;
		force = pow(saturate(1 - force), 1 / power);
		
		ParticleBuffer[particleIndex].force += dist * force * strength;
	}
}

technique11 SetAttractor { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
