//@author: antokhio
//@help: template for standard shaders
//@tags: template
//@credits: 

//StructuredBuffer<float4> color <bool color=true;>;

Texture2D tex0;
SamplerState s0:IMMUTABLE {Filter=MIN_MAG_MIP_LINEAR;AddressU=CLAMP;AddressV=CLAMP;};

float4x4 tW : WORLD;
float4x4 tVP : VIEWPROJECTION;

struct VS_IN
{
	float4 pos : POSITION;
	float4 color : COLOR;
};

struct VS_OUT
{
    float4 pos: SV_POSITION;
    float4 color: COLOR;
};

VS_OUT VS(VS_IN input)
{
    VS_OUT output = (VS_OUT)0;
    output.pos  = mul(input.pos,mul(tW,tVP));
    output.color = input.color;
    return output;
}

float4 PS(VS_OUT input): SV_Target
{
    return input.color;
}

technique10 Template
{
	pass P0 <string topology="pointlist";>
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}




