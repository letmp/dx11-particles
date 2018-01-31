ChangeLog
=========

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
