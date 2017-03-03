StructuredBuffer<int> lTypeBuffer <string uiname="Light Type";>;

StructuredBuffer<float3> lPosDirBuffer <string uiname="Light Position";>;
StructuredBuffer<float> lAtt0Buffer <string uiname="Light Attenuation 0";>;
StructuredBuffer<float> lAtt1Buffer <string uiname="Light Attenuation 1";>;
StructuredBuffer<float> lAtt2Buffer <string uiname="Light Attenuation 2";>;

StructuredBuffer<float4> lAmbBuffer <string uiname="Ambient Color";>;
StructuredBuffer<float4> lDiffBuffer <string uiname="Diffuse Color";>;
StructuredBuffer<float4> lSpecBuffer <string uiname="Specular Color";>;

StructuredBuffer<float> lPowerBuffer <string uiname="Power";>;
StructuredBuffer<float> lRangeBuffer <string uiname="Light Range";>;

float4 MultiGouraudDirectional(int i, float3 NormV, float3 ViewDirV,float4x4 tV, uint particleIndex)
{
    uint lAmbCount, dummy;
    lAmbBuffer.GetDimensions(lAmbCount, dummy);
    uint lDiffCount;
    lDiffBuffer.GetDimensions(lDiffCount, dummy);
    uint lSpecCount;
    lSpecBuffer.GetDimensions(lSpecCount, dummy);
    uint lPowerCount;
    lPowerBuffer.GetDimensions(lPowerCount, dummy);

    float3 lDir = lPosDirBuffer[i];
    float4 lAmb = lAmbBuffer[i % lAmbCount];
    float4 lDiff = lDiffBuffer[i % lDiffCount];
    float4 lSpec = lSpecBuffer[i % lSpecCount];
    float lPower = lPowerBuffer[particleIndex % lPowerCount];

    //inverse light direction in view space
    float3 LightDirV = normalize(-mul(float4(lDir,0.0f), tV).xyz);

    //halfvector
    float3 H = normalize(ViewDirV + LightDirV);

    //compute blinn lighting
    float3 shades = lit(dot(NormV, LightDirV), dot(NormV, H), lPower).xyz;

    float4 diff = lDiff * shades.y;
    float4 spec = lSpec * shades.z;

    return diff + lAmb + spec;
}

float4 MultiGouraudPoint(int i,float3 PosW, float3 NormV, float3 ViewDirV, float4x4 tV, uint particleIndex)
{

    uint lAtt0Count, dummy;
    lAtt0Buffer.GetDimensions(lAtt0Count, dummy);
    uint lAtt1Count;
    lAtt1Buffer.GetDimensions(lAtt1Count, dummy);
    uint lAtt2Count;
    lAtt2Buffer.GetDimensions(lAtt2Count, dummy);
    uint lAmbCount;
    lAmbBuffer.GetDimensions(lAmbCount, dummy);
    uint lDiffCount;
    lDiffBuffer.GetDimensions(lDiffCount, dummy);
    uint lSpecCount;
    lSpecBuffer.GetDimensions(lSpecCount, dummy);
    uint lPowerCount;
    lPowerBuffer.GetDimensions(lPowerCount, dummy);
    uint lRangeCount;
    lRangeBuffer.GetDimensions(lRangeCount, dummy);

    float3 lPos = lPosDirBuffer[i];
    float lAtt0 = lAtt0Buffer[i % lAtt0Count];
    float lAtt1 = lAtt1Buffer[i % lAtt1Count];
    float lAtt2 = lAtt2Buffer[i % lAtt2Count];
    float4 lAmb = lAmbBuffer[i % lAmbCount];
    float4 lDiff = lDiffBuffer[i % lDiffCount];
    float4 lSpec = lSpecBuffer[i % lSpecCount];
    float lPower = lPowerBuffer[particleIndex % lPowerCount];
    float lRange = lRangeBuffer[i % lRangeCount];

    float d = distance(PosW, lPos);

    float atten = 0;
    if (d<lRange)
    {
       atten = 1/(saturate(lAtt0) + saturate(lAtt1) * d + saturate(lAtt2) * pow(d, 2));
    }
    float4 amb = atten * lAmb;
    amb.a = 1;

    //inverse light direction in view space
    float3 LightDirW = normalize(lPos - PosW);
    float3 LightDirV = mul(float4(LightDirW,0.0f), tV).xyz;

    //halfvector
    float3 H = normalize(ViewDirV + LightDirV);

    //compute blinn lighting
    float4 shades = lit(dot(NormV, LightDirV), dot(NormV, H), lPower);

    float4 diff = lDiff * shades.y * atten;
    float4 spec = lSpec * shades.z * atten;
    
    return diff + lAmb + spec;
}