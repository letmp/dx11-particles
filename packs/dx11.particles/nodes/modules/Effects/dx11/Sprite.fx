

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 position;
	#endif
};

StructuredBuffer<Particle> ParticleBuffer;
StructuredBuffer<uint> AlivePointerBuffer;

Texture2D texture2d;
SamplerState sL
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Clamp;
    AddressV = Clamp;
};

cbuffer cbPerDraw : register( b0 )
{
    float4x4 tVP : VIEWPROJECTION;
	float4x4 tVI:VIEWINVERSE;
	float4x4 tW:WORLD;
	float4 Color <bool color=true;> = {1.0,1.0,1.0,1.0};
	float4x4 tTex <string uiname="Texture Transform";>;
	float3 Size <float uimin=0.0;> = 1.0;
	float3 UpVector={0,1,0};
};

float3 g_positions[4]:IMMUTABLE ={{-1,1,0},{1,1,0},{-1,-1,0},{1,-1,0}};
float2 g_texcoords[4]:IMMUTABLE ={{0,0},{1,0},{0,1},{1,1}};

/* ===================== STRUCTURES ===================== */

struct VSIn
{
	uint iv : SV_VertexID;
};

struct VSOut
{
    float4 PosWVP : SV_POSITION;
	float2 TexCd : TEXCOORD0;
	float4 PosW:TEXCOORD1;
	float3 Size:TEXCOORD2;
	uint particleIndex : VID;
};

/* ===================== VERTEX SHADER ===================== */

VSOut VS(VSIn In)
{
    VSOut Out = (VSOut)0;
	
	uint particleIndex = AlivePointerBuffer[In.iv];
	Out.particleIndex = particleIndex;
	
	float4 p = float4(0,0,0,1);
	p.xyz = ParticleBuffer[particleIndex].position;

	Out.PosW = mul(p, tW);
    Out.PosWVP = mul(Out.PosW, tVP);

	float3 size = Size;
	#if defined(KNOW_SCALE)
            size *= ParticleBuffer[particleIndex].scale;
    #endif
	Out.Size = size;
	
    return Out;
}

/* ===================== GEOMETRY SHADER ===================== */

[maxvertexcount(4)]
void gsSPRITE(point VSOut In[1], inout TriangleStream<VSOut> SpriteStream)
{
    VSOut Out = In[0];
	
	for(int i=0;i<4;i++){

		Out.TexCd=mul(float4((g_texcoords[i].xy*2-1)*float2(1,-1),0,1),tTex).xy*float2(1,-1)*.5+.5;
		Out.PosWVP=mul( float4( In[0].PosW.xyz + In[0].Size * mul(g_positions[i].xyz,(float3x3)tVI),1),tVP);
		SpriteStream.Append(Out);
	}

}

float3x3 lookat(float3 dir,float3 up=float3(0,1,0)){float3 z=normalize(dir);float3 x=normalize(cross(up,z));float3 y=normalize(cross(z,x));return float3x3(x,y,z);} 
[maxvertexcount(4)]
void gsBILLBOARD(point VSOut In[1], inout TriangleStream<VSOut> SpriteStream,uniform bool axis=0)
{
    VSOut Out=In[0];
	float3 vUp=normalize(float3(1,0,1));
	vUp=UpVector;

	float3 View=normalize((In[0].PosW.xyz-tVI[3].xyz));
	
	if(axis){
		float3x3 lktup=lookat(vUp+.000001);
		View=mul(View,transpose(lktup));
		View.z=0;
		View=mul(View,(lktup));
	}
	
	float3x3 lkt=lookat(View,vUp);
	
	for(int i=0;i<4;i++){
		Out.TexCd=mul(float4((g_texcoords[i].xy*2-1)*float2(1,-1),0,1),tTex).xy*float2(1,-1)*.5+.5;
		Out.PosWVP=mul(float4(In[0].PosW.xyz + In[0].Size * mul(g_positions[i].xyz, lkt),1),tVP);
		SpriteStream.Append(Out);
	}

}

[maxvertexcount(1)]
void gsPOINT(point VSOut In[1], inout PointStream<VSOut>GSOut)
{
	VSOut Out;	
	Out=In[0];
	Out.TexCd=mul(float4(0.5,0.5,0,1),tTex).xy*float2(1,-1)*.5+.5;
	GSOut.Append(Out);
}

/* ===================== PIXEL SHADER ===================== */

float4 PS(VSOut In): SV_Target
{
    float4 col = Color;
	
    #if defined(KNOW_COLOR)
       col *= ParticleBuffer[In.particleIndex].color;
    #endif

	col *= texture2d.Sample( sL, In.TexCd.xy);
    
	if (col.a == 0) discard; // dont draw invisible pixels*/
	
    return col;
}

/* ===================== TECHNIQUE ===================== */

technique10 Sprite{
	pass P0{
		SetVertexShader(CompileShader(vs_4_0,VS()));
		SetGeometryShader(CompileShader(gs_4_0,gsSPRITE()));
		SetPixelShader(CompileShader(ps_4_0,PS()));
	}
}
technique10 Billboard{
	pass P0{
		SetVertexShader(CompileShader(vs_4_0,VS()));
		SetGeometryShader(CompileShader(gs_4_0,gsBILLBOARD()));
		SetPixelShader(CompileShader(ps_4_0,PS()));
	}
}
technique10 BillboardAxis{
	pass P0{
		SetVertexShader(CompileShader(vs_4_0,VS()));
		SetGeometryShader(CompileShader(gs_4_0,gsBILLBOARD(1)));
		SetPixelShader(CompileShader(ps_4_0,PS()));
	}
}
technique10 Point{
	pass P0{
		SetVertexShader(CompileShader(vs_4_0,VS()));
		SetGeometryShader(CompileShader(gs_4_0,gsPOINT()));
		SetPixelShader(CompileShader(ps_4_0,PS()));
	}
}