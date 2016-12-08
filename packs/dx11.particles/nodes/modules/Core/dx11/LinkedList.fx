#include "../fxh/Core.fxh"
#include "../fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;
RWStructuredBuffer<LinkedListElement> LinkedListBuffer : LINKEDLISTBUFFER;
RWStructuredBuffer<uint> LinkedListOffsetBuffer : LINKEDLISTOFFSETBUFFER;

float4x4 InverseTransform;
int CellCount;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_ClearLinkedList(csin input)
{
	if (FlagBuffer[1] == true){ return; } // linked list was already generated in this frame - dont clear it
	
	if (input.DTID.x < asuint(CellCount * CellCount * CellCount)){
			LinkedListOffsetBuffer[input.DTID.x] = -1;	
	} 
	
	if (input.DTID.x < MAXPARTICLECOUNT){
		LinkedListElement element;
		element.id = -1;
	    element.next = -1;
	    element.particleIndex = -1;
		LinkedListBuffer[input.DTID.x] = element;
	}
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CS_BuildLinkedList(csin input)
{
	if (FlagBuffer[1] == true){ return; } // linked list was already generated in this frame
	
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;

	float4 tp = mul(float4(ParticleBuffer[particleIndex].position, 1), InverseTransform);

	tp = tp * 0.5f + 0.5f;
	tp.y = 1.0f -tp.y;
	int3 cl = tp.xyz * CellCount;

	uint counter = LinkedListBuffer.IncrementCounter();
    uint cellindex = cl.z * CellCount * CellCount + cl.y * CellCount + cl.x;
	
	uint oldoffset;
	InterlockedExchange(LinkedListOffsetBuffer[cellindex],counter,oldoffset);

	LinkedListElement element;
	element.id = input.DTID.x;
    element.next = oldoffset;
    element.particleIndex = particleIndex;

    LinkedListBuffer[counter] = element;
}

technique11 ClearLinkedList { pass P0 { SetComputeShader( CompileShader( cs_5_0, CS_ClearLinkedList() ) ); } }
technique11 BuildLinkedList { pass P0 { SetComputeShader( CompileShader( cs_5_0, CS_BuildLinkedList() ) ); } }