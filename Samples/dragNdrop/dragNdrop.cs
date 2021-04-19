#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif


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

		protected override void OnInitialized ()
		{
			Load ("#ui.test.crow").DataSource = this;
		}

        private void refreshGraphicTree (object sender, MouseButtonEventArgs e) {
            NotifyValueChanged ("GraphicTree", (object)null);
            NotifyValueChanged ("GraphicTree", GraphicTree);
        }

        Group startGroup = null;
        

        private void W_StartDrag (object sender, DragDropEventArgs e) {
			Rectangle r = e.DragSource.LastPaintedSlot;
            startGroup = e.DragSource.Parent as Group;

            Surface dragImg = surf.CreateSimilar (Content.ColorAlpha,
				r.Width, r.Height);
            using (Context gr = new Context(dragImg)) {
                gr.SetSource (e.DragSource.bmp, 0, 0);
                gr.Paint ();
            }
			CreateDragImage (dragImg, r);
            /*lock (UpdateMutex)
                startGroup.RemoveChild (e.DragSource);*/
        }
        private void W_EndDrag (object sender, DragDropEventArgs e) {
            lock (UpdateMutex)
                startGroup.AddChild (e.DragSource);            
        }

        private void W_DragEnter (object sender, DragDropEventArgs e) {
            lock (UpdateMutex)
                (e.DropTarget as Group).AddChild (e.DragSource);
        }
        private void W_DragLeave (object sender, DragDropEventArgs e) {
            lock (UpdateMutex)
                (e.DragSource.Parent as Group).RemoveChild (e.DragSource);
        }
        private void W_Drop (object sender, DragDropEventArgs e) {
            //(e.DropTarget as Group).AddChild (e.DragSource);            
        }


    }
}
