#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;


namespace test
{
	class GOLIBTest_3 : OpenTKGameWindow
	{
		public GOLIBTest_3 ()
			: base(1024, 600,"test")
		{}

		GraphicObject g;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			g = LoadInterface<Container>("Interfaces/test3.xml");

		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear (ClearBufferMask.ColorBufferBit);
			base.OnRenderFrame (e);
			SwapBuffers ();
		}
			

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_3 win = new GOLIBTest_3( )) {
				win.Run (30.0);
			}
		}
	}
}