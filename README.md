<h1 align="center">
  <br>
  <a href="http://www.amitmerchant.com/electron-markdownify">
    <img src="https://github.com/jpbruyere/Crow/blob/master/Images/Icons/crow.png" alt="C.R.O.W." width="140">
  </a>
  <br>  
    <br>
  C# Rapid Open Widgets
  <br>  
<p align="center">
  <a href="https://www.paypal.me/GrandTetraSoftware">
    <img src="https://img.shields.io/badge/Donate-PayPal-green.svg">
  </a>
  <a href="https://www.nuget.org/packages/Crow.OpenTK">
    <img src="https://buildstats.info/nuget/Crow.OpenTK">
  </a>
  <a href="https://travis-ci.org/jpbruyere/Crow">
      <img src="https://travis-ci.org/jpbruyere/Crow.svg?branch=master">
  </a>
  <a href="https://ci.appveyor.com/project/jpbruyere/Crow">
    <img src="https://ci.appveyor.com/api/projects/status/j387lo59vnov8jbc?svg=true">
  </a>
</p>
</h1>

**C.R.O.W.** is a [widget toolkit](https://en.wikipedia.org/wiki/Widget_toolkit) and
rendering engine entirely developed in **C#**, offering a nice trade-off between
complexity of language and performances. Crow provides a declarative interface language
with styling and templates
called [IML](interface-markup-language) for **Interface Markup Language** similar to
[XAML](https://en.wikipedia.org/wiki/Extensible_Application_Markup_Language) and a binding system
for easy c# code linking.
<p align="center">
  <a href="https://github.com/jpbruyere/Crow/blob/master/Images/screenshot.png">
    <img src="https://github.com/jpbruyere/Crow/blob/master/Images/screenshot.png" width="400">
  </a>
</p>

For **documentation** and **tutorials** visit the [Wiki](https://github.com/jpbruyere/Crow/wiki)
or the [Project Site](https://jpbruyere.github.io/Crow/).

Please report bugs and issues on [GitHub](https://github.com/jpbruyere/Crow/issues)

## Getting Start

### Requirements
- [mono > 5.0](http://www.mono-project.com/download/)
- [Cairo Graphic Library](https://cairographics.org/) >= 1.10 
- [rsvg library](https://developer.gnome.org/rsvg/) for svg rendering
- [nuget](https://www.nuget.org/).

### Building from source

_[Git](https://git-scm.com) has to be installed._

```bash
git clone https://github.com/jpbruyere/Crow.git     # Download source code from github
cd Crow                                             # Enter the source directory
nuget restore Crow.sln                              # Restore nuget packages
xbuild  /p:Configuration=Release Crow.sln           # Build with Mono 
```

### Using nuget

* add [Crow.OpenTK NuGet package](https://www.nuget.org/packages/Crow.OpenTK/) to your project.
* Derive **CrowWindow** class.
* Load some widget in the **OnLoad** override with `CrowWindow.Load` .
* Build your project with **mono**. (**xbuild**)
- copy **Crow.dll.config** to output directory if you have trouble finding native libs.
