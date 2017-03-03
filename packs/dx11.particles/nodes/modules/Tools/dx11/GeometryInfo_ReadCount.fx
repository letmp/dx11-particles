Texture2D counterTexture <string uiname="Counter Texture";>;

RWStructuredBuffer<uint> RWCounterBuffer : BACKBUFFER;

[numthreads(1,1,1)]
void CS(uint3 tid : SV_DispatchThreadID)
{
	RWCounterBuffer[0] = counterTexture.Load(int3(0,0,0)).r;
}

technique10 Copy
{
	pass P0
	{
		SetComputeShader( CompileShader( cs_5_0, CS() ) );
	}
}




