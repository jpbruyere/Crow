#define MONO_CAIRO_DEBUG_DISPOSE


using System;
using System.Runtime.InteropServices;
using OpenTK;
using OpenTK.Graphics.OpenGL;

using System.Diagnostics;

//using GGL;
using Crow;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.IO;


namespace testCrowOTK
{
	class CrowTest : OpenTKGameWindow
	{
		public CrowTest ()
			: base(800, 600,"Crow Test with OpenTK")
		{
			VSync = VSyncMode.Off;
			Interface.CurrentInterface = CrowInterface;
		}


		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			CrowInterface.LoadInterface ("#Tests.ui.test.crow").DataSource = this;	
		}

		[STAThread]
		static void Main ()
		{
			CrowTest win = new CrowTest ();
			win.Run (30);
		}
	}
}