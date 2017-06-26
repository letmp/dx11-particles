Texture2D texRGB <string uiname="RGB";>;
Texture2D texRGBDepth <string uiname="RGBDepth";>;
Texture2D texWorld <string uiname="World";>;
float maxDistance = 0.1;

SamplerState sPoint : IMMUTABLE
{
    Filter = MIN_MAG_MIP_POINT;
    AddressU = Border;
    AddressV = Border;
};

struct vsIn
{
    float4 pos : POSITION;
	float4 uv: TEXCOORD0;
};

struct psIn
{
    float4 pos : SV_Position;
	float2 uv: TEXCOORD0;
};

cbuffer cbPerDraw : register(b0)
{
	float4x4 tVP : LAYERVIEWPROJECTION;
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
	float Alpha <float uimin=0.0; float uimax=1.0;> = 1; 
	float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
	float4x4 tColor <string uiname="Color Transform";>;
};

psIn VS(vsIn input)
{
	psIn output;
	
	float4 position = float4( texWorld.SampleLevel(sPoint,input.uv.xy,0).xyz *  float3(-1,1,1), 1);
	
	output.pos = mul(position,mul(tW,tVP));
	
	float2 texUvColor = texRGBDepth.SampleLevel(sPoint,input.uv.xy,0).rg;
	output.uv = texUvColor;
	
	return output;
}

[maxvertexcount(3)]
void GS(triangle psIn input[3], inout TriangleStream<psIn> gsout)
{
	psIn elem = (psIn)0;
	
	// check if the distance between all 3 vertices of the triangle is 
	// below the given threshold
	if( distance(input[0].pos.xyz,input[1].pos.xyz) < maxDistance &&
		distance(input[0].pos.xyz,input[2].pos.xyz) < maxDistance && 
		distance(input[1].pos.xyz,input[2].pos.xyz) < maxDistance &&
		input[0].pos.w != 0 && input[1].pos.w != 0 && input[2].pos.w != 0
		){
			for(int i = 0; i < 3;i++){
				if(input[i].pos.z != 0) gsout.Append(input[i]);
			}
	}
	gsout.RestartStrip();
	
}

float4 PS(psIn input): SV_Target
{
	if( input.uv.x <= 0 || input.uv.x > 1 || input.uv.y <= 0 || input.uv.y > 1) discard;
    return texRGB.Sample(sPoint, input.uv.xy) * cAmb;
}

technique11 Constant
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( CompileShader( gs_4_0, GS() ) );
        SetPixelShader( CompileShader( ps_4_0, PS() ) );

	}
}
