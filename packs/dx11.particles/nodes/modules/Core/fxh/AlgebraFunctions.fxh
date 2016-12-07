// =============================================================================
// CONSTANTS
// =============================================================================

#if !defined(PI)
	#define PI 3.1415926535897932
	#define TWOPI 6.283185307179586;
#endif


// =============================================================================
// QUATERNION FUNCTIONS
// =============================================================================

float4 QuatMul(float4 q1, float4 q2)
{
return float4(
	q1.w * q2.x + q1.x * q2.w + q1.z * q2.y - q1.y * q2.z,
	q1.w * q2.y + q1.y * q2.w + q1.x * q2.z - q1.z * q2.x,
	q1.w * q2.z + q1.z * q2.w + q1.y * q2.x - q1.x * q2.y,
	q1.w * q2.w - q1.x * q2.x - q1.y * q2.y - q1.z * q2.z
	);
}

float4 QuatFromAxisAngle(float3 axis, float angle)
{ 
  float4 qr;
  float half_angle = (angle * 0.5) * PI / 180.0;
  qr.x = axis.x * sin(half_angle);
  qr.y = axis.y * sin(half_angle);
  qr.z = axis.z * sin(half_angle);
  qr.w = cos(half_angle);
  return qr;
}

float4 QuatInverse(float4 q)
{ 
  return float4(-q.x, -q.y, -q.z, q.w); 
}

// =============================================================================
// ROTATION FUNCTIONS
// =============================================================================

float3 RotateVertexPosition(float3 position, float3 axis, float angle)
{ 
  float4 q = QuatFromAxisAngle(axis, angle);
  float3 v = position.xyz;
  return v + 2.0 * cross(q.xyz, cross(q.xyz, v) + q.w * v);
}

float3 RotateByVector(float3 vec){
	
	float dst = length(vec);
    float nVelX = vec.x;
    float nVelY = vec.y;
    float nVelZ = vec.z;
    
    if (dst != 0)
    {
        nVelX = vec.x / dst;
        nVelY = vec.y / dst;
        nVelZ = vec.z / dst;
    }
    
    float3 rotation;
    float conv = (2 * PI);
    
    // POLAR -> CARTESIAN
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
        
        rotation = float3(pitch, yaw, 0) / conv ;
    }
    
    else
    
    {
        rotation = float3(0,0,0);
    }
	return rotation;
}

// =============================================================================
// MATRIX FUNCTIONS
// =============================================================================
float4x4 MatrixRotation(float3 rot)
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

float4x4 MatrixTranslation(float3 transXYZ){
	
	return float4x4( 	1,0,0, transXYZ.x,
						0,1,0, transXYZ.y,
						0,0,1, transXYZ.z,
						0,0,0,1);
}

float4x4 MatrixScaling(float3 scaleXYZ){
	
	return float4x4( 	scaleXYZ.x,0,0,0,
						0,scaleXYZ.y,0,0,
						0,0,scaleXYZ.z,0,
						0,0,0,1);
}