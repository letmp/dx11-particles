RWStructuredBuffer<float4> Out : BACKBUFFER;
StructuredBuffer<float2> uv <string uiname="UV Buffer";>;
Texture2D tex <string uiname="Texture";>;

SamplerState s0 <string uiname="Sampler State";>
{Filter=MIN_MAG_MIP_POINT;AddressU=CLAMP;AddressV=CLAMP;};

float2 Resolution;

[numthreads(8, 8, 1)]
void CS( uint3 DTid : SV_DispatchThreadID){
	if (DTid.x >= (uint) Resolution.x || DTid.y >= (uint) Resolution.y) { return; }

	uint w,h,dummy;
	tex.GetDimensions(0,w,h,dummy);
	
	float2 stepSizeXY = float2(w / Resolution.x, h / Resolution.y);	
	float2 coord = DTid.xy * stepSizeXY / float2(w,h);
	float4 color =  tex.SampleLevel(s0, coord,0 );	
	uint index = DTid.x + DTid.y * Resolution.x;
	
	Out[index] = color;
}

technique11 Process{pass P0{SetComputeShader(CompileShader(cs_5_0,CS()));}}







