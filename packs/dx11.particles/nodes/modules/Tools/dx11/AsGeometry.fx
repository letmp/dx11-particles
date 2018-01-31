#include <packs\dx11.particles\nodes\modules\Core\fxh\AlgebraFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AlivePointerBuffer;

cbuffer cbPerDraw : register( b0 )
{
	float4x4 tV: VIEW;
	float4x4 tWV: WORLDVIEW;
	float4x4 tWVP: WORLDVIEWPROJECTION;
	float4x4 tVP : VIEWPROJECTION;	
	float4x4 tP: PROJECTION;
	float4x4 tWIT: WORLDINVERSETRANSPOSE;
};

cbuffer cbPerObj : register( b1 )
{
	float4x4 tW : WORLD;
	float4 cAmb <bool color=true;String uiname="Default Color";> = { 1.0f,1.0f,1.0f,1.0f };
};

cbuffer cbTextureData : register(b2)
{
	float4x4 tTex <string uiname="Texture Transform"; bool uvspace=true; >;
};

Texture2D inputTexture <string uiname="Texture";>;

SamplerState linearSampler <string uiname="Sampler State";>
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

/* ===================== STRUCTURES ===================== */

struct VSIn
{
	float4 pos : POSITION;
	float3 norm : NORMAL;
	float4 uv: TEXCOORD0;
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSOut
{
    float4 pos: POSITION;
	float3 norm : NORMAL;
	float4 uv: TEXCOORD0;
	uint ii : IID;
};

/* ===================== VERTEX SHADER ===================== */

VSOut VS(VSIn In)
{
    VSOut Out = (VSOut)0;
    
	uint particleIndex = AlivePointerBuffer[In.ii];
	Out.ii = In.ii;
	Out.norm = In.norm;
	float4 p = In.pos;
	
	#if defined(KNOW_SCALE)
		p = mul(p,MatrixScaling(ParticleBuffer[particleIndex].scale));
 	#endif	
	#if defined(KNOW_ROTATION)
		p = mul(p,MatrixRotation(ParticleBuffer[particleIndex].rotation));
 	#endif
	
	p.xyz += ParticleBuffer[particleIndex].position;
	Out.pos = mul(p,mul(tW,tVP));
	
	Out.uv = mul(In.uv, tTex);
	
	return Out;
}

/* ===================== GEOMETRY SHADER ===================== */
[maxvertexcount(3)]
void GS(triangle VSOut input[3], inout TriangleStream<VSOut>GSOut)
{
	VSOut v = (VSOut)0;
	GSOut.RestartStrip();

	for(uint i=0;i<3;i++)
	{
		v=input[i];
		GSOut.Append(v);
	}
	GSOut.RestartStrip();
}


GeometryShader StreamOutGS = ConstructGSWithSO(
	CompileShader( gs_5_0, GS() ),
	"POSITION.xyz; NORMAL.xyz; TEXCOORD.xy; IID.x", NULL, NULL, NULL, -1
);

/* ===================== TECHNIQUE ===================== */

technique11 GeometryOut
{
	pass P0
	{		
		SetVertexShader( CompileShader( vs_5_0, VS() ) );
	    SetGeometryShader( StreamOutGS );
	}
}