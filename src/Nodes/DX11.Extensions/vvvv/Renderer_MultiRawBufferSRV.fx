
ByteAddressBuffer fBufOut0 : BACKBUFFER0_SRV; // <- Shader Resource View (use that when you only want to read since it is faster)
RWByteAddressBuffer fBufOut1 : BACKBUFFER1; // <- Unordered Access View

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CS(csin input)
{
	float buf0value = asfloat(fBufOut0.Load(input.DTID.x));
	
	float buf1value = asfloat(fBufOut1.Load(input.DTID.x));
	fBufOut1.Store( input.DTID.x , asuint(buf0value + buf1value) );
	
}
technique11 cst { pass P0{SetComputeShader( CompileShader( cs_5_0, CS() ) );} }