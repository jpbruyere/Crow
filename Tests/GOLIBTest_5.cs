using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

using System.Diagnostics;

//using GGL;
using go;
using System.Threading;
using System.Collections.Generic;


namespace test
{
	class GOLIBTest_5 : OpenTKGameWindow
	{
		public GOLIBTest_5 ()
			: base(1024, 600,"test5")
		{}

		Container c;
		List<GraphicObject> gl = new List<GraphicObject>();
		List<Label> ll = new List<Label>();
		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			LoadInterface("Interfaces/test5.goml", out c);
			gl.Add (c.FindByName ("g0"));
			ll.Add (c.FindByName ("lab0")as Label);
		}

		int cpt;
		protected override void OnUpdateFrame (FrameEventArgs e)
		{
			if (cpt > 100)
				cpt = 0;
			else
				cpt++;

			foreach (Label l in ll) {
				l.Text = cpt.ToString ();
			}
			foreach (GraphicObject o in gl) {
				Color a = o.Background;
				if (a.A > 1)
					a.A = 0;
				else
					a.A += 0.05;
				//a.A = 0.5;
				o.Background = a;
				o.Width = cpt + 10;
			}
			base.OnUpdateFrame (e);
		}

		[STAThread]
		static void Main ()
		{
			using (GOLIBTest_5 win = new GOLIBTest_5( )) {
				win.Run (30.0);
			}
		}
	}
}