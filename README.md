CROW [![NuGet Version and Downloads](https://buildstats.info/nuget/Crow.dll)](https://www.nuget.org/packages/Crow.dll/)
[![Build Status](https://travis-ci.org/jpbruyere/Crow.svg?branch=master)](https://travis-ci.org/jpbruyere/Crow)

**CROW** is a pure **C#** widget toolkit originaly developed for easy GUI implementation with OpenTK.
Trying to make it as efficient as possible, it evolved as a full feature toolkit with templates, styles and  bindings.
Running under Mono, With multi-platform libraries it should run on any target.

**CROW** use **Mono.Cairo** for rendering. The resulting widgets are outputed to an in memory bitmap
and a clipping system ensure only dirty region are updated. Depending on the graphical context, this bitmap
may be used as Texture, Pixbuf, etc...

**Crow** has full transparency support, but a fast opaque rendering queue exist for heavy critical application.

Thanks to the job done by OpenTK team on linux drm/kms support, Crow may run without a X server directely in console.

#####Screen shots
<img src="/magic3d.png?raw=true" alt="Magic3d" width="20%" /> 
<img src="/screenshot1.png?raw=true" alt="Screen Shot" width="20%" /> 
<img src="/screenshot2.png?raw=true" alt="Screen Shot" width="20%" /> 

FEATURES
========

- **XML** interface definition.
- Templates and styling
- Dynamic binding system with code injection.
- Inlined delegate in XML

Example
-------

```
```
Building
========

#####Build Crow
```
git clone https://github.com/jpbruyere/Crow.git   	# Download source code from git
cd Crow	                                    		# Enter the source directory
nuget restore Crow.sln								# Restore nuget packages
msbuild /p:Configuration=Release Crow.sln			# Build on .Net (Windows)
xbuild  /p:Configuration=Release Crow.sln			# Build on Mono (Linux / Mac OS X)
```


