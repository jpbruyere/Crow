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
	class GOLIBTest_stack : OpenTKGameWindow
	{
		public GOLIBTest_stack ()
			: base(1024, 600,"test")
		{}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("Interfaces/test_stack.goml");

		}

		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			base.OnUpdateFrame (e);
		}

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_stack win = new GOLIBTest_stack( )) {
				win.Run (30.0);
			}
		}
	}
}