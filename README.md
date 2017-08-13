# C# Rapid Open Widgets
[![Donate](https://img.shields.io/badge/Donate-PayPal-green.svg)](https://www.paypal.me/GrandTetraSoftware) [![NuGet Version and Downloads](https://buildstats.info/nuget/Crow.OpenTK)](https://www.nuget.org/packages/Crow.OpenTK) [![Build Status](https://travis-ci.org/jpbruyere/Crow.svg?branch=master)](https://travis-ci.org/jpbruyere/Crow) [![Build Status Windows](https://ci.appveyor.com/api/projects/status/j387lo59vnov8jbc?svg=true)](https://ci.appveyor.com/project/jpbruyere/Crow)

**CROW** is a pure **C#** widget toolkit originally developed for fast GUI implementation in [OpenTK](http://opentk.github.io/) applications.

You can visit the [Wiki](https://github.com/jpbruyere/Crow/wiki) or the [Project Site](https://jpbruyere.github.io/Crow/) for documentation and tutorials. _(in progress)_

Please report bugs and issues on [GitHub](https://github.com/jpbruyere/Crow/issues)

Features
--------

- **XML** interface definition.
- Templates and styling
- Dynamic binding system with code injection.
- Inlined delegates in XML

Screen shots
------------

<table width="100%">
  <tr>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot5.png" alt="CrowIDE" width="90%"/></td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot4.png" alt="Screen Shot" width="90%" /> </td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot3.png" alt="Screen Shot" width="90%"/> </td>
  </tr>
</table>

Requirements
------------

- c# compiler
- [Cairo Graphic Library](https://cairographics.org/) >= 1.10 

Building
------------------
```bash
git clone https://github.com/jpbruyere/Crow.git   	# Download source code from github
cd Crow	                                    		# Enter the source directory
nuget restore Crow.sln								# Restore nuget packages
xbuild  /p:Configuration=Release Crow.sln			# Build with Mono 
```

Using CROW in your OpenTK project
---------------------------------
* add [Crow.OpenTK NuGet package](https://www.nuget.org/packages/Crow.OpenTK/) to your project.
* Derive **CrowWindow** class.
* Load some widget in the **OnLoad** override with `CrowWindow.Load` .
* Build your project with **mono**. (**xbuild**)
- copy **Crow.dll.config** to output directory if you have trouble finding native libs.
