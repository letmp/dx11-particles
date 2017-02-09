#include <packs/dx11.particles/nodes/modules/Core/fxh/Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>
#include <packs/dx11.particles/nodes/modules/Core/fxh/TextureFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 velocity;
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

float4x4 tVP;
Texture2D tex;

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
	ParticleBuffer[particleIndex].velocity = GetRGB(tex, ParticleBuffer[particleIndex].position, tVP);
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	ParticleBuffer[particleIndex].velocity += GetRGB(tex, ParticleBuffer[particleIndex].position, tVP);
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }
technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
