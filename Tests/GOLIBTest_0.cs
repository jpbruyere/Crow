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


namespace test6
{
	class GOLIBTest_0 : OpenTKGameWindow
	{
		public GOLIBTest_0 ()
			: base(1024, 600,"test")
		{}

		GraphicObject g;
		Label l;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);
			g = LoadInterface("Interfaces/test0.goml");
			l = g.FindByName ("labCpt") as Label;
		}

		void onUp (object sender, MouseButtonEventArgs e)
		{
			decimal tmp = 0;
			if (!decimal.TryParse (l.Text, out tmp))
				return;
			
			tmp += 1;
			l.Text = tmp.ToString ();
		}
		void onDown (object sender, MouseButtonEventArgs e)
		{
			decimal tmp = 0;
			if (!decimal.TryParse (l.Text, out tmp))
				return;

			tmp -= 1;
			l.Text = tmp.ToString ();
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