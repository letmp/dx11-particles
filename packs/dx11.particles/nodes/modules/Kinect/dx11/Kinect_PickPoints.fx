float4x4 tW : WORLD;
RWStructuredBuffer<float3> rwbuffer : BACKBUFFER;
//Texture2D texDepth <string uiname="Texture Depth";>;
//Texture2D texRGBDepth <string uiname="Texture RGBDepth";>;
Texture2D texWorld <string uiname="Texture World";>;
StructuredBuffer<float2> uv <string uiname="UV Buffer";>;
//float2 FOV;
int count;

SamplerState sPoint : IMMUTABLE
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Border;
    AddressV = Border;
};


[numthreads(1, 1, 1)]
void CS( uint3 i : SV_DispatchThreadID)
{ 
	if (i.x >=  (uint) count ) { return;}
	/*float2 coords = texRGBDepth.SampleLevel(sPoint,uv[i.x],0).rg;
	
	float depth =  texDepth.SampleLevel(sPoint,coords,0).r * 65.535 ;
	float XtoZ = tan(FOV.x/2) * 2;
    float YtoZ = tan(FOV.y/2) * 2;
	
	float4 pos = float4(0,0,0,1);
	pos.x = ((uv[i.x].x - 0.5) * depth * XtoZ * -1);
	pos.y = ((0.5 - uv[i.x].y ) * depth * YtoZ);
	pos.z = depth;*/
	float3 pos = texWorld.SampleLevel(sPoint,uv[i.x],0).xyz;
	pos = mul(float4(pos,1), tW).xyz;
	
	rwbuffer[i.x] = pos.xyz;
}

technique11 Process
{
	pass P0
	{
		SetComputeShader( CompileShader( cs_5_0, CS() ) );
	}
}







