#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_Particles.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions_DynBuffer.fxh>
#include <packs\dx11.particles\nodes\modules\Modifiers\fxh\NoiseSimplex.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
	COMPOSITESTRUCT
	#else
	float3 position;
	float3 force;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

float4x4 tW:WORLD;

StructuredBuffer<float3> NoiseAmountBuffer <String uiname="NoiseAmount Buffer";>;
StructuredBuffer<float> NoiseFreqBuffer <String uiname="NoiseFrequency Buffer";>;
StructuredBuffer<float3> NoiseOffsetBuffer <String uiname="NoiseOffset Buffer";>;
StructuredBuffer<float> NoiseEpsBuffer <String uiname="NoiseEpsilon Buffer";>;

float3 ComputeCurl (float x, float y, float z, float eps)
{
	float n1, n2, a, b;
	float3 curl;
	
	float x1, x2 , y1, y2, z1, z2;
	
	if (eps == 0) eps = 0.00001;

	x1 = snoise(float3(x + eps, y, z));
	x2 = snoise(float3(x - eps, y, z));
	y1 = snoise(float3(x, y + eps, z));
	y2 = snoise(float3(x, y - eps, z));
	z1 = snoise(float3(x, y, z + eps));
	z2 = snoise(float3(x, y, z - eps));
	
	
	a = (y1 - y2) / (2* eps);
	b = (z1 - z2) / (2* eps);
	curl.x = a - b;
	
	a = (z1 - z2) / (2* eps);
	b = (x1 - x2) / (2* eps);
	curl.y = a - b;
	
	a = (x1 - x2) / (2* eps);
	b = (z1 - z1) / (2* eps);
	curl.z = a - b;
	
	return curl;
	
}

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
		
	uint size, stride;
	
	NoiseAmountBuffer.GetDimensions(size,stride);
	float3 noiseAmount = NoiseAmountBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	NoiseOffsetBuffer.GetDimensions(size,stride);
	float3 offset = NoiseAmountBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	NoiseFreqBuffer.GetDimensions(size,stride);
	float freq = NoiseFreqBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	NoiseEpsBuffer.GetDimensions(size,stride);
	float eps = NoiseEpsBuffer[GetDynamicBufferIndex( particleIndex, input.DTID.x , size)];
	
	float3 p = (ParticleBuffer[particleIndex].position+offset)*freq;
	
	float3 curlNoise = mul(float4(ComputeCurl(p.x,p.y,p.z,eps),1),tW).xyz ;
	
	ParticleBuffer[particleIndex].force += (curlNoise * noiseAmount) ;
		
}

technique11 Turbulence { pass P0{SetComputeShader( CompileShader( cs_5_0, CSUpdate() ) );} }


