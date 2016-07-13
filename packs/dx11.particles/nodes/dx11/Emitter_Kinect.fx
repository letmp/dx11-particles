#include "../fxh/Defines.fxh"

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

// ParticleSystem Buffers
RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<uint> EmitterCounterBuffer : EMITTERCOUNTERBUFFER;
RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

// Inputs
StructuredBuffer<float> LifespanBuffer <string uiname="Lifespan Buffer";>;
Texture2D texRGB <string uiname="RGB";>;
Texture2D texDepth <string uiname="Depth";>;
Texture2D texRayTable <string uiname="RayTable";>;
Texture2D texRGBDepth <string uiname="RGBDepth";>;

// CBuffer
cbuffer cbuf
{
	uint EmitCount = 0;
	uint EmitterSize = 0;
	float4x4 tW : WORLD;
	float2 FOV;
	float2 Resolution;
	bool useRayTable;
	bool useRawData;
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
	
	// safeguard -> skip all pixels that are outside the texture
	if (texId.x >= (uint) Resolution.x || texId.y >= (uint) Resolution.y) { return; }
	
	uint w,h, dummy;
	texDepth.GetDimensions(0,w,h,dummy);
	
	// calculate sampling coordinates
	float2 texUvDepth = texId * float2(w / Resolution.x, h / Resolution.y) / float2(w,h);
	
	// get depth of that pixel
	float depth =  texDepth.SampleLevel(sPoint, texUvDepth,0 ).r * 65.535 ;
	// get texture coordinate for sampling the rgb texture
	float2 texUvColor = texRGBDepth.SampleLevel(sPoint,texUvDepth,0).rg;
	if(useRawData){
		texUvColor.x /= 1920.0f;
		texUvColor.y /= 1080.0f;
	}
	
	// set particle if depth-value and texUvColor coords are valid
	if (depth > 0 && !(texUvColor.x < 0 || texUvColor.x > 1 || texUvColor.y < 0 || texUvColor.y > 1)){

		// INCREMENT EmitterCounter
		uint emitterCounter = EmitterCounterBuffer.IncrementCounter(); 
		if (emitterCounter >= EmitCount )return; // safeguard 
		
		// SET slotIndex
		//uint slotIndex = EMITTEROFFSET + (input.DTID.x + input.DTID.x * input.DTID.y); // for 4,4,1 threads
		uint slotIndex = EMITTEROFFSET + input.DTID.x;
		
		// INIT NEW PARTICLE
		Particle p = (Particle) 0;
		
		// SET POSITION
		float4 position = float4(0,0,0,1);
		if (useRayTable){
			float2 raytable =  texRayTable.SampleLevel(sPoint,texUvDepth,0).xy;
			position.x = depth * raytable.x * -1;
			position.y = depth * raytable.y;
			position.z = depth;
		}
		else{
			float XtoZ = tan(FOV.x/2) * 2;
		    float YtoZ = tan(FOV.y/2) * 2;
			position.x = ((texUvDepth.x - 0.5) * depth * XtoZ * -1);
			position.y = ((0.5 - texUvDepth.y) * depth * YtoZ);		
			position.z = depth;
		}
		position = mul(position, tW);
		p.position = position.xyz;
		
		// SET COLOR
		p.color = texRGB.SampleLevel(sPoint,texUvColor,0);
		
		uint size, stride;
		LifespanBuffer.GetDimensions(size,stride);
		p.lifespan = 0.5 / psTime.x; // ensure that each particle lives only 1 frame
		
		// ADD PARTICLE TO PARTICLEBUFFER
		ParticleBuffer[slotIndex] = p;
		
		// UPDATE ALIVEINDEXBUFFER
		uint aliveIndex = AliveCounterBuffer[0] + AliveCounterBuffer.IncrementCounter();
		AliveIndexBuffer[aliveIndex] = slotIndex;	
	}
	
	
		

}

technique11 EmitParticles
{
	pass P0
	{
		SetComputeShader( CompileShader( cs_5_0, CS_Emit() ) );
	}
}