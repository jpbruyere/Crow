using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
            startGroup = e.DragSource.Parent as Group;
            DragImageHeight = e.DragSource.LastPaintedSlot.Height;
            DragImageWidth = e.DragSource.LastPaintedSlot.Width;

            Crow.Cairo.Surface dragImg = surf.CreateSimilar (Crow.Cairo.Content.ColorAlpha, DragImageWidth, DragImageHeight);
            using (Crow.Cairo.Context gr = new Crow.Cairo.Context(dragImg)) {
                gr.SetSource (e.DragSource.bmp, 0, 0);
                gr.Paint ();
            }
            DragImage = dragImg;
            lock (UpdateMutex)
                startGroup.RemoveChild (e.DragSource);
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
