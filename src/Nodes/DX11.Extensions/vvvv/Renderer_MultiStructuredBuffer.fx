RWStructuredBuffer<uint> fBufCounter : COUNTERBUFFER;
RWStructuredBuffer<float> fBufOut0 : BACKBUFFER0;
RWStructuredBuffer<float> fBufOut1 : BACKBUFFER1;

StructuredBuffer<float> fBufIn0;
bool apply;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CS(csin input)
{
	if(apply){
		fBufOut0[input.DTID.x] = fBufIn0[input.DTID.x];
		fBufOut1[input.DTID.x] = fBufIn0[input.DTID.x] * 2;
		
		if (fBufIn0[input.DTID.x] > 0){
			fBufCounter.IncrementCounter();
		}	
	}
	
}
[numthreads(1, 1, 1)]
void CS_Final(csin input)
{
	if (apply){
		fBufCounter[0] = fBufCounter.IncrementCounter();
	}	
}

technique11 csmain { pass P0{SetComputeShader( CompileShader( cs_5_0, CS() ) );} }
technique11 csfinal { pass P0{SetComputeShader( CompileShader( cs_5_0, CS_Final() ) );} }