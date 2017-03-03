#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

StructuredBuffer<float> SBuffer <string uiname="S Buffer";>;
StructuredBuffer<float> BBuffer <string uiname="B Buffer";>;
StructuredBuffer<float> RBuffer <string uiname="R Buffer";>;
StructuredBuffer<float> SpeedBuffer <string uiname="Speed Buffer";>;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSUpdate(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	float3 position = ParticleBuffer[particleIndex].position;
	
	uint cnt, stride;
	SBuffer.GetDimensions(cnt,stride);
	float S = SBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , cnt)];
	BBuffer.GetDimensions(cnt,stride);
	float B = BBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , cnt)];
	RBuffer.GetDimensions(cnt,stride);
	float R = RBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , cnt)];
	SpeedBuffer.GetDimensions(cnt,stride);
	float Speed = SpeedBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , cnt)];
	
	position.x = position.x + S * (position.y - position.x) * psTime.y * Speed;
	position.y = position.y + ( (R * position.x) - position.y - (position.x * position.z) ) * psTime.y * Speed;
    position.z = position.z + ( (position.x * position.y) - (B * position.z) ) * psTime.y * Speed;
	
	ParticleBuffer[particleIndex].position = position;
}

technique11 LorenzAttractor { pass P0{SetComputeShader( CompileShader( cs_5_0, CSUpdate() ) );} }
