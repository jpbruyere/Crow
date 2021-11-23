//Tutorial
using Crow;

using Glfw;

namespace TestWidget {
	public class TestWidget : Widget {
		protected override void onDraw (IContext gr) {
			base.onDraw (gr);
			gr.SetSource (myColor);
			gr.Rectangle (ClientRectangle);
			gr.Fill();
		}
		Color myColor = Colors.Green;
		public override void onMouseEnter(object sender, MouseMoveEventArgs e) {
			base.onMouseEnter (sender, e);
			myColor = Colors.Chartreuse;
			RegisterForRedraw ();
		}
		public override void onMouseLeave(object sender, MouseMoveEventArgs e) {
			base.onMouseLeave (sender, e);
			myColor = Colors.Green;
			RegisterForRedraw ();
		}
	}
}