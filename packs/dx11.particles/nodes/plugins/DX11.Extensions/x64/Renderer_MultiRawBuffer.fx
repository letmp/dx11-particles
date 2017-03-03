RWByteAddressBuffer fBufOut0 : BACKBUFFER0;
RWByteAddressBuffer fBufOut1 : BACKBUFFER1;

StructuredBuffer<float> fBufIn0;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CS(csin input)
{
	fBufOut0.Store( input.DTID.x * 4 , asuint(fBufIn0[input.DTID.x]) );
	
	if( input.DTID.x % 2 == 0){
		fBufOut1.Store( input.DTID.x * 4 , asuint(fBufIn0[input.DTID.x]) );
	}
	
}
technique11 cst { pass P0{SetComputeShader( CompileShader( cs_5_0, CS() ) );} }