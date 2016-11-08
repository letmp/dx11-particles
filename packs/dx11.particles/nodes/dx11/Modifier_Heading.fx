#include "../fxh/Defines.fxh"
#include "../fxh/IndexFunctions.fxh"

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 rotation;
		float3 velocity;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

#if !defined(PI)
	#define PI 3.1415926535897932
	#define TWOPI 6.283185307179586;
#endif

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
	
	
	ParticleBuffer[particleIndex].rotation = rotationV;
}

technique11 Set { pass P0{SetComputeShader( CompileShader( cs_5_0, CSSet() ) );} }