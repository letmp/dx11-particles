#include "../../Core/fxh/Defines.fxh"
#include "../../Core/fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 rotation;
		float3 spin;
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

#if !defined(PI)
	#define PI 3.1415926535897932
	#define TWOPI 6.283185307179586;
#endif

float dist;
float3 omega;

float4x4 VRotate(float3 rot)
{
   rot.x *= TWOPI;
   rot.y *= TWOPI;
   rot.z *= TWOPI;
   float sx = sin(rot.x);
   float cx = cos(rot.x);
   float sy = sin(rot.y);
   float cy = cos(rot.y);
   float sz = sin(rot.z);
   float cz = cos(rot.z);
 
   return float4x4( cz*cy+sz*sx*sy, sz*cx, cz*-sy+sz*sx*cy, 0,
                   -sz*cy+cz*sx*sy, cz*cx, sz*sy+cz*sx*cy , 0,
                    cx * sy       ,-sx   , cx * cy        , 0,
                    0             , 0    , 0              , 1);
}

float4x4 transMat(float3 transXYZ){
	
	return float4x4( 	1,0,0, transXYZ.x,
						0,1,0, transXYZ.y,
						0,0,1, transXYZ.z,
						0,0,0,1);
}

float4 multQuat(float4 q1, float4 q2)
{
return float4(
q1.w * q2.x + q1.x * q2.w + q1.z * q2.y - q1.y * q2.z,
q1.w * q2.y + q1.y * q2.w + q1.x * q2.z - q1.z * q2.x,
q1.w * q2.z + q1.z * q2.w + q1.y * q2.x - q1.x * q2.y,
q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
);
}

/*float3 rotate_vector( float4 quat, float3 vec )
{
float4 qv = multQuat( quat, float4(vec, 0.0) );
return multQuat( qv, float4(-quat.x, -quat.y, -quat.z, quat.w) ).xyz;
}*/

float3 rotate_vector( float4 quat, float3 vec )
{
return vec + 2.0 * cross( cross( vec, quat.xyz ) + quat.w * vec, quat.xyz );
}


struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSSet(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	float3 vel = ParticleBuffer[particleIndex].velocity;
	
    float length = sqrt(vel.x * vel.x + vel.y * vel.y  + vel.z * vel.z); //length
    float nVelX = vel.x;
    float nVelY = vel.y;
    float nVelZ = vel.z;
    
    if (length != 0)
    {
        nVelX = vel.x / length;
        nVelY = vel.y / length;
        nVelZ = vel.z / length;
    }
    
    //converter
    float conv = (2 * PI);
    float3 rotationV;
    
    //CONVERT FROM POLAR TO CARTESIAN VVVV-STYLE
    float lengthPol = nVelX * nVelX + nVelY * nVelY + nVelZ * nVelZ;
    
    if (lengthPol > 0)
    {
        lengthPol = sqrt(lengthPol);
        float pitch = asin(nVelY / lengthPol);
        float yaw = 0.0;
        if(nVelZ != 0)
        yaw = atan2(-nVelX, -nVelZ);
        else if (nVelX > 0)
        yaw = -PI / 2;
        else
        yaw = PI / 2;
        
        rotationV = float3(pitch, yaw, lengthPol) / conv ;
    }
    
    else
    
    {
        rotationV = float3(0,0,0);
    }
	
	float4 reloc1 = mul(transMat(float3(-dist,0,0)),float4(0,0,0,1));
	float4 reloc2 = mul( VRotate(-omega), reloc1);
	float4 reloc3 = mul( transMat(float3(dist,0,0)), reloc2);
	//float4 pnew = mul( transpose(VRotate(rotationV)), reloc3);
	
	ParticleBuffer[particleIndex].rotation = rotationV;
	ParticleBuffer[particleIndex].acceleration += reloc3.xyz;
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }