#include "../fxh/Defines.fxh"
#include "../fxh/Functions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

RWStructuredBuffer<uint> AliveIndexBuffer : ALIVEINDEXBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;

RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;

RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;

float4x4 tFilter : WORLD;
int drawIndex : DRAWINDEX;

RWStructuredBuffer<GroupLink> GroupLinkBuffer : GROUPLINKBUFFER_/*STUB_GROUPNAME*/;
RWStructuredBuffer<uint> GroupCounterBuffer : GROUPCOUNTERBUFFER_/*STUB_GROUPNAME*/;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSClearGroup(csin input)
{
	if (input.DTID.x > MAXGROUPELEMENTCOUNT) return;
	GroupLink link = { -1, -1 };
	GroupLinkBuffer[input.DTID.x] = link; // resets all links
	
	GroupCounterBuffer[input.DTID.x] = 0;
}

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSetGroup(csin input)
{
	uint slotIndex = getSlotIndex(	input.DTID.x,
									FlagBuffer,
									SelectionIndexBuffer,
									SelectionCounterBuffer,
									AliveIndexBuffer,
									AliveCounterBuffer);
	if (slotIndex == -1 ) return;
	
	if (input.DTID.x == 0){
		GroupCounterBuffer.IncrementCounter();
	}

	float3 pointCoord = mul(float4(ParticleBuffer[slotIndex].position,1), tFilter).xyz;
	if( !(	pointCoord.x < -0.5 || pointCoord.x > 0.5 ||
			pointCoord.y < -0.5 || pointCoord.y > 0.5 ||
			pointCoord.z < -0.5 || pointCoord.z > 0.5))
	{
		GroupLink link;
		link.groupId = drawIndex;
		link.particleId = slotIndex;
		
		GroupLinkBuffer[slotIndex + (drawIndex * MAXPARTICLECOUNT)] = link;
		
		uint oldval;
		InterlockedAdd(GroupCounterBuffer[drawIndex],1,oldval);
	}
}

technique11 ClearGroup { pass P0{SetComputeShader( CompileShader( cs_5_0, CSClearGroup() ) );} }
technique11 SetGroup { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSetGroup() ) );} }
