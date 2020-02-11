using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using Crow;
using Crow.IML;

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
			CMDQuit = new Command (new Action (() => running = false)) { Caption = "Quit", Icon = new SvgPicture ("#Crow.Icons.exit-symbol.svg") };

			Widget w = Load ("#HelloWorld.helloworld.crow");
			w.KeyPress += W_KeyPress;
			w.DataSource = this;
		}

		public SolidColor testColor = Color.Red;
		public SolidColor TestColor {
			get => testColor;
			set {
				if (testColor == value)
					return;
				testColor = value;
				NotifyValueChanged ("TestColor", testColor);
			}
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
		public override bool OnKeyDown (Key key)
		{
			switch (key) {
			case Key.d:
				dump ();
				break;
			case Key.space:
				TestColor = Color.Green;
				break;
			default:
				return base.OnKeyDown (key);
			}
			return true;
		}

		void dump ()
		{
			Instantiator inst = Instantiators ["#HelloWorld.helloworld.crow"];

			foreach (Delegate cd in inst.CachedDelegates) {
				FieldInfo mb = typeof(Delegate).GetField("original_method_info", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField);

				DynamicMethod dynMethod = mb.GetValue (cd) as DynamicMethod;
				/*Console.WriteLine (typeof (ILGenerator).GetFields (BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.GetField));

				var ilgen = dynMethod.GetILGenerator ();
				 


*/

				MethodBody body = dynMethod.GetMethodBody ();

				byte [] il = body.GetILAsByteArray ();
				foreach (byte b in il) {
					Console.Write ($"{b:x2} ");
					//Console.WriteLine (b);
				}

			}

		}

	}
}
