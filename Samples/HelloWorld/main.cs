using System;
using Crow;

namespace HelloWorld
{
	class Program : Interface {
		Command CMDQuit;
		static void Main (string[] args) {
			using (Program vke = new Program ()) {
				vke.Run ();
			}
		}
		protected override void Startup ()
		{
			CMDQuit = new Command (new Action (Quit)) { Caption = "Quit", Icon = new SvgPicture ("#Crow.Icons.exit-symbol.svg") };

			Widget w = Load ("#HelloWorld.helloworld.crow");
			w.KeyPress += W_KeyPress;
			w.DataSource = this;
		}

		Color [] colors = { Color.Blue, Color.DarkGoldenRod, Color.Red, Color.Azure, Color.Brown, Color.Black, Color.White, Color.Pink };
		int ptr = 0;

		void W_KeyPress (object sender, KeyPressEventArgs e)
		{
			switch (e.KeyChar) {
			case 'w':
				LoadIMLFragment ($"<DockWindow Name='win{ptr}' Left='450' Top='450' Width='150' Height='150' Background='{colors [ptr]}'/>");
				break;
			case 'x':
				LoadIMLFragment ($"<Window Name='win{ptr}' Left='450' Top='450' Width='150' Height='150' Background='{colors [ptr]}'/>");
				break;
			}

			ptr++;
			if (ptr == colors.Length)
				ptr = 0;
		}

	}
}
