// =====================================================
//                  HELPER FUNCTIONS
// =====================================================

uint SetBit(uint input, int bit){
	uint output = (input | (uint)1 << bit);
	return output;
}

bool HasBit(uint input, int bit){
	return (input & ((uint)1 << bit)) != 0;
}