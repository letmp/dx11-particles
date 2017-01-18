StructuredBuffer<float3> lPosBuffer <string uiname="Light Position";>;
StructuredBuffer<float> lAtt0Buffer <string uiname="Light Attenuation 0";>;
StructuredBuffer<float> lAtt1Buffer <string uiname="Light Attenuation 1";>;
StructuredBuffer<float> lAtt2Buffer <string uiname="Light Attenuation 2";>;

StructuredBuffer<float3> lDirBuffer <string uiname="Light Direction";>;

StructuredBuffer<float4> lAmbBuffer <string uiname="Ambient Color";>;
StructuredBuffer<float4> lDiffBuffer <string uiname="Diffuse Color";>;
StructuredBuffer<float4> lSpecBuffer <string uiname="Specular Color";>;

StructuredBuffer<float> lPowerBuffer <string uiname="Power";>;
StructuredBuffer<float> lRangeBuffer <string uiname="Light Range";>;


float4 MultiPhongDirectional(float3 NormV, float3 ViewDirV,float4x4 tV)
{

    uint lDirCount, dummy;
    lDirBuffer.GetDimensions(lDirCount, dummy);
    uint lAmbCount;
    lAmbBuffer.GetDimensions(lAmbCount, dummy);
    uint lDiffCount;
    lDiffBuffer.GetDimensions(lDiffCount, dummy);
    uint lSpecCount;
    lSpecBuffer.GetDimensions(lSpecCount, dummy);
    uint lPowerCount;
    lPowerBuffer.GetDimensions(lPowerCount, dummy);

    float4 color = float4(0,0,0,0);
    
    for(int i = 0; i < lDirCount; i++){

        float3 lDir = lDirBuffer[i];
        float4 lAmb = lAmbBuffer[i % lAmbCount];
        float4 lDiff = lDiffBuffer[i % lDiffCount];
        float4 lSpec = lSpecBuffer[i % lSpecCount];
        float lPower = lPowerBuffer[i % lPowerCount];

        float4 amb = float4(lAmb.rgb, 1);

        //inverse light direction in view space
        float3 LightDirV = normalize(-mul(float4(lDir,0.0f), tV).xyz);

        //halfvector
        float3 H = normalize(ViewDirV + LightDirV);

        //compute blinn lighting
        float3 shades = lit(dot(NormV, LightDirV), dot(NormV, H), lPower);

        float4 diff = lDiff * shades.y;
        diff.a = 1;

        //reflection vector (view space)
        float3 R = normalize(2 * dot(NormV, LightDirV) * NormV - LightDirV);

        //normalized view direction (view space)
        float3 V = normalize(ViewDirV);

        //calculate specular light
        float4 spec = pow(max(dot(R, V),0), lPower*.2) * lSpec;

        color += (amb + diff) + spec;
    }

    return color;
}

float4 MultiPhongPoint(float3 PosW, float3 NormV, float3 ViewDirV, float4x4 tV)
{

    uint posCount, dummy;
    lPosBuffer.GetDimensions(posCount, dummy);

    uint lAtt0Count;
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


    float4 color = float4(0,0,0,0);
    for(int i = 0; i < posCount; i++){

        float3 lPos = lPosBuffer[i];
        
        float lAtt0 = lAtt0Buffer[i % lAtt0Count];
        float lAtt1 = lAtt1Buffer[i % lAtt1Count];
        float lAtt2 = lAtt2Buffer[i % lAtt2Count];
        float4 lAmb = lAmbBuffer[i % lAmbCount];
        float4 lDiff = lDiffBuffer[i % lDiffCount];
        float4 lSpec = lSpecBuffer[i % lSpecCount];
        float lPower = lPowerBuffer[i % lPowerCount];
        float lRange = lRangeBuffer[i % lRangeCount];

        float d = distance(PosW, lPos);
        float atten = 0;

        //compute attenuation only if vertex within lightrange
        if (d<lRange)
        {
           atten = 1/(saturate(lAtt0) + saturate(lAtt1) * d + saturate(lAtt2) * pow(d, 2));
        }

        float4 amb = lAmb * atten;
        amb.a = 1;

        float3 LightDirW = normalize(lPos - PosW);
        float3 LightDirV = mul(float4(LightDirW,0.0f), tV).xyz;

        //halfvector
        float3 H = normalize(ViewDirV + LightDirV);

        //compute blinn lighting
        float4 shades = lit(dot(NormV, LightDirV), dot(NormV, H), lPower);

        float4 diff = lDiff * shades.y * atten;
        diff.a = 1;

        //reflection vector (view space)
        float3 R = normalize(2 * dot(NormV, LightDirV) * NormV - LightDirV);

        //normalized view direction (view space)
        float3 V = normalize(ViewDirV);

        //calculate specular light
        float4 spec = pow(max(dot(R, V),0), lPower*.2) * lSpec;

        color+= (amb + diff) + spec;
    }

    return color;
}