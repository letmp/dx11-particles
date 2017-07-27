ByteAddressBuffer RawBuffer;

SamplerState linearSampler : IMMUTABLE
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};
 
cbuffer cbPerDraw : register( b0 )
{
	float4x4 tVP : LAYERVIEWPROJECTION;	
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
	float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
};

struct VS_IN
{
	float4 PosO : POSITION;
	uint iv : SV_VertexID;
};

struct vs2ps
{
    float4 PosWVP: SV_Position;
	float4 Color: COLOR0;
};

vs2ps VS(VS_IN input)
{
    vs2ps output;
	
	uint index = input.iv;
	uint elementOffset = index * 16;
	
	float r = asfloat(RawBuffer.Load(elementOffset + 0));
	float g = asfloat(RawBuffer.Load(elementOffset + 4));
	float b = asfloat(RawBuffer.Load(elementOffset + 8));
	float a = asfloat(RawBuffer.Load(elementOffset + 12));
	
	output.Color = float4(r,g,b,a);
	
	float4 pos = input.PosO;
	pos.y = pos.y * -1;
    output.PosWVP  = mul(pos ,mul(tW,tVP));
    
    return output;
}

float4 PS(vs2ps In): SV_Target
{
    return In.Color;
}

technique10 Constant
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}




