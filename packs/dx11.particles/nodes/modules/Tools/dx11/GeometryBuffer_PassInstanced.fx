//@author: antokhio
//@help: template for standard shaders
//@tags: template
//@credits: 


StructuredBuffer<float4x4> tW;

Texture2D tex0 <string uiname = "Texture In";>;
SamplerState s0:IMMUTABLE {Filter=MIN_MAG_MIP_LINEAR;AddressU=CLAMP;AddressV=CLAMP;};

int InstanceStartIndex = 0;

struct VS_IN
{
	uint ii : SV_InstanceID;
	float4 pos : POSITION;
	float4 normal : NORMAL;
	float4 uv : TEXCOORD0;
};

struct VS_OUT
{
    float3 pos: POSITION;
	float3 normal : NORMAL;
    float4 color: COLOR;
};

VS_OUT VS(VS_IN input)
{
    VS_OUT output = (VS_OUT)0;
    output.pos  = mul(input.pos,tW[input.ii + InstanceStartIndex]).xyz;
	output.normal = mul (normalize (input.normal.xyz), (float3x3) tW[input.ii + InstanceStartIndex]);
    output.color = tex0.SampleLevel(s0,input.uv.xy,0);

	
    return output;
}

GeometryShader StreamOutGS = ConstructGSWithSO( CompileShader( vs_4_0, VS() ), "POSITION.xyz;NORMAL.xyz;COLOR.xyzw;", NULL,NULL,NULL,-1 );

//if the above does not work, try this line instead
//GeometryShader StreamOutGS = ConstructGSWithSO( CompileShader( vs_4_0, VS() ), "POSITION.xyz;NORMAL.xyz;TEXCOORD.xy" );

technique10 PassMesh
{
    pass P0 <string topology="pointlist";>
    {
        SetVertexShader( CompileShader( vs_4_0, VS() ) );
        SetGeometryShader( StreamOutGS );
    }  
}




