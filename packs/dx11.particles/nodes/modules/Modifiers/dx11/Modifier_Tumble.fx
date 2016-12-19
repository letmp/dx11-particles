#include <packs\dx11.particles\nodes\modules\Core\fxh\Core.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\IndexFunctions.fxh>
#include <packs\dx11.particles\nodes\modules\Core\fxh\AlgebraFunctions.fxh>

struct Particle {
	#if defined(COMPOSITESTRUCT)
  		COMPOSITESTRUCT
 	#else
		float3 velocity;
		float3 acceleration;
		float angle;
	#endif
};

RWStructuredBuffer<Particle> ParticleBuffer : PARTICLEBUFFER;

float speed <String uiname="Speed";> = 1;
float strength <String uiname="Strength";> = 2;

struct csin
{
	uint3 DTID : SV_DispatchThreadID;
	uint3 GTID : SV_GroupThreadID;
	uint3 GID : SV_GroupID;
};

[numthreads(XTHREADS, YTHREADS, ZTHREADS)]
void CSAdd(csin input)
{
	uint particleIndex = GetParticleIndex( input.DTID.x );
	if (particleIndex == -1 ) return;
	
	float3 axis = ParticleBuffer[particleIndex].velocity;
	if (length(axis) > 0.2){
		axis = normalize(axis);
	
		float4x4 rotation = MatrixRotation(RotateByVector(axis));
		float4 translation = float4(strength, 0, 0, 0);	
		float4 circle = mul(translation, rotation);
		
		float newAngle = ParticleBuffer[particleIndex].angle + (speed * psTime.y);
		float3 circlePoint = RotateVertexPosition( circle.xyz, axis, degrees(newAngle) * TWOPI);
		
		ParticleBuffer[particleIndex].acceleration += circlePoint;
		ParticleBuffer[particleIndex].angle = newAngle;	
	}
	
}

technique11 Add { pass P0{SetComputeShader( CompileShader( cs_5_0, CSAdd() ) );} }
