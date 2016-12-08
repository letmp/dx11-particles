#include "../../Core/fxh/Core.fxh"
#include "../../Core/fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 acceleration;
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
	
	uint cnt, stride;
	AttrPosition.GetDimensions(cnt,stride);
	int attrCount = cnt; 
	
	for (int i = 0; i < attrCount; i++)
	{
		float3 dist = AttrPosition[i] - position;	
		
		AttrRadius.GetDimensions(cnt,stride);
		float radius = AttrRadius[i % cnt];
		
		AttrStrength.GetDimensions(cnt,stride);
		float strength = AttrStrength[i % cnt];
		
		AttrPower.GetDimensions(cnt,stride);
		float power = AttrPower[i % cnt];
		
		float force = length(dist) / radius;
		force = pow(saturate(1 - force), power);
		
		ParticleBuffer[particleIndex].acceleration += dist * force * strength;
	}
}

technique11 SetAttractor { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
