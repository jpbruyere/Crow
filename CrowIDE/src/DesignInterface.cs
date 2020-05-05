// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Crow.Cairo;

namespace Crow.Coding
{
	public class DesignInterface : Interface
	{
		public DesignInterface () : base (100, 100, false, false)
		{

			surf = new ImageSurface (Format.Argb32, 100, 100);

			loadStyling ();
		}

		public override void InterfaceThread ()
		{

			//running = true;
			//while (running) {
			//	Update ();
			//	Thread.Sleep (5);
			//}
		}

		public ProjectFileNode ProjFile;


		public override Widget CreateInstance (string path)
		{
			ProjectFileNode pi;

			if (ProjFile.Project.solution.GetProjectFileFromPath (path, out pi))
				return CreateITorFromIMLFragment (pi.Source).CreateInstance();					
		
			return null;
		}
		public override Stream GetStreamFromPath (string path)
		{
			ProjectFileNode pi;
			if (ProjFile.Project.solution.GetProjectFileFromPath (path, out pi)) {
				return new FileStream (pi.FullPath, FileMode.Open);	
			}
			throw new Exception ($"In Design File not found: {path}");
		}


		public override void ProcessResize (Rectangle bounds)
		{
			if (bounds == clientRectangle)
				return;
			lock (UpdateMutex) {
				clientRectangle = bounds;
				surf.Dispose ();
				surf = new ImageSurface (Format.Argb32, clientRectangle.Width, clientRectangle.Height);

				foreach (Widget g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				RegisterClip (clientRectangle);
			}

		}
		public override bool OnMouseMove (int x, int y)
		{
			int deltaX = x - MousePosition.X;
			int deltaY = y - MousePosition.Y;
			MousePosition = new Point (x, y);
			MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);

			if (ActiveWidget != null) {
				//TODO, ensure object is still in the graphic tree
				//send move evt even if mouse move outside bounds
				ActiveWidget.onMouseMove (this, e);
				if (!ActiveWidget.IsDragged)//if active is dragged, process mouse move as it was not visible.
					return true;
			}

			if (HoverWidget != null) {
				
				//check topmost graphicobject first
				Widget tmp = HoverWidget;
				Widget topc = null;
				while (tmp is Widget) {
					topc = tmp;
					tmp = tmp.LogicalParent as Widget;
				}
				int idxhw = GraphicTree.IndexOf (topc);
				if (idxhw != 0) {
					int i = 0;
					while (i < idxhw) {
						if (GraphicTree [i].localLogicalParentIsNull) {
							if (GraphicTree [i].MouseIsIn (e.Position)) {
								while (HoverWidget != null) {
									HoverWidget.onMouseLeave (HoverWidget, e);
									HoverWidget = HoverWidget.LogicalParent as Widget;
								}

								GraphicTree [i].checkHoverWidget (e);
								return true;
							}
						}
						i++;
					}
				}

				if (HoverWidget.MouseIsIn (e.Position)) {
					if (!(HoverWidget is TemplatedControl))
						HoverWidget.checkHoverWidget (e);
					return true;
				} else {
					HoverWidget.onMouseLeave (HoverWidget, e);
					//seek upward from last focused graph obj's
					while (HoverWidget.LogicalParent as Widget != null) {
						HoverWidget = HoverWidget.LogicalParent as Widget;
						if (HoverWidget.MouseIsIn (e.Position)) {
							HoverWidget.checkHoverWidget (e);
							return true;
						} else
							HoverWidget.onMouseLeave (HoverWidget, e);
					}
				}
			}

			//top level graphic obj's parsing
			lock (GraphicTree) {
				for (int i = 0; i < GraphicTree.Count; i++) {
					Widget g = GraphicTree [i];
					if (g.MouseIsIn (e.Position)) {
						if (!(HoverWidget is TemplatedControl))
							g.checkHoverWidget (e);
						if (g is Window)
							PutOnTop (g);
						return true;
					}
				}
			}
			HoverWidget = null;
			return false;		
		}
	
		protected override void processLayouting ()
		{
			#if MEASURE_TIME
			layoutingMeasure.StartCycle();
			#endif

			if (Monitor.TryEnter (LayoutMutex)) {
				DiscardQueue = new Queue<LayoutingQueueItem> ();
				LayoutingQueueItem lqi;
				while (LayoutingQueue.Count > 0) {
					lqi = LayoutingQueue.Dequeue ();
					//Console.WriteLine (lqi.ToString ());
					#if DEBUG_LAYOUTING
					currentLQI = lqi;
					curLQIsTries.Add(currentLQI);
					#endif
					lqi.ProcessLayouting ();
				}
				LayoutingQueue = DiscardQueue;
				Monitor.Exit (LayoutMutex);
				DiscardQueue = null;
			}

			#if MEASURE_TIME
			layoutingMeasure.StopCycle();
			#endif
		}
	}
}

