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


namespace test3
{
	class GOLIBTest_HStack : OpenTKGameWindow
	{
		public GOLIBTest_HStack ()
			: base(1024, 600,"test")
		{}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			LoadInterface("Interfaces/testHStack.goml");

		}			

		[STAThread]
		static void Main ()
		{
			Console.WriteLine ("starting example");

			using (GOLIBTest_HStack win = new GOLIBTest_HStack( )) {
				win.Run (30.0);
			}
		}
	}
}