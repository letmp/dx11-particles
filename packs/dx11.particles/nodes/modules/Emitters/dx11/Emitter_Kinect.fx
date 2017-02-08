#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float4 color;
		float lifespan;
		float age;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;
RWStructuredBuffer<uint> AlivePointerBuffer : ALIVEPOINTERBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

Texture2D texRGB <string uiname="RGB";>;
Texture2D texRGBDepth <string uiname="RGBDepth";>;
Texture2D texWorld <string uiname="World";>;

cbuffer cbuf
{
	uint EmitCount = 0;
	uint EmitterSize = 0;
	float4x4 tW : WORLD;
	bool useRawData;
	float2 Resolution;
}

SamplerState sPoint : IMMUTABLE
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Border;
    AddressV = Border;
};

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
	
	// get XY pixel id 
	//uint2 texId = uint2(input.DTID.x, input.DTID.y); // for 4,4,1 threads
	uint2 texId = int2(input.DTID.x % Resolution.x ,input.DTID.x / Resolution.x);
	
	uint w,h, dummy;
	texWorld.GetDimensions(0,w,h,dummy);
	
	// calculate sampling coordinates
	float2 texUv = texId * float2(w / Resolution.x , h / Resolution.y) / float2(w,h);
	float halfPixel = (1.0f / Resolution.x) * 0.5f;
	texUv += halfPixel;
	
	// get texture coordinate for sampling the rgb texture
	float2 texUvColor = texRGBDepth.SampleLevel(sPoint,texUv,0).rg;
	if(useRawData){
		texUvColor.x /= 1920.0f;
		texUvColor.y /= 1080.0f;
	}
	
	float3 position =texWorld.SampleLevel(sPoint,texUv,0).xyz;
	
	// set particle if depth-value and texUvColor coords are valid
	if ( all(position) && !(texUvColor.x < 0 || texUvColor.x > 1 || texUvColor.y < 0 || texUvColor.y > 1)){

		// INCREMENT EmitterCounter
		uint emitterCounter = EmitterCounterBuffer.IncrementCounter(); 
		if (emitterCounter >= EmitCount )return; // safeguard 
		
		// SET particleIndex
		//uint particleIndex = EMITTEROFFSET + (input.DTID.x + input.DTID.x * input.DTID.y); // for 4,4,1 threads
		uint particleIndex = EMITTEROFFSET + input.DTID.x;
		
		// INIT NEW PARTICLE
		Particle p = (Particle) 0;
		
		// SET POSITION
		p.position = mul(float4(position,1),tW).xyz;
		
		// SET COLOR
		p.color = texRGB.SampleLevel(sPoint,texUvColor,0);
		
		p.lifespan = psTime.y / 2; // ensure that each particle lives only 1 frame
				
		// ADD PARTICLE TO PARTICLEBUFFER
		ParticleBuffer[particleIndex] = p;
		
		// UPDATE ALIVEPOINTERBUFFER
		uint aliveIndex = AliveCounterBuffer[0] + AliveCounterBuffer.IncrementCounter();
		AlivePointerBuffer[aliveIndex] = particleIndex;
	}
	
}

technique11 EmitParticles
{
	pass P0
	{
		SetComputeShader( CompileShader( cs_5_0, CS_Emit() ) );
	}
}