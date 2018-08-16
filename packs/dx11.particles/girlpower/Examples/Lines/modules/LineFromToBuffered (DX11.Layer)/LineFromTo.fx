//@author: gregsn
//@help: standard line shader
//@tags: 
//@credits: 

#include <packs\InstanceNoodles\nodes\modules\Common\InstanceNoodles.fxh>

Texture2D texture2d <string uiname="Texture";>;

SamplerState g_samLinear : IMMUTABLE
{
    Filter = MIN_MAG_MIP_LINEAR;
    AddressU = Wrap;
    AddressV = Wrap;
};
 
cbuffer cbPerDraw : register( b0 )
{
	float4x4 tWVP : WORLDVIEWPROJECTION;
	float4x4 tP : PROJECTION;

	float4x4 tTex <string uiname="Texture Transform";>;

	
};
StructuredBuffer<float3>  Point1Buffer, Point2Buffer;
StructuredBuffer<float>  widthBuffer;
float widthDefault = 0.01;

StructuredBuffer<float4> colorBuffer;
float4 colorDefault <bool color=true;String uiname="Color Defaut";> = { 1.0f,1.0f,1.0f,1.0f };

struct VS_IN
{
	float4 pos : POSITION;
	float4 uv : TEXCOORD0;

	uint ii : SV_InstanceID;
};

struct vs2ps
{
    float4 pos: SV_POSITION;
    float4 uv: TEXCOORD0;
	float4 color : COLOR0;
};

vs2ps VS(VS_IN input)
{
    float w = bLoad(widthBuffer, widthDefault, input.ii); 
	w *= 0.003;

    //inititalize all fields of output struct with 0
    vs2ps Out = (vs2ps)0;
    
    float u= input.pos.x+0.5;
    //Out.uv = float2(u, Pos.y*2);
    
    // get point on curve
    float4 p;
    //p = float4(lerp(Point1, Point2, u), 1);

    // get position in projection space
    //p = mul(p, tWVP);

    // get tangent in projection space
    float4 p1 = mul(float4(Point1Buffer[input.ii], 1), tWVP);
    float4 p2 = mul(float4(Point2Buffer[input.ii], 1), tWVP);

    p = lerp(p1, p2, u);

    p1 /= p1.w;
    p2 /= p2.w;
    float4 tangent = p2 - p1;

    //p = lerp(p1, p2, u);

    // get normal in projection space
    float2 normal = normalize(float2(tangent.y, -tangent.x));

    // translate point to get a thick curve
    float2 off = input.pos.y * normal * w * p.w;

    // correct aspect ratio
    off *= mul(float4(1, 1, 0, 0), tP).xy;

    p+= float4(off, 0, 0);

    //tangent = normalize(tangent);
    //float3 normal = cross(tangent, float3(0,0,1));
    //p += Pos.y * float4(normal, 0) * w * p.w;

    // output pos p
    Out.pos = p; input.pos;

    input.uv.x *= .1 * length(tangent) / w;

    //ouput texturecoordinates
    Out.uv = mul(input.uv, tTex);
	
	//color buffer
	 Out.color = bLoad(colorBuffer, colorDefault, input.ii);

    return Out;
}


// --------------------------------------------------------------------------------------------------
// PIXELSHADERS:
// --------------------------------------------------------------------------------------------------

float4 PS(vs2ps In): SV_Target
{
    float4 col = texture2d.Sample(g_samLinear, In.uv.xy) * In.color;
    //col.a *= 1 - pow(abs(In.uv.y), 4);
    return col;
}

// --------------------------------------------------------------------------------------------------
// TECHNIQUES:
// --------------------------------------------------------------------------------------------------

technique10 TLine
{
    pass P0
    {
		SetVertexShader( CompileShader( vs_5_0, VS() ) );
		SetPixelShader( CompileShader( ps_5_0, PS() ) );
    }
}
