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
	class GOLIBTest_1 : OpenTKGameWindow
	{
		public GOLIBTest_1 ()
			: base(1024, 600,"test")
		{}

		Container g;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("Interfaces/log.xml", out g);

		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear (ClearBufferMask.ColorBufferBit);
			base.OnRenderFrame (e);
			SwapBuffers ();
		}

		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_1 win = new GOLIBTest_1( )) {
				win.Run (30.0);
			}
		}
	}
}