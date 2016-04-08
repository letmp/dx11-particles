#include "../fxh/Defines.fxh"

#if !defined(PI)
#define PI 3.1415926535897932
#endif

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
		float3 velocity;
		float lifespan;
	#endif
};

Texture2D texture2d;
StructuredBuffer<Particle> ParticleBuffer;
//StructuredBuffer<uint> AliveIndexBuffer;

cbuffer cbPerDraw : register( b0 )
{
    float4x4 tVP : VIEWPROJECTION;
};

cbuffer cbPerObj : register( b1 )
{
    float4 c <bool color=true;> = 1;
	float2 res : TARGETSIZE;
    float radius = 5;
    float innerradius = 0;
    float Perspective = 1;
	bool RenderDead = false;
};

float3 qpos[4]:IMMUTABLE ={float3( -1, 1, 0 ),float3( 1, 1, 0 ),float3( -1, -1, 0 ),float3( 1, -1, 0 ),};
float2 quv[4]:IMMUTABLE ={float2(0,1), float2(1,1),float2(0,0),float2(1,0),};

SamplerState sL
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
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
	uint iv : VID;
};

VSOut VS(VSIn In)
{
    VSOut Out = (VSOut)0;
	//uint slotIndex = AliveIndexBuffer[In.iv];
	uint slotIndex = In.iv;
	
	float3 p = ParticleBuffer[slotIndex].position;
	float3 vel = ParticleBuffer[slotIndex].velocity;

    Out.pos = mul(float4(p, 1), tVP);

	Out.iv=In.iv;
    return Out;
}
[maxvertexcount(4)]
void GS(point VSOut In[1], inout TriangleStream<VSOut> GSOut)
{
    VSOut o;
	o.iv=In[0].iv;
	uint iv=In[0].iv;
	if((ParticleBuffer[iv].lifespan >= 0.0f) || RenderDead)
	{
	    float3 p = In[0].pos.xyz / max(In[0].pos.w, 0.01);
	    float denom = max(In[0].pos.w, 0.01);
	
	    float2 ss = 1/res;
	
	    for(uint i=0; i<4; i++)
	    {
	    	float size = radius / lerp(1, denom, Perspective);
	        #if defined(KNOW_SIZE)
	            size *= McpsBuffer[iv].size;
	        #endif
	        float2 cpos = qpos[i].xy * size;
	    	cpos *= ss;
	        cpos += p.xy;
	        o.pos = float4(cpos, p.z, 1);
	        o.uv = quv[i];
	    	if((p.z < 1) && (p.z > 0))
	    	{
	    		if(size > 0.2)
	        		GSOut.Append(o);
	    	}
	    }
	
		GSOut.RestartStrip();
	}
}

float4 PS_Tex(VSOut In): SV_Target
{
    #if !defined(TEXTURED)
	    clip(0.5 - length(In.uv.xy-.5));
	    clip(length(In.uv.xy-.5) - innerradius);
	#endif

    float4 col = c;

    #if defined(KNOW_COLOR)
       col *= ParticleBuffer[In.iv].color;
    #endif
    #if defined(TEXTURED)
	   col *= texture2d.Sample( sL, In.uv);
    #endif

    return col;
}


technique10 tech
{
	pass P0
	{

		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetGeometryShader( CompileShader( gs_4_0, GS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS_Tex() ) );
	}
}
