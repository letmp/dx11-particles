![dx11-particles!](https://raw.githubusercontent.com/letmp/dx11-particles/master/images/wallpaper-particles.png)

DX11.Particles
=================

This pack is a collection of tools and techniques to handle particles in [vvvv](https://vvvv.org/). It consists of many different emitters, selectors and modifiers that can be freely combined.
It also includes tools for dealing with depth cameras like Kinect(1/2).

The purpose of this pack is to create a central repository for modules and plugins related to particles processed on the GPU.

If you have questions or want to share an idea just head over to the vvvv contribution page of [DX11.Particles](https://vvvv.org/contribution/dx11.particles).

Background
==========

When dealing with particles (and depth cameras like Kinect) in vvvv there are alot of tasks that pop up again and again:
* Write custom shaders to emit particles based on different sources like geometries, textures, buffers, depthcameras
* Write custom shaders to modify their sizes, rotation, velocites, colors, ...
* Write custom shaders to select particles depending on inherent attributes
* Write custom shaders to visualize particles
* Calibrate the Kinect to match the real with the virutal environment
* Analyze attributes of particles to detect events

This pack attempts to provide a modular foundation that can be extended and reused very easily. It included many nodes for all of the listed needs and is designed to be extended in a clear manner.
Nonetheless, the purpose of this project is to bundle the power of the vvvv community and build a comprehensive toolkit we all benefit from.

One requirement while starting this package was to be independent from big thirdparty frameworks like openframeworks or opencv. The reason for that was to keep the maintenance expenses as low as possible.

The other requirement was performance, so all expensive calculations are done on the GPU.

Features
==========

* clean, modular and reusable design
* emit, select, modify and visualize particles
* very easy customization of particle structure
* emitters include modules for structuredBuffers, geometries, kinect, layer, rwstructuredBuffers
* modifiers include modules like attractor, bounce, color, curlnoise, friction, force, heading, rotation, scaling, selfrepulsion, spin, targets, tumble, turbulence, vectorfields, velocity and more
* selectors include modules like age, blobs, color, force, intersect, lifespan, texturemask, modulo, scale, velocity and more
* kinect calibration for mapping realworld data to the virtual scene
* hittests, centroids, bounds
* includes many additional plugins for vvvv ( for example dynamic shaders and multibufferrenderer)

Getting Started
===============

You need:
* latest version of [vvvv](https://vvvv.org/)
* latest version of [dx11-vvvv](https://vvvv.org/contribution/directx11-nodes)
* latest version of [dx11.particles](https://vvvv.org/contribution/dx11.particles)

Be careful to download the packs in identical architectures (x86 or x64).

If you want to create clouds of particles with real world data:
* Kinect/Kinect2

There are help patches for all included nodes that give small examples how to use them. Just highlight a node of your choice and press F1. You can also find more complex examples under /dx11.particles/girlpower/

Best Practise
============

* When you start to develop additional nodes (or nodes on top of existing nodes) try to stay as modular as possible for further usage.
* Stick to the existing folder structure.
* Try to build simple helppatches that show the purpose of the developed node. You can also use the girlpower folder for more complex setups.
* Besides the existing categories there are alot more imaginable. Don't be shy to create new folders for that.

Problems? Questions?
====================

Feel free to ask questions in the [original thread](http://vvvv.org/contribution/dx11.particles) of this pack. You can also post as guest there.
You can also contact me via skype (le-tmp) or email (robert (at) intolight.de).


License
=======

© intolight, 2017
![CC 4.0BY NC SA](http://i.creativecommons.org/l/by-nc-sa/4.0/88x31.png)

Author: intolight (robert (at) intolight.de)

This software is distributed under the [CC Attribution-NonCommercial-ShareAlike 4.0](https://creativecommons.org/licenses/by-nc-sa/4.0/) license.

If this license seems to restrictive for your use case, please contact *license (at) intolight.de* and tell us about your project or your goal, so we can find a solution or an alternative license for you.
