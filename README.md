#CROW :  _C# Rapid Open Widgets._
[![NuGet Version and Downloads](https://buildstats.info/nuget/Crow.OpenTK)](https://www.nuget.org/packages/Crow.OpenTK) [![Build Status](https://travis-ci.org/jpbruyere/Crow.svg?branch=master)](https://travis-ci.org/jpbruyere/Crow) [![Build Status Windows](https://ci.appveyor.com/api/projects/status/j387lo59vnov8jbc?svg=true)](https://ci.appveyor.com/project/jpbruyere/Crow)

**CROW** is a pure **C#** widget toolkit originally developed for easy GUI implementation for [OpenTK](http://opentk.github.io/).

Crow is in early developement state, I'm working on a first beta (0.5).

> Trying to make it as efficient as possible, it evolved as a full feature toolkit with templates, styles, compositing,  and bindings, allowing me to develop new OpenGL apps in a couple of hours.
Running under Mono, With multi-platform libraries it should run on any target.

> Using Crow is an easy way to get instantly some controls into your your OpenGL application. With the binding system, your local variables are bound to the interface very easily and with the full transparency, your openGL scene will always stay fully visible.

You can visit the [Wiki](https://github.com/jpbruyere/Crow/wiki) or the [Project Site](https://jpbruyere.github.io/Crow/) for documentation and tutorials. _(in progress)_

Please report bugs and issues on [GitHub](https://github.com/jpbruyere/Crow/issues)

###Screen shots :

<table width="100%">
  <tr>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/magic3d.png" alt="Magic3d" width="90%"/></td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot3.png" alt="Screen Shot" width="90%" /> </td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot1.png" alt="Screen Shot" width="90%"/> </td>
  </tr>
</table>

###Features :
- **XML** interface definition.
- Templates and styling
- Dynamic binding system with code injection.
- Inlined delegates in XML

###Requirements :
- Mono >= 4.0 Framework. 
- [Cairo Graphic Library](https://cairographics.org/) >= 1.10 
- [OpenTK](http://opentk.github.io/).
- glib, gio, and gdk >= 3.0. (part of GTK project).

###Installing dependencies

####On Linux
For **mono**, You may install **mono-complete**, or only **xbuild**, **mono runtime** and minimal system **CIL** libs (system, xml, drawing)
```bash
sudo apt-get install mono-complete libcairo1.10-cil libgio3.0-cil libgdk3.0-cil libglib3.0-cil
```
Or:
```bash
sudo apt-get install -y xbuild mono-runtime libmono-system-core4.0-cil libmono-system-xml4.0-cil libmono-system-drawing4.0-cil libcairo1.10-cil libgio3.0-cil libgdk3.0-cil libglib3.0-cil
```
####On Windows
- Install [Mono and GTK#](http://www.mono-project.com/download/#download-win)
- Add **CIL dll's** path to your environment **PATH** variable, and also **native dll's** path of cairo and gtk.

    `set path=%path%;C:\Program Files (x86)\Mono\bin`

###Build from sources :
```bash
git clone https://github.com/jpbruyere/Crow.git   	# Download source code from github
cd Crow	                                    		# Enter the source directory
nuget restore Crow.sln								# Restore nuget packages
xbuild  /p:Configuration=Release Crow.sln			# Build with Mono 
```
###Using CROW in your OpenTK project :
* add [Crow.OpenTK NuGet package](https://www.nuget.org/packages/Crow.OpenTK/) to your project.
* Derive **OpenTKGameWindow** class.
* Load some widget in the **OnLoad** override with `CrowInterface.LoadInterface` .
* Build your project with **mono**. (**xbuild**)
- copy **Crow.dll.config** to output directory if you have trouble finding native libs.
