// =====================================================
//           PARTICLEBUFFER INDEX FUNCTIONS
// =====================================================

RWStructuredBuffer<uint> AlivePointerBuffer : ALIVEPOINTERBUFFER;
RWStructuredBuffer<uint> AliveCounterBuffer : ALIVECOUNTERBUFFER;
RWStructuredBuffer<uint> SelectionPointerBuffer : SELECTIONPOINTERBUFFER;
RWStructuredBuffer<uint> SelectionCounterBuffer : SELECTIONCOUNTERBUFFER;
RWStructuredBuffer<bool> FlagBuffer : FLAGBUFFER;
RWStructuredBuffer<uint> SelectionIndexBuffer : SELECTIONINDEXBUFFER;

uint GetParticleIndex(uint threadIndex){
	uint particleIndex = -1;
	
	if (FlagBuffer[0] == true){ // Apply to selected particles only
		if(threadIndex >= MAXPARTICLECOUNT || threadIndex >= SelectionCounterBuffer[0]) return -1;
		particleIndex = SelectionPointerBuffer[threadIndex];
	}
	else { // apply to all (alive) particles
		if(threadIndex >= MAXPARTICLECOUNT || threadIndex >= AliveCounterBuffer[0]) return -1;
		particleIndex = AlivePointerBuffer[threadIndex];
	}
	return particleIndex;
}