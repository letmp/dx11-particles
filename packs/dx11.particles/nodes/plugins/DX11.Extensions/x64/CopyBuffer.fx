RWStructuredBuffer<uint> buf1 : BUFFER1;
RWStructuredBuffer<uint> buf2 : BUFFER2;
RWStructuredBuffer<uint> buf3 : BUFFER3;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CSInit(csin input)
{
	buf1[input.DTID.x] = input.DTID.x;
}

[numthreads(1, 1, 1)]
void CSEdit(csin input)
{
	buf2[input.DTID.x] *= 2;
	buf3[input.DTID.x] = buf2[input.DTID.x] * 3;
}

technique11 init { pass P0{SetComputeShader( CompileShader( cs_5_0, CSInit() ) );} }
technique11 edit { pass P0{SetComputeShader( CompileShader( cs_5_0, CSEdit() ) );} }