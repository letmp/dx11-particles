
void VS(float3 p : POSITION, out float4 screenPos : SV_Position)
{
	screenPos = float4 (0,0,0,1);
}

float4 PS(float4 p : SV_Position) : SV_Target
{
	return 1;
}

technique10 Constant
{
	pass P0 <string topology="pointlist";>
	{
		SetVertexShader( CompileShader( vs_4_0, VS() ) );
		SetPixelShader( CompileShader( ps_4_0, PS() ) );
	}
}




