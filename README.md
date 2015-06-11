#GOLib

Graphic Object Library: custom widget library c# version.

- Use OpenTK as top container for device abstraction layer by default, (other container: GTK, GDK)
- Allow easy creation of XAML like interface under linux directely in console mode, without X
  It only required Mono,Mesa,GBM and DRM libraries.
- Templated controls, with dynamic binding
- inlined delegate in XML
- Curent drawing routines use Mono.Cairo

#####GOLib in action

![GOLib in action](/screenshot2.png?raw=true "golib")

![GOLib in action](/magic3d.png?raw=true "Magic3d")

RoadMap:

	- TreeView, templated of course...
	  Menu, Popper, Combobox, PangoLayouting controls, Improved editor
	- Monodevelop addin
	- improve inline delegates to handle all conversion and graphic tree parsing with directory navigation syntax
	- Make an easyly compilable example of complete application (3d mesh editor for example)
	- inlined SVG with binding and c# scripting for animation
	- simplified Image subElement.
