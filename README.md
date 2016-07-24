CROW [![NuGet Version and Downloads](https://buildstats.info/nuget/Crow.OpenTK)](https://www.nuget.org/packages/Crow.OpenTK) [![Build Status](https://travis-ci.org/jpbruyere/Crow.svg?branch=master)](https://travis-ci.org/jpbruyere/Crow) [![Build Status Windows](https://ci.appveyor.com/api/projects/status/j387lo59vnov8jbc?svg=true)](https://ci.appveyor.com/project/jpbruyere/Crow)
===========

**CROW** is a pure **C#** widget toolkit originally developed for easy GUI implementation for [OpenTK](http://opentk.github.io/).

Trying to make it as efficient as possible, it evolved as a full feature toolkit with templates, styles, compositing,  and bindings, allowing me to develop new OpenGL apps in a couple of hours.
Running under Mono, With multi-platform libraries it should run on any target.

Using Crow is an easy way to get instantly some controls into your your OpenGL application. With the binding system, your local
variables are bound to the interface very easily and with the full transparency, your openGL scene will always stay fully visible.

You can visit the [Wiki](https://github.com/jpbruyere/Crow/wiki) or the [Project Site](https://jpbruyere.github.io/Crow/) for documentation and tutorials. _(in progress)_

Please report bugs and issues on [GitHub](https://github.com/jpbruyere/Crow/issues)

Screen shots
============

<table width="100%">
  <tr>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/magic3d.png" alt="Magic3d" width="90%"/></td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot3.png" alt="Screen Shot" width="90%" /> </td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot1.png" alt="Screen Shot" width="90%"/> </td>
  </tr>
</table>

Features
========
- **XML** interface definition.
- Templates and styling
- Dynamic binding system with code injection.
- Inlined delegates in XML

Requirements
============

- **C#6** compatible compiler.
- Mono 4.5 Framework on every os. (It solves the **String Encoding** problem.)
- Cairo Graphic Library >= 1.12
- OpenTK version assume you have OpenGL libraries installed on your system.
- GTK Sharp. (At least glib, gio, gdk and cairo cil binding, atk, pango and gtk sharp may be removed from references)

Using CROW in your OpenTK project
=================================
* add [Crow.OpenTK NuGet package](https://www.nuget.org/packages/Crow.OpenTK/) to your project.
* Derive **OpenTKGameWindow** class.
* Load some widget in the **OnLoad** override with `CrowInterface.LoadInterface` .

Build from sources
==================

```
git clone https://github.com/jpbruyere/Crow.git   	# Download source code from github
cd Crow	                                    		# Enter the source directory
nuget restore Crow.sln								# Restore nuget packages
msbuild /p:Configuration=Release Crow.sln			# Build on .Net (Windows)
xbuild  /p:Configuration=Release Crow.sln			# Build on Mono (Linux / Mac OS X)
```
