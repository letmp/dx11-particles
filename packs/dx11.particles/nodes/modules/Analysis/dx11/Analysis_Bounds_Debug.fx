StructuredBuffer<float3> Bounds;

Texture2D inputTexture <string uiname="Texture";>;

SamplerState linearSampler <string uiname="Sampler State";>
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

cbuffer cbPerDraw: register( b0 )
{
	float4x4 tVP : VIEWPROJECTION;
	float4 cAmb <bool color=true;String uiname="Color";> = { 1.0f,1.0f,1.0f,1.0f };
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
};

cbuffer cbTextureData : register(b2)
{
	float4x4 tTex <string uiname="Texture Transform"; bool uvspace=true; >;
};

struct vsInput
{
	float4 pos : POSITION;
	float4 uv: TEXCOORD0;
	uint ii : SV_InstanceID;
};

struct vs2ps
{
    float4 position: SV_POSITION;
	float4 uv: TEXCOORD0;
};

/* ===================== VERTEX SHADER ===================== */

vs2ps VS(vsInput input)
{
    vs2ps output = (vs2ps)0;
    
	uint ii = input.ii;
	
	float3 center = Bounds[ii * 4 + 0].xyz;
	float3 width = Bounds[ii * 4 + 1].xyz;
	
	float3 pos = (input.pos.xyz * width ) + center;
	output.position = mul(float4(pos,1),mul(tW,tVP));
	
	// Set output position to 0 if size was 0 -> gets discarded in PS
	if (width.x == 0 && width.y == 0 && width.z == 0) output.position.w = 0;
	
	output.uv = mul(input.uv, tTex);
	
	return output;
}

/* ===================== PIXEL SHADER ===================== */

float4 PS_COLOR(vs2ps input): SV_Target
{
	if (input.position.w == 0) discard;
	float4 col = inputTexture.Sample(linearSampler,input.uv.xy) * cAmb;
    return col;
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
