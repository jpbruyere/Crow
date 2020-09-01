// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Threading;
using Crow;
using Glfw;

namespace EglBackend
{

	class MainClass : Interface
	{
		const int OpenglEsApi = 0x00030002;
		const int EglContextApi = 0x00036002;
		Crow.Cairo.EGLDevice dev;

		MainClass (IntPtr hWnd) : base (800, 600, hWnd) {
			IntPtr eglDisp = Glfw3.GetEGLDisplay ();
			IntPtr eglCtx = Glfw3.GetEGLContext (hWnd);

			dev = new Crow.Cairo.EGLDevice (eglDisp, eglCtx);
			Console.WriteLine ($"Egl device creation status: {dev.Status}");
			surf = new Crow.Cairo.GLSurface (dev, Glfw3.GetEGLSurface (hWnd), this.clientRectangle.Width, this.clientRectangle.Height);
			Console.WriteLine ($"Cairo surface creation status: {surf.Status}");
			Glfw3.MakeContextCurrent (hWnd);
			Glfw3.SwapInterval (1);

			using (Crow.Cairo.Context ctx = new Crow.Cairo.Context (surf)) {
				ctx.SetSourceRGB (1, 0, 0);
				ctx.Paint ();
			}

			/*Thread t = new Thread (InterfaceThread) {
				IsBackground = true
			};
			t.Start ();*/

		}
		protected override void Dispose (bool disposing)
		{
			surf.Dispose ();
			dev.Dispose ();

			base.Dispose (disposing);
		}

		public static void Main (string [] args)
		{
			Glfw3.Init ();
			Glfw3.SetErrorCallback ((error, description) => Console.WriteLine ($"{error}:{description}"));
			Glfw3.WindowHint (WindowAttribute.ClientApi, OpenglEsApi);
			Glfw3.WindowHint (WindowAttribute.ContextVersionMajor, 2);
			Glfw3.WindowHint (WindowAttribute.ContextCreationApi, EglContextApi);

			/*Glfw3.WindowHint (WindowAttribute.RedBits, 8);
			Glfw3.WindowHint (WindowAttribute.GreenBits, 8);
			Glfw3.WindowHint (WindowAttribute.BlueBits, 8);
			Glfw3.WindowHint (WindowAttribute.DepthBits, 8);*/


			IntPtr hWnd = Glfw3.CreateWindow (800, 600, "Egl Test", MonitorHandle.Zero, IntPtr.Zero);

			if (hWnd == IntPtr.Zero)
				throw new Exception ("[GLFW3] Unable to create egl Window");
				
			Glfw3.MakeContextCurrent (hWnd);

			using (MainClass app = new MainClass (hWnd)) {
				app.Init ();
				while(!Glfw3.WindowShouldClose(hWnd)) {
					app.Update ();
					if (app.IsDirty) {
						(app.surf as Crow.Cairo.GLSurface).SwapBuffers ();
						//Glfw3.SwapBuffers (hWnd);
						app.IsDirty = false;
					}
					Glfw3.PollEvents ();
				}
			}


			Glfw3.DestroyWindow (hWnd);
			Glfw3.Terminate ();
		}

		protected override void OnInitialized ()
		{
			base.OnInitialized ();
			registerGlfwCallbacks ();
			foreach (string s in System.Reflection.Assembly.GetExecutingAssembly ().GetManifestResourceNames ()) {
				Console.WriteLine (s);
			}

			Load ("#ui.helloworld.crow").DataSource = this;
		}
	}
}
