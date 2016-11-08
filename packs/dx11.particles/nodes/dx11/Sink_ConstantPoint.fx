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
    float4x4 tVP : VIEWPROJECTION;
};

cbuffer cbPerObj : register( b1 )
{
    float4 c <bool color=true;> = 1;
};

struct VSIn
{
	uint iv : SV_VertexID;
	uint ii : SV_InstanceID;
};

struct VSOut
{
    float4 pos : SV_POSITION;
	float2 uv : TEXCOORD0;
	uint particleIndex : VID;
};

VSOut VS(VSIn In)
{
    VSOut Out = (VSOut)0;
	
	uint particleIndex = AlivePointerBuffer[In.iv];
	Out.particleIndex = particleIndex;
	
	float3 p = ParticleBuffer[particleIndex].position;
    Out.pos = mul(float4(p, 1), tVP);

    return Out;
}

float4 PS(VSOut In): SV_Target
{
	float4 col = c;
	 #if defined(KNOW_COLOR)
       col *= ParticleBuffer[In.particleIndex].color;
    #endif
    return col;
}

technique10 ConstantPoint
{
	pass P0
	{

		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}
