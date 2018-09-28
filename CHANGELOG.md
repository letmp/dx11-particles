ChangeLog
=========

# 1.0.6 (28.09.2018)
* bugfix: setupcounter in emitters is always enabled. disabling it killed all particles
* bugfix: calculation of pixel ids in Layer Emitter was wrong
* bugfix: scale selector in equal mode wasnt working
* bugfix: dispose buffers on reset
* added: girlpower example for creating lines with particles
* added: kill modifier
* added: position selector
* added: emitter for Orbbec Astra camera
* updated: readability of buffer overview pdf
* updated: better help patch for age modifier
* updated: email address of author
* updated: kinect -> depth cameras
* removed: obselete nodes

# 1.0.5 (28.03.2018)
* updated: Age & Lifespan modifiers are affecting each other now
* updated: ColorByLifeTime help patch shows the new Age/Lifetime behaviour
* added: Kill modifier
* updated: help patch for age modifier is a bit more in depth
* bugfix: MultiStructuredBufferRendererNode did not work for AppendStructuredBuffers
* removed: obselete version of WorldFilter for Kinect

# 1.0.4 (31.01.2018)
* added: AsGeometry node outputs a single geometry for the usage with 3rd party shaders

# 1.0.3 (18.12.2017)
* added: network support (buffer/image/raw converter nodes, splitter/store nodes, zeromq nodes,.. )
* added: AttributeBuffer also outputs a buffer with selected attributes
* added: Peak (DX11.Particles.Analysis)
* added: Geometry (DX11.Particles.Kinect) to Render Kinect Depth as Geometry directly
* added: Tutorials for custom selectors and modifiers
* updated: WorldRepair & WorldSmooth nodes (Kinect Filter)
* bugfixed: GeometryBuffer (DX11.Particles.Tools Instanced) was adding duplicate particles
* bugfixed: vectorfield modifier had wrong textureformat and did wrong lookups
* bugfixed: ply importer
* bugfixed: MultistructuredBufferRenderer
* bugfixed: Dynamic Shader node did duplicate init of dynamic pins in some cases
* bugfixed: Blobdetection 

# 1.0.1 (26.6.2017)
* big performance improvements (especially on startup)
* experimental chunk logic (import ascii/ply/obj pointclouds and generate chunks -> stream them with LOD. have a look at girlpower folder)
* cloneable templates for emitters/modifiers/selectors
* geometry emitter (have a look at girlpower)
* better vectorfield handling and debugging
* a lot of small bugfixes

# 1.0.0 Initial Release (25.12.2016)
