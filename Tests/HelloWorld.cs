using System;
using Crow;

namespace Tests
{
	class HelloWorld : CrowWindow
	{
		public HelloWorld ()
			: base(800, 600,"Crow Test with OpenTK")
		{
		}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			CrowInterface.AddWidget(new Label("Hello World"));
		}

		[STAThread]
		static void Main ()
		{
			HelloWorld win = new HelloWorld ();
			win.Run (30);
		}
	}
}