#include "../fxh/Core.fxh"

/*
The flagbuffer holds all flags that are needed for the particle ecosystem.
Flags currently used:
0 -> SelectionFlag ( false = modifiers iterate over all particles / true => modifiers iterate over SelectionPointerBuffer)
1 -> LinkedListFlag ( true if a linked list is already generated in one frame)
*/

RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;
bool HasSelection = false;
bool HasLinkedList = false;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CS_SetAllFlags(csin input)
{
	if(input.DTID.x > 0) return;
	FlagBuffer[0] = HasSelection;
	FlagBuffer[1] = HasLinkedList;
}

[numthreads(1, 1, 1)]
void CS_SetSelectionFlag(csin input)
{
	if(input.DTID.x > 0) return;
	FlagBuffer[0] = HasSelection;
	
}

[numthreads(1, 1, 1)]
void CS_SetLinkedListFlag(csin input)
{
	if(input.DTID.x > 0) return;
	FlagBuffer[1] = HasLinkedList;
	
}

technique11 AllFlags { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetAllFlags() ) );} }
technique11 SelectionFlag { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetSelectionFlag() ) );} }
technique11 LinkedListFlag { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_SetLinkedListFlag() ) );} }


