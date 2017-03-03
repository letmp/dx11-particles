// =====================================================
//                  TEXTURE FUNCTIONS
// =====================================================

SamplerState s0: IMMUTABLE {Filter = MIN_MAG_MIP_POINT;AddressU=Border;AddressV=Border;};

float3 GetRGB(Texture2D tex, float3 position, float4x4 tVP)
{
	float4 pos = mul(float4 (position,1), tVP);
	float2 uv = pos.xy/pos.w;
	uv.x = (uv.x / 2 + 0.5);
	uv.y = (uv.y / - 2 + 0.5);
	return tex.SampleLevel (s0, uv, 0).rgb;
}