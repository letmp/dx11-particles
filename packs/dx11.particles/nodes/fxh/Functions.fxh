#define DEFINES_FXH


// =====================================================
//                  COMMON FUNCTIONS
// =====================================================

int GetSlotIndex(uint pId){
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