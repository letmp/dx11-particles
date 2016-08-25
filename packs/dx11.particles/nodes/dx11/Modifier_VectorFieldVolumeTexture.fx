RWTexture3D<float3> RWDistanceVolume : BACKBUFFER;

float3 InvVolumeSize : INVTARGETSIZE;
StructuredBuffer<float3> vectorFields;

[numthreads(16, 16, 4)]
void CS( uint3 i : SV_DispatchThreadID)
{ 
	float3 p = i;
	p *= InvVolumeSize;
	p -= 0.5f;
	
	int index = i.x + (i.x * i.y) + (i.x * i.y * i.z);
	
	RWDistanceVolume[i] = vectorFields[index];
}

technique11 Process
{
	pass P0
	{
		SetComputeShader( CompileShader( cs_5_0, CS() ) );
	}
}







