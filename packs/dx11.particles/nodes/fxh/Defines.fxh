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

// =====================================================
//                  DEFAULT DEFINES
// =====================================================
// RENDERSEMANTICS
cbuffer mcpsUniforms
{
    float2 mcpsTime : PS_TIME;
};
