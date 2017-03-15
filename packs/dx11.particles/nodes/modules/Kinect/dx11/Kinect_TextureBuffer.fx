//@author: tmp
//@help: samples a texture and draws it to a given render target array slice
//@tags: queue
//@credits: vux

Texture2D texture2d <string uiname="Texture";>;
int slice <string uiname="Slice Index";>;

SamplerState pSamp : IMMUTABLE
{
	Filter = MIN_MAG_MIP_POINT;
	AddressU = Wrap;
	AddressV = Wrap;
};

cbuffer cbControls : register( b0 )
{
	float4x4 tVP : VIEWPROJECTION;
	float4x4 tTex <string uiname="Texture Transform"; bool uvspace=true; >;
};

// ================= STRUCTS =================
struct vsin
{
	float4 Pos : POSITION;
	float2 TexCd : TEXCOORD0;
};

struct vs2gs
{
	float4 Pos: SV_POSITION;
	float2 TexCd: TEXCOORD0;
};

struct gs2ps
{
	float4 Pos   	: SV_POSITION;
	float2 TexCd	: TEXCOORD0;
	uint   ArrayIndex 	: SV_RenderTargetArrayIndex;
};

// ================= VS,GS, PS =================

vs2gs VS(vsin input)
{
	vs2gs Out = (vs2gs)0;
	Out.Pos   = input.Pos;
	Out.TexCd = input.TexCd;
	
	return Out;
}

[maxvertexcount(3)]
void GS(triangle vs2gs input[3], inout TriangleStream<gs2ps> triOutputStream)
{
	gs2ps output;
	for(int i=0; i < 3; ++i)
	{
		output.Pos    = input[i].Pos;
		output.TexCd  = input[i].TexCd.xy;
		output.ArrayIndex  = slice;
		triOutputStream.Append(output);
	}
}

float4 PS(gs2ps input) : SV_Target
{
	float4 col = texture2d.Sample( pSamp, input.TexCd.xy );
	return col;
}

// ================= TECHNIQUE =================

technique11 BufferImages
{
	pass P0
	{
		SetVertexShader		( CompileShader( vs_5_0, VS() ) );
		SetGeometryShader	( CompileShader( gs_5_0, GS() ) );
		SetPixelShader		( CompileShader( ps_5_0, PS() ) );
	}
}




