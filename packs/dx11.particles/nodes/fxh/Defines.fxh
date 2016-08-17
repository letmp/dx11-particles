#define DEFINES_FXH

// =====================================================
//                  DEFAULT DEFINES
// =====================================================

// THREAD DEFAULTS
#if !defined(XTHREADS)
	#define XTHREADS 1
#endif
#if !defined(YTHREADS)
	#define YTHREADS 1
#endif
#if !defined(ZTHREADS)
	#define ZTHREADS 1
#endif

// BUFFER DEFAULTS
#if !defined(MAXPARTICLECOUNT)
	#define MAXPARTICLECOUNT 0
#endif

#if !defined(EMITTEROFFSET)
	#define EMITTEROFFSET 0
#endif

#if !defined(MAXGROUPCOUNT)
	#define MAXGROUPCOUNT 0
#endif

#if !defined(MAXGROUPELEMENTCOUNT)
	#define MAXGROUPELEMENTCOUNT 0
#endif

// RENDERSEMANTICS
cbuffer dx11ParticlesUniforms
{
    float2 psTime : PS_TIME;
};

// =====================================================
//                  DEFAULT STRUCTS
// =====================================================

struct GroupLink {
	uint groupId;
	uint particleId;
};