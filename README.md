GOLib
=====

Graphic Object Library: custom widget library c# version.

- Use OpenTK as top container for device abstraction layer
- Curent drawing routines use Mono.Cairo

RoadMap:

    - Implement Vertical and Horizontal layouting queue instead
      of testing the whole object tree during layout.
    - Implement GL textures backend, as in the c++ version
    - Validate complete drm rendering stack (OpenTK, and Cairo
      already have experimental support for drm stack)

