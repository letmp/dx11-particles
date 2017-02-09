#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float3> PositionBuffer <string uiname="Position Buffer";>;
StructuredBuffer<float> AmountBuffer <string uiname="Amount Buffer";>;
StructuredBuffer<float> MaxForceBuffer <string uiname="MaxForce Buffer";>;
StructuredBuffer<float> LandingRadiusBuffer <string uiname="LandingRadius Buffer";>;

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
	
	uint size,stride;
	PositionBuffer.GetDimensions(size,stride);
	float3 target = PositionBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	LandingRadiusBuffer.GetDimensions(size,stride);
	float landingRadius = LandingRadiusBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	AmountBuffer.GetDimensions(size,stride);
	float amount = AmountBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	MaxForceBuffer.GetDimensions(size,stride);
	float maxForce = MaxForceBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	float3 desired = target - p;
	float d = length(desired);
	if (d != 0) desired = normalize(desired);
	
	if (d < landingRadius && d >= 0.0)
	{
		float map = saturate(d);
		desired *= map;
	}
	//else
	desired *= amount;
	
	float3 steer = desired - v;
	steer = Limit(steer,maxForce);
	ParticleBuffer[particleIndex].velocity += steer;
}

technique11 SetTarget { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
