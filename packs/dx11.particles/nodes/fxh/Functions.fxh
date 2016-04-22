#define DEFINES_FXH

int getSlotIndex(uint pId, RWStructuredBuffer<bool> FlagBuffer, RWStructuredBuffer<uint> SelectionIndexBuffer, RWStructuredBuffer<uint> SelectionCounterBuffer, RWStructuredBuffer<uint> AliveIndexBuffer, RWStructuredBuffer<uint> AliveCounterBuffer){
	uint slotIndex = 0;
	
	if (FlagBuffer[0] == true){ // Apply to selected particles only
		if(pId >= MAXPARTICLECOUNT || pId >= SelectionCounterBuffer[0]) return -1;
		slotIndex = SelectionIndexBuffer[pId];
	}
	else { // apply to all (alive) particles
		if(pId >= MAXPARTICLECOUNT || pId >= AliveCounterBuffer[0]) return -1;
		slotIndex = AliveIndexBuffer[pId];
	}
	return slotIndex;
}

