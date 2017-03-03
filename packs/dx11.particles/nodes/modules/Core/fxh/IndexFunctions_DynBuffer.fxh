// =====================================================
//            DYNAMIC BUFFER INDEX FUNCTIONS
// =====================================================

StructuredBuffer<uint> BinSizeBuffer;
bool UseSelectionIndex <String uiname="Use SelectionId";> = 1;

uint binMod(int slice, uint size){	
	if (size == 0) return 0;
	if (slice >= 0){
		if (slice % size == 0 && slice != 0) return size;
		return slice % size;
	} 
	else return ((size + slice) % size) + 1;
}

uint2 GetDynamicBufferBin(uint threadIndex, uint bufferSize){
	if(UseSelectionIndex){
		uint selectionId = 0;
		if(FlagBuffer[0] == true) selectionId = SelectionIndexBuffer[threadIndex];
		
		uint size, stride;
		BinSizeBuffer.GetDimensions(size,stride);

		uint binOffset = 0;
		for ( uint i = 0; i < selectionId; i++){
			binOffset += BinSizeBuffer[i % size];
		}
		uint binSize = BinSizeBuffer[selectionId % size];
		return uint2(binOffset, binSize);
	} else {
		return uint2(0, bufferSize);
		/*uint size, stride;
		BinSizeBuffer.GetDimensions(size,stride);
		uint binId = threadIndex % size;
		uint binSize = binMod(BinSizeBuffer[binId], bufferSize);
		return uint2(0, binSize);*/
	}
}

uint GetDynamicBufferIndex( uint index, uint2 bin, uint bufferSize ){
	return (bin.x + (index % bin.y)) % bufferSize; 
}

uint GetDynamicBufferIndex(uint particleIndex, uint threadIndex, uint bufferSize){
	if(UseSelectionIndex){
		uint2 bin = GetDynamicBufferBin(threadIndex, bufferSize);
		return GetDynamicBufferIndex( particleIndex, bin, bufferSize);
	}
	else{
		return particleIndex % bufferSize;
		/*uint2 bin = GetDynamicBufferBin(0, bufferSize);
		return particleIndex % bin.y;*/
	} 
}