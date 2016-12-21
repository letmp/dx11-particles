RWTexture3D<float3> RWDistanceVolume : BACKBUFFER;

float3 VolumeSize : TARGETSIZE;
StructuredBuffer<float3> vectorFields;

[numthreads(16, 16, 4)]
void CS( uint3 i : SV_DispatchThreadID)
{	
	int3 vol=round(VolumeSize);
	RWDistanceVolume[i] = vectorFields[ i.x  +  (i.y * vol.x)  +  (i.z * vol.x * vol.y)];
}

technique11 Process
{
	pass P0
	{
		SetComputeShader( CompileShader( cs_5_0, CS() ) );
	}
}