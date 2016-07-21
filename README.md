CROW [![NuGet Version and Downloads](https://buildstats.info/nuget/Crow.dll)](https://www.nuget.org/packages/Crow.dll/) [![Build Status](https://travis-ci.org/jpbruyere/Crow.svg?branch=master)](https://travis-ci.org/jpbruyere/Crow)
===========

**CROW** is a pure **C#** widget toolkit originally developed for easy GUI implementation for OpenTK.
Trying to make it as efficient as possible, it evolved as a full feature toolkit with templates, styles, compositing,  and  bindings.
Running under Mono, With multi-platform libraries it should run on any target.

**Crow** has full transparency support, but a fast opaque rendering queue exist for heavy critical application.

Screen shots
============

<table width="100%">
  <tr>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/magic3d.png" alt="Magic3d" width="90%"/></td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot3.png" alt="Screen Shot" width="90%" /> </td>
    <td width="30%" align="center"><img src="https://jpbruyere.github.io/Crow/images/screenshot1.png" alt="Screen Shot" width="90%"/> </td>
  </tr>
</table>

Feature
========

- **XML** interface definition.
- Templates and styling
- Dynamic binding system with code injection.
- Inlined delegates in XML

Building
========

```
git clone https://github.com/jpbruyere/Crow.git   	# Download source code from github
cd Crow	                                    		# Enter the source directory
nuget restore Crow.sln								# Restore nuget packages
msbuild /p:Configuration=Release Crow.sln			# Build on .Net (Windows)
xbuild  /p:Configuration=Release Crow.sln			# Build on Mono (Linux / Mac OS X)
```
