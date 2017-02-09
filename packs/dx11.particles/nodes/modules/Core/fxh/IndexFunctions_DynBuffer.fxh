// =====================================================
//            DYNAMIC BUFFER INDEX FUNCTIONS
// =====================================================

StructuredBuffer<uint> BinSizeBuffer;
bool UseSelectionIndex <String uiname="Use SelectionId";> = 1;

uint2 GetDynamicBufferBin(uint threadIndex, uint bufferSize){
	if(UseSelectionIndex){
		uint selectionId = SelectionIndexBuffer[threadIndex];
		
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
	} 
}