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
	class GOLIBTest_0 : OpenTKGameWindow
	{
		public GOLIBTest_0 ()
			: base(1024, 600,"test")
		{}

		GraphicObject g;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("Interfaces/test0.goml", out g);

		}
		protected override void OnRenderFrame (FrameEventArgs e)
		{
			GL.Clear (ClearBufferMask.ColorBufferBit);
			base.OnRenderFrame (e);
			SwapBuffers ();
		}
		protected override void OnKeyDown (KeyboardKeyEventArgs e)
		{
			switch (e.Key) {
			case Key.Left:
				g.Left++;
				break;
			case Key.Right:
				g.Left--;
				break;
			case Key.Up:
				g.Top--;
				break;
			case Key.Down:
				g.Top++;
				break;
			default:
				break;
			}
		}
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_0 win = new GOLIBTest_0( )) {
				win.Run (30.0);
			}
		}
	}
}