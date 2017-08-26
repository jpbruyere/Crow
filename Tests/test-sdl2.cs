//
// test-sdl2.cs
//
// Author:
//       jp <>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Platform;
using Crow.SDL2;

namespace Crow
{
	public class test_sdl2
	{
		public test_sdl2 ()
		{
		}

		static int colors = 32, depth=24, stencil=0,samples=1;
		static int width=500,height=500;

		static void Main(){
			SysWMInfo sdlInfo;

			SDL.Init (SystemFlags.EVERYTHING);


			IntPtr hWin = SDL.CreateWindow ("test sdl2", 0, 0, width, height, WindowFlags.OPENGL | WindowFlags.SHOWN);

			SDL.GL.SetAttribute (ContextAttribute.CONTEXT_MAJOR_VERSION, 3);
			SDL.GL.SetAttribute (ContextAttribute.CONTEXT_MAJOR_VERSION, 3);
			SDL.GL.SetAttribute (ContextAttribute.SHARE_WITH_CURRENT_CONTEXT, 1);

			IntPtr hCtx = SDL.GL.CreateContext (hWin);
			IntPtr hCairoCtx = SDL.GL.CreateContext (hWin);

			SDL.GetWindowWMInfo (hWin, out sdlInfo);
			SDL.GL.MakeCurrent(hWin,hCtx);

			int major, minor;
			SDL.GL.GetAttribute (ContextAttribute.CONTEXT_MAJOR_VERSION, out major);
			SDL.GL.GetAttribute (ContextAttribute.CONTEXT_MINOR_VERSION, out minor);
			Console.WriteLine ("gl context = {0}.{1}\n", major, minor);

			Toolkit.Init();
			IWindowInfo wi = Utilities.CreateSdl2WindowInfo (hWin);
			ContextHandle otkCtx = new ContextHandle (hCtx);

			OpenTK.Graphics.GraphicsContext ctx = new OpenTK.Graphics.GraphicsContext (otkCtx, wi);



			ctx.LoadAll();

			Cairo.GLXDevice dev = new Cairo.GLXDevice (sdlInfo.Info.X11.Display, hCairoCtx);

			int tex = GL.GenTexture ();
			GL.BindTexture (TextureTarget.Texture2D, tex);
			GL.TexImage2D (TextureTarget.Texture2D, 0, PixelInternalFormat.Rgba, width, height, 0,
				PixelFormat.Bgra, PixelType.UnsignedByte, IntPtr.Zero);

			//dev.Acquire ();
			Cairo.GLSurface surf = new Cairo.GLSurface (dev,Cairo.Content.ColorAlpha,(uint)tex, width,height);

			SDL.GL.MakeCurrent(hWin,hCairoCtx);
			using (Cairo.Context gr = new Cairo.Context (surf)) {
				gr.Rectangle (150, 100, 200, 100);
				gr.SetSourceRGBA (1.0, 0.0, 0.0,0.5);
				gr.Fill ();
				gr.Rectangle (200, 150, 200, 100);
				gr.SetSourceRGBA (0.0, 1.0, 0.0,0.5);
				gr.Fill ();
			}
			surf.Flush ();
			surf.SwapBuffers ();

			SDL.GL.MakeCurrent(hWin,hCtx);


			Matrix4 proj = Matrix4.CreateOrthographicOffCenter (-1f, 1f, -1f, 1f, 1.0f, -1.0f);
			GL.MatrixMode(MatrixMode.Projection);
			GL.LoadMatrix (ref proj);
			GL.MatrixMode(MatrixMode.Modelview);
			GL.LoadIdentity();

			GL.ClearColor(0.0f, 0.0f, 0.5f, 1.0f);
			GL.Clear (ClearBufferMask.ColorBufferBit|ClearBufferMask.DepthBufferBit);

			GL.Viewport (0, 0, width, height);

			GL.Enable(EnableCap.DepthTest);
			GL.Enable(EnableCap.Texture2D);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.One, BlendingFactorDest.OneMinusSrcAlpha);

			GL.BindTexture (TextureTarget.Texture2D, tex);
			GL.Begin(PrimitiveType.Quads);

			// Bottom-Left
			GL.TexCoord2(0, 1);
			GL.Vertex2(-1, -1);

			// Upper-Left
			GL.TexCoord2(0, 0);
			GL.Vertex2(-1, 1);

			// Upper-Right
			GL.TexCoord2(1, 0);
			GL.Vertex2(1, 1);

			// Bottom-Right
			GL.TexCoord2(1, 1);
			GL.Vertex2(1, -1);

			GL.End();
	
			//ctx.SwapBuffers ();
			SDL.GL.SwapWindow (hWin);

			System.Threading.Thread.Sleep (1000);

			surf.Dispose();
			GL.DeleteTexture (tex);
			dev.Dispose ();


			SDL.GL.MakeCurrent (hWin, IntPtr.Zero);
			SDL.GL.DeleteContext (hCairoCtx);
			SDL.GL.DeleteContext (hCtx);
			SDL.DestroyWindow (hWin);




		}
	}
}

