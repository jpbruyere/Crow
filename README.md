GOLib
=====

Graphic Object Library: custom widget library on top of cairo. Support win32, opengl, framebuffer surfaces

Initially designed to provide fast widget toolkit for game development with OpenTK, the interface code of my game engine
was splitted in this lib to provide generic widgets toolkit for other uses.

On linux, OpenTK gives a abstraction layer for periferics.

On the framme buffer, I use a small self-made multi-threaded lib on top of ev-dev.

For win32 surface, i still need to code low level periferic access, no great motivation to do that....

It support transparency by default (compositing), but a faster lib with no transparency could be achieve easily


Lots of debug have been made, i release the code, and will try to make it evolves and perhaps find new motivations with 
contributors.


