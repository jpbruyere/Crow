#GOLib

Graphic Object Library: custom widget library c# version.

- Use OpenTK as top container for device abstraction layer
- Curent drawing routines use Mono.Cairo

#####GOLib in action

![GOLib in action](/magic3d.png?raw=true "Magic3d")

RoadMap:

    - Implement Vertical and Horizontal layouting queue instead
      of testing the whole object tree during layout.
    - Implement GL textures backend, as in the c++ version
    - Validate complete drm rendering stack (OpenTK, and Cairo
      already have experimental support for drm stack)

