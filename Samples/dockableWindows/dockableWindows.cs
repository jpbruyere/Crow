using Crow;
using Glfw;
using Samples;

namespace tests
{
	public class BasicTests : SampleBase
	{
		static void Main ()
		{
			using (BasicTests app = new BasicTests ()) {
				app.Run ();
			}
		}

		Color[] colors =
		{
			Colors.Blue,
			Colors.Red,
			Colors.Green,
			Colors.DarkOrchid,
			Colors.DarkOrange,
			Colors.DarkOliveGreen,
			Colors.Cyan,
			Colors.Chartreuse,			
		};
		DockStack mainStack;
		protected override void OnInitialized () {
			Load ("#ui.test.crow").DataSource = this;
			mainStack = FindByName ("mainDock") as DockStack;
			AddWidget (new DockWindow (this) { Background = Colors.Blue, Left = 10, Top = 110, Resizable = true, Caption = "win1" });
			AddWidget (new DockWindow (this) { Background = Colors.Red, Left = 30, Top = 130, Resizable = true, Caption = "win2" });
			AddWidget (new DockWindow (this) { Background = Colors.Green, Left = 50, Top = 150, Resizable = true, Caption = "win3" });
			AddWidget (new DockWindow (this) { Background = Colors.BurlyWood, Left = 70, Top = 170, Resizable = true, Caption = "win4" });
			AddWidget (new DockWindow (this) { Background = Colors.DarkOrchid, Left = 90, Top = 190, Resizable = true, Caption = "win5" });
		}
		private void refreshGraphicTree (object sender, MouseButtonEventArgs e) {
			NotifyValueChanged ("GraphicTree", (object)null);
			NotifyValueChanged ("GraphicTree", GraphicTree);
		}
		int colorIdx = 0;
		int nameIdx = 6;
        public override bool OnKeyDown (Key key) {
			if (colorIdx >= colors.Length)
				colorIdx = 0;
            switch (key) {
			case Key.F6:
				AddWidget (new DockWindow (this) { Background = colors[colorIdx++], Left = 90, Top = 190, Resizable = true, Caption = $"win{nameIdx++}" });
				return true;
			case Key.F2:
				Configuration.Global.Set<string> ("WindowConfigTest", mainStack.ExportConfig());
				return true;
			case Key.F3:
				mainStack.ImportConfig (Configuration.Global.Get<string> ("WindowConfigTest"));
				return true;
			default:
				return base.OnKeyDown (key);
			}			
        }
    }
}
