<h1 align="center">
  <br>
  <a href="http://www.amitmerchant.com/electron-markdownify">
    <img src="https://github.com/jpbruyere/Crow/blob/master/Images/crow.png" alt="C.R.O.W." width="140">
  </a>
  <br>  
    <br>
  C# Rapid Open Widgets
  <br>  
<p align="center">
  <a href="https://gitter.im/CSharpRapidOpenWidgets?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge">
    <img src="https://badges.gitter.im/CSharpRapidOpenWidgets.svg">
  </a>
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

## Presentation
**C.R.O.W.** is a [widget toolkit](https://en.wikipedia.org/wiki/Widget_toolkit) and rendering engine entirely developed in **C#**, offering a nice trade-off between complexity of language and performances. Crow provides a declarative interface language with styling and templates called [IML](interface-markup-language) for **Interface Markup Language** similar to [XAML](https://en.wikipedia.org/wiki/Extensible_Application_Markup_Language) and a binding system for easy c# code linking.
<p align="center">
  <a href="https://github.com/jpbruyere/Crow/blob/master/Images/screenshot.png">
    <img src="https://github.com/jpbruyere/Crow/blob/master/Images/screenshot.png" width="400">
  </a>
  <br>(CrowIde is not realy functional for now, it serves me to test limits and perf of templated interfaces)
</p>

#### Features
- [Declarative interface definition](https://github.com/jpbruyere/Crow/wiki/interface-markup-language).
- [Templates](https://github.com/jpbruyere/Crow/wiki/Templates)
- [Styling](https://github.com/jpbruyere/Crow/wiki/Styling)
- [Dynamic binding system](https://github.com/jpbruyere/Crow/wiki/The-binding-system)
- SVG rendering (with [rsvg library](https://developer.gnome.org/rsvg/))

#### Status

**C.R.O.W.** is in beta development state, api could change.

I've tested three pathes for further developments:
* OpenTk integration, initial architecture. Nuget package = Crow.OpentTK
* Stand Alone Crow with no OS integration, Nuget has version 8.* under the name Crow.
* Vulkan backend (vk.net) with VKVG as a replacement for cairo and GLFW for OS integration, this last path is the closest to what I want to make with CROW. Maybe I'll try to keep Cairo as an option for backend. The nuget packge is Crow.VK and the source three is in the branch vknet. Due to the early state of VKVG, this should be considered as an early preview.

#### Documentation
* [Introduction](https://github.com/jpbruyere/Crow/wiki)
* [Classes documentation autogenerated from doxygen](https://github.com/jpbruyere/Crow/wiki/index)
* [Tutorials](https://github.com/jpbruyere/Crow/wiki/Tutorials)

Please report bugs and issues on [GitHub](https://github.com/jpbruyere/Crow/issues)

-------------------

## Getting Start

### Requirements
- [Cairo Graphic Library](https://cairographics.org/) >= 1.20 
- [rsvg library](https://developer.gnome.org/rsvg/) for svg rendering

