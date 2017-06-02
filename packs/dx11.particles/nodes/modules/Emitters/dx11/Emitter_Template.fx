/* The following include is mandatory! */
#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

/* Here comes the struct definition of the particle. You have to register
   all attributes in the else-clause you want to use in the emitter code.*/
struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float3 force;
		float lifespan;
		float age;
	#endif
};
/* These Buffers are mandatory! They are needed for the lifecycle management
   and for the particle data itself. */
RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;
RWStructuredBuffer<uint> AlivePointerBuffer : ALIVEPOINTERBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

/* These buffers feed the data to the particle. */
StructuredBuffer<float3> PositionBuffer <string uiname="Position Buffer";>;
StructuredBuffer<float3> VelocityBuffer <string uiname="Velocity Buffer";>;
StructuredBuffer<float3> ForceBuffer <string uiname="Force Buffer";>;
StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;

cbuffer cbuf
{
    uint EmitCount = 0;
	uint EmitterSize = 0;
	bool ForceEmission = false;
	uint OffsetEmission = 0;
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
	
	// safeguard
	if(input.DTID.x >= EmitterSize) return;
	
	/* Every emitter is registered to the particle system and gets his own region
	   in the particle-buffer. EMITTEROFFSET is fed to this emitter via the
	   define inputpin (like many other constants)*/
	uint particleIndex = EMITTEROFFSET + input.DTID.x; 
	
	/* Now comes the emission part. There are two modes where particles can be
	   put:
	   1) in slots where dead particles lie ( so lifespan <= 0) 
	   2) or in a ringbuffer style where "living" could be killed (ForceEmission == 1)*/
	float currentLifespan = ParticleBuffer[particleIndex].lifespan;
	if ( currentLifespan <= 0.0f || ForceEmission){
		
		// Now we found a slot where a new particle can be placed
		// => the index of the slot = particleIndex 
		
		// The emittercounter can be incremented because we can emit a particle
		uint emitterCounter = EmitterCounterBuffer.IncrementCounter(); 
		// we can stop shader-execution if we already emitted the desired amount of particles
		if (emitterCounter >= EmitCount )return;
		
		/* When the emitter should behave like a ringbuffer, we have to change
		   the particleIndex. The offset is calculated outside the shader and 
		   submitted via the OffsetEmission pin. So here is the calculation of the
		   particleIndex that behaves like a ringbuffer:
		*/
		if (ForceEmission) particleIndex = EMITTEROFFSET + ((emitterCounter + OffsetEmission) % EmitterSize) ;
		
		/* Now we have to update a buffer that stores the indexes of all
		   living particles: the ALIVEPOINTERBUFFER
		   This buffer is used to access particles very fast (e.g. via Modifier or Selector) */
		
		/* The counter stores the number of currently living particles and we increment
		   the counter (because we emit a new particle). This counter provides the index to the
		   ALIVEPOINTERBUFFER*/
		uint aliveIndex = AliveCounterBuffer[0] + AliveCounterBuffer.IncrementCounter();
		// And now we put the particleIndex there
		AlivePointerBuffer[aliveIndex] = particleIndex;
		
		
		/* Ok.. Now we have an index in the PARTICLEBUFFER
		   and registered this index in the ALIVEPOINTERBUFFER.
		   We can go on and write all the attributes to the new particle and
		   then store the particle.*/
		
		/* Since a particle can have many attributes and we don't want to write
		   code for all of them, we initialize them all with value 0
		*/
		Particle p = (Particle) 0;
		
		/* Get all the data of the incoming buffers to the particle.
		   The index to the dynamicbuffers comes from the emittercounter */
		uint size, stride;
		PositionBuffer.GetDimensions(size,stride);
		p.position = PositionBuffer[emitterCounter % size];
		
		VelocityBuffer.GetDimensions(size,stride);
		p.velocity = VelocityBuffer[emitterCounter % size];
		
		ForceBuffer.GetDimensions(size,stride);
		p.force = ForceBuffer[emitterCounter % size];

		LifespanBuffer.GetDimensions(size,stride);
		p.lifespan = LifespanBuffer[emitterCounter % size];
		
		/* There are attributes that we don't want to initialize
		   with 0 by default. For example color and scale. The particlesystem
		   doesn't have scale and color attributes by default. They are added when
		   there is a color or scale modifier present. The notification about
		   their presence is handled with defines like KNOW_SCALE or KNOW_COLOR */
		#if defined(KNOW_SCALE)
            p.scale = float3(1,1,1);
    	#endif
		#if defined(KNOW_COLOR)
            p.color = float4(1,1,1,1);
    	#endif
		
		/* Last but not least we add the particle to the buffer. */
		ParticleBuffer[particleIndex] = p;
		
	}
}

technique11 EmitParticles { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Emit() ) );} }