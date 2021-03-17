using Crow;
using Glfw;

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
			Colors.Yellow,
			Colors.Grey,
			Colors.Onyx,
			Colors.Cyan,
			Colors.Chartreuse,			
		};
		protected override void OnInitialized () {
			Load ("#ui.test.crow").DataSource = this;
			AddWidget (new DockWindow (this) { Background = Colors.Blue, Left = 10, Top = 110, Resizable = true });
			AddWidget (new DockWindow (this) { Background = Colors.Red, Left = 30, Top = 130, Resizable = true });
			AddWidget (new DockWindow (this) { Background = Colors.Green, Left = 50, Top = 150, Resizable = true });
			AddWidget (new DockWindow (this) { Background = Colors.Yellow, Left = 70, Top = 170, Resizable = true });
			AddWidget (new DockWindow (this) { Background = Colors.Grey, Left = 90, Top = 190, Resizable = true });
		}
		private void refreshGraphicTree (object sender, MouseButtonEventArgs e) {
			NotifyValueChanged ("GraphicTree", (object)null);
			NotifyValueChanged ("GraphicTree", GraphicTree);
		}
		int colorIdx = 0;
        public override bool OnKeyDown (Key key) {
			if (colorIdx >= colors.Length)
				colorIdx = 0;
            switch (key) {
			case Key.F6:
				AddWidget (new DockWindow (this) { Background = colors[colorIdx++], Left = 90, Top = 190, Resizable = true });
				return true;
			default:
				return base.OnKeyDown (key);
			}			
        }
    }
}
