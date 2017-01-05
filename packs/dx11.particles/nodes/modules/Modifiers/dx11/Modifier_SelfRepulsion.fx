#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<LinkedListElement> LinkedListBuffer : LINKEDLISTBUFFER;
RWStructuredBuffer<uint> LinkedListOffsetBuffer : LINKEDLISTOFFSETBUFFER;

int CellCount;
float4x4 tW: WORLD;
//float Gamma;
float Radius <String uiname="Default Radius"; float uimin=0.0;> = 1;
float RepulseAmount;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Set(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	float4 tp = mul(float4(ParticleBuffer[particleIndex].position, 1), tW);

	tp = tp * 0.5f + 0.5f;
	tp.y = 1.0f -tp.y;
	int3 cl = tp.xyz * CellCount;

    uint cellindex = cl.z * CellCount * CellCount + cl.y * CellCount + cl.x;
	if (cellindex > asuint(CellCount * CellCount * CellCount)) return;
	uint next = LinkedListOffsetBuffer[cellindex];
	
	float dist = 0;
	//float g = Gamma/(1.00001-Gamma);
	while (next != -1){
		
		uint particleIndexOther = LinkedListBuffer[next].particleIndex;
		
		if (particleIndexOther != particleIndex){
			
			float minDist = Radius;
			#if defined(KNOW_SCALE)
				minDist = ( length(ParticleBuffer[particleIndex].scale / 2)+ length(ParticleBuffer[particleIndexOther].scale / 2)) / 2 * Radius;
			#endif
			
			dist = distance(ParticleBuffer[particleIndex].position, ParticleBuffer[particleIndexOther].position);
			
			if (dist < minDist){
				float f = saturate(1 - dist * 2);
			  	//f = pow( f, g );
				ParticleBuffer[particleIndex].velocity += (ParticleBuffer[particleIndex].position - ParticleBuffer[particleIndexOther].position) * lerp(0.0f,1.00f,f) * minDist * RepulseAmount;				
			}
		}
		
		if (LinkedListBuffer[next].next != next) next = LinkedListBuffer[next].next;
		else next = -1;
	}
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Set() ) );} }

