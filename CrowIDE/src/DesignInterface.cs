//
// DesignInterface.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
using System;
using Crow;
using System.Globalization;
using System.Threading;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Cairo;

namespace Crow.Coding
{
	public class DesignInterface : Interface, IValueChange
	{
		#region IValueChange implementation
		/// <summary>
		/// Raise to notify that the value of a property has changed, the binding system
		/// rely mainly on this event. the member name may not be present in the class, this is 
		/// used in **propertyless** bindings, this allow to raise custom named events without needing
		/// to create an new one in the class or a new property.
		/// </summary>
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		/// <summary>
		/// Helper function to raise the value changed event
		/// </summary>
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			//Debug.WriteLine ("Value changed: {0}->{1} = {2}", this, MemberName, _value);
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		public DesignInterface () : base()
		{
		}

		public ProjectFile ProjFile;


		protected override void InitBackend ()
		{
			surf = new ImageSurface(Format.Argb32, 100, 100);
		}
		public override void ProcessResize (Rectangle bounds)
		{
			if (bounds == clientRectangle)
				return;
			lock (UpdateMutex) {
				clientRectangle = bounds;
				surf.Dispose ();
				surf = new ImageSurface(Format.Argb32, clientRectangle.Width, clientRectangle.Height);

				foreach (GraphicObject g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);

				RegisterClip (clientRectangle);
			}

		}
		public override GraphicObject CreateInstance (string path)
		{
			ProjectFile pi;

			if (ProjFile.Project.solution.GetProjectFileFromPath (path, out pi))
				return CreateITorFromIMLFragment (pi.Source).CreateInstance();					
		
			return null;
		}
		public override System.IO.Stream GetStreamFromPath (string path)
		{
			ProjectFile pi;
			if (ProjFile.Project.solution.GetProjectFileFromPath (path, out pi)) {
				return new FileStream (pi.AbsolutePath, FileMode.Open);	
			}
			throw new Exception ($"In Design File not found: {path}");
		}
		public override bool ProcessMouseMove (int x, int y)
		{
			int deltaX = x - Mouse.X;
			int deltaY = y - Mouse.Y;
			Mouse.X = x;
			Mouse.Y = y;
			MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);
			e.Mouse = Mouse;

			if (ActiveWidget != null) {
				//TODO, ensure object is still in the graphic tree
				//send move evt even if mouse move outside bounds
				ActiveWidget.onMouseMove (this, e);
				if (!ActiveWidget.IsDragged)//if active is dragged, process mouse move as it was not visible.
					return true;
			}

			if (HoverWidget != null) {
				
				//check topmost graphicobject first
				GraphicObject tmp = HoverWidget;
				GraphicObject topc = null;
				while (tmp is GraphicObject) {
					topc = tmp;
					tmp = tmp.LogicalParent as GraphicObject;
				}
				int idxhw = GraphicTree.IndexOf (topc);
				if (idxhw != 0) {
					int i = 0;
					while (i < idxhw) {
						if (GraphicTree [i].localLogicalParentIsNull) {
							if (GraphicTree [i].MouseIsIn (e.Position)) {
								while (HoverWidget != null) {
									HoverWidget.onMouseLeave (HoverWidget, e);
									HoverWidget = HoverWidget.LogicalParent as GraphicObject;
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
					while (HoverWidget.LogicalParent as GraphicObject != null) {
						HoverWidget = HoverWidget.LogicalParent as GraphicObject;
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
					GraphicObject g = GraphicTree [i];
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

