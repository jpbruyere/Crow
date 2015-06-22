#define MONO_CAIRO_DEBUG_DISPOSE
#define DEBUG_CLIP_RECTANGLE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;


namespace test2
{
	class GOLIBTest_Scrollbar : OpenTKGameWindow
	{
		public GOLIBTest_Scrollbar ()
			: base(1024, 600,"test")
		{}

		Group g;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("Interfaces/testScrollbar.goml", out g);

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

			using (GOLIBTest_Scrollbar win = new GOLIBTest_Scrollbar( )) {
				win.Run (30.0);
			}
		}
	}
}