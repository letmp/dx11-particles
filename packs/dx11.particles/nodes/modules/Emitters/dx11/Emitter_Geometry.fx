//@author: antokhio
//@help: emitt particles from geometry
//@tags: 
//@credits: 

#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 force;
		float lifespan;
		float age;
		float4 color;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;
RWStructuredBuffer<uint> AlivePointerBuffer : ALIVEPOINTERBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

ByteAddressBuffer GeometryBuffer;

StructuredBuffer<float3> VelocityBuffer <string uiname="Velocity Buffer";>;
StructuredBuffer<float3> ForceBuffer <string uiname="Force Buffer";>;
StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;

cbuffer cbuf
{
	uint EmitCount = 0;
	uint EmitterSize = 0;
	bool ForceEmission = false;
	bool VelocityFromNormal = false;
	bool ForceFromNormal = false;
	uint OffsetEmission = 0;
	float4x4 tW : WORLD;
	float3 scale <String uiname="Default Scale";> = { 1.0f,1.0f,1.0f };
}


struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_Emit(csin input)
{
	if(input.DTID.x >= EmitterSize) return;
	uint particleIndex = EMITTEROFFSET + input.DTID.x;
	
	float currentLifespan = ParticleBuffer[particleIndex].lifespan;
	
	if ( currentLifespan <= 0.0f || ForceEmission)
	{
		uint emitterCounter = EmitterCounterBuffer.IncrementCounter(); 
		if (emitterCounter >= EmitCount )return;
		
		if (ForceEmission) particleIndex = EMITTEROFFSET + ((emitterCounter + OffsetEmission) % EmitterSize) ;
		
		// update AlivePointerBuffer
		uint aliveIndex = AliveCounterBuffer[0] + AliveCounterBuffer.IncrementCounter();
		AlivePointerBuffer[aliveIndex] = particleIndex;
		
		// create new particle
		
		Particle p = (Particle) 0;
		uint size, stride;
		GeometryBuffer.GetDimensions(size);
		
		p.position = asfloat (GeometryBuffer.Load3((particleIndex % size) * 40));
		p.color    = asfloat (GeometryBuffer.Load4((particleIndex % size) * 40 + 24));
		float3 normalV = asfloat (GeometryBuffer.Load3((particleIndex % size) * 40 + 12));
		
		VelocityBuffer.GetDimensions(size,stride);
		if (VelocityFromNormal)
			p.velocity = VelocityBuffer[emitterCounter % size] * normalV;
		else 
			p.velocity = VelocityBuffer[emitterCounter % size];
		
		ForceBuffer.GetDimensions(size,stride);
		if (ForceFromNormal)
			p.force = ForceBuffer[emitterCounter % size] * normalV;
		else 
			p.force = ForceBuffer[emitterCounter % size];
		
		LifespanBuffer.GetDimensions(size,stride);
		p.lifespan = LifespanBuffer[emitterCounter % size];
		
		#if defined(KNOW_SCALE)
            p.scale = scale;
    	#endif
		
		ParticleBuffer[particleIndex] = p;
		
	}
}


technique11 EmitParticles
{
	pass P0
	{
		SetComputeShader( CompileShader( cs_5_0, CS_Emit() ) );
	}
}




