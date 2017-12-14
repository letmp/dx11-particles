StructuredBuffer<float4> Position;

cbuffer cbPerDraw: register( b0 )
{
	float4x4 tVP : VIEWPROJECTION;
	float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
};

struct vsInput
{
	float4 pos : POSITION;
	uint ii : SV_InstanceID;
};

struct vs2ps
{
    float4 position: SV_POSITION;
	bool dscrd : DISCARD;
};

/* ===================== VERTEX SHADER ===================== */

vs2ps VS(vsInput input)
{
    vs2ps output = (vs2ps)0;
    
	uint ii = input.ii;
	
	float4 p = input.pos;
	p.xyz += Position[ii].xyz;
	output.position = mul(p,mul(tW,tVP));
	if (Position[ii].w == 0) output.dscrd = true;
	return output;
}

/* ===================== PIXEL SHADER ===================== */

float4 PS_COLOR(vs2ps input): SV_Target
{
	if(input.dscrd) discard;
    return (cAmb);
}


/* ===================== TECHNIQUE ===================== */
technique10 Color
{
	pass P0
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_COLOR() ) );
	}
}
