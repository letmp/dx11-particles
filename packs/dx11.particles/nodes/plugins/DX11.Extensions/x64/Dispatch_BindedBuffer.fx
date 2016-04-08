RWStructuredBuffer<uint> counter : COUNTER;
RWStructuredBuffer<uint> stuff : STUFF;

int argument;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(1, 1, 1)]
void CSClear(csin input)
{
	counter[input.DTID.x] = 0;
	stuff[input.DTID.x] = 0;
	
	counter[0] = argument;
	counter[1] = argument * 2;
	
}
[numthreads(1, 1, 1)]
void CSCount(csin input)
{
	counter.IncrementCounter();
}
[numthreads(1, 1, 1)]
void CSCheck(csin input)
{
	stuff[input.DTID.x] = 1;
}
technique11 count { pass P0{SetComputeShader( CompileShader( cs_5_0, CSCount() ) );} }
technique11 clear { pass P0{SetComputeShader( CompileShader( cs_5_0, CSClear() ) );} }
technique11 check { pass P0{SetComputeShader( CompileShader( cs_5_0, CSCheck() ) );} }