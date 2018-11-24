//
// DbgLogViewer.cs
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
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if DEBUG_LOG
namespace Crow
{
	public class DbgLogViewer : ScrollingObject
	{
		#region CTOR
		protected DbgLogViewer () : base(){}
		public DbgLogViewer (Interface iface) : base(iface){}
		#endregion

		bool loaded = false;
		double xScale = 0.125, yScale = 1.0, lineHeight = 0.0, leftMargin;
		string logFile;
		long currentTick = 0, selStart = -1, selEnd = -1, minTicks = 0, maxTicks = 0;

		public string LogFile {
			get { return logFile; }
			set {
				if (logFile == value)
					return;
				logFile = value;
				NotifyValueChanged ("LogFile", logFile);
				loaded = false;
				RegisterForGraphicUpdate ();
			}
		}
		public double XScale {
			get { return xScale; }
			set {
				if (xScale == value)
					return;
				xScale = value;
				NotifyValueChanged ("XScale", xScale);
				RegisterForGraphicUpdate ();
			}
		}
		public double YScale {
			get { return yScale; }
			set {
				if (yScale == value)
					return;
				yScale = value;
				NotifyValueChanged ("YScale", yScale);
				RegisterForGraphicUpdate ();
			}
		}
		List<DbgEvent> events;
		List<DbgGo> objs;

		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			if (!loaded) {
				if (!File.Exists (logFile))
					return;
				events = new List<DbgEvent>();
				objs = new List<DbgGo> ();

				using (StreamReader s = new StreamReader (logFile)) {
					if (s.ReadLine () != "[GraphicObjects]") {
						loaded = false;
						return;
					}
					while (!s.EndOfStream) {
						string l = s.ReadLine ();
						if (l == "[Events]")
							break;
						objs.Add (DbgGo.Parse (l));
					}
					while (!s.EndOfStream)
						events.Add (DbgEvent.Parse (s.ReadLine ()));						
				}
				loaded = true;
				lineHeight = gr.FontExtents.Height;
				if (events.Count > 0) {
					minTicks = events [0].begin;
					maxTicks = events [events.Count - 1].begin;
				} else
					minTicks = maxTicks = 0;
				
				MaxScrollY = Math.Max(0, objs.Count - (int)Math.Ceiling((double)ClientRectangle.Height / lineHeight));
			}

			Rectangle r = ClientRectangle;

			gr.LineWidth = 1.0;

			leftMargin = 0.0;

			foreach (DbgGo g in objs) {
				if (g.yIndex == 0)
					continue;
				double penX = r.X + g.xLevel * 5.0;
				Cairo.TextExtents te = gr.TextExtents (g.name);
				if (te.Width + penX > leftMargin)
					leftMargin = te.Width + penX; 
				double penY = (g.yIndex - ScrollY) * lineHeight + r.Top;
				if (penY < r.Top || penY > r.Bottom + lineHeight)
					continue;
			
				Foreground.SetAsSource (gr);
				gr.MoveTo (penX, penY - gr.FontExtents.Descent);
				gr.ShowText (g.name);
				gr.SetSourceColor (Crow.Color.Jet);
				gr.MoveTo (r.X, penY - 0.5);
				gr.LineTo (r.Right, penY - 0.5);
				gr.Stroke ();
			}

			leftMargin += 2.5;
			r.Left += (int)leftMargin;
			gr.MoveTo (leftMargin, r.Top);
			gr.LineTo (leftMargin, r.Bottom);
			Foreground.SetAsSource (gr);
			gr.Stroke ();

			MaxScrollX = (int)(maxTicks - minTicks) - r.Width;

			gr.SetFontSize (8);

			gr.Save ();
			gr.Rectangle (r);
			gr.Clip ();

			foreach (DbgEvent evt in events) {
				double x = (int)((double)(evt.begin - minTicks - ScrollX) * xScale) ;
				x += (double)r.Left + 0.5;
				double w = Math.Max (Math.Max(2.0, 2.0 * xScale), (double)(evt.end - evt.begin) * xScale);

				if (x + w < r.Left || x > r.Right)
					continue;

				if (evt.type.HasFlag (DbgEvtType.GraphicObject)) {
					gr.SetSourceColor (Crow.Color.DarkSlateBlue);
					DbgData data = (DbgData)evt.data;
					double y = (objs.Where(o=>o.index == data.obj).FirstOrDefault().yIndex - ScrollY - 1) * lineHeight;
					if (evt.type.HasFlag (DbgEvtType.GOLayouting)) {
						if (evt.type == DbgEvtType.GOProcessLayouting) {							
							switch (data.result) {
							case LayoutingQueueItem.Result.Success:
								gr.SetSourceColor (Crow.Color.Green);
								break;
							case LayoutingQueueItem.Result.Deleted:
								gr.SetSourceColor (Crow.Color.Red);
								break;
							case LayoutingQueueItem.Result.Discarded:
								gr.SetSourceColor (Crow.Color.DarkOrange);
								break;
							case LayoutingQueueItem.Result.Requeued:
								gr.SetSourceColor (Crow.Color.GreenYellow);
								break;
							case LayoutingQueueItem.Result.Register:
								gr.SetSourceColor (Crow.Color.Blue);
								break;
							}

						}else
							gr.SetSourceColor (Crow.Color.Bisque);
						
					}else if (evt.type == DbgEvtType.GOInitialization)
						gr.SetSourceColor (Crow.Color.Cyan);					
					else if (evt.type == DbgEvtType.GOClippingRegistration)
						gr.SetSourceColor (Crow.Color.BlueViolet);	
					else if (evt.type == DbgEvtType.GORegisterClip)
						gr.SetSourceColor (Crow.Color.Orange);
					else if (evt.type == DbgEvtType.GODraw)
						gr.SetSourceColor (Crow.Color.Pink);
					else if (evt.type == DbgEvtType.GORecreateCache)
						gr.SetSourceColor (Crow.Color.YellowGreen);
						
					gr.Rectangle (x, y, w, lineHeight);
					gr.Fill ();

					if (evt.type.HasFlag (DbgEvtType.GOLayouting)) {
						gr.SetSourceColor (Crow.Color.Black);
						switch (data.layout) {
						case LayoutingType.Height:
							gr.MoveTo (x + w * 0.5, y + 1);
							gr.LineTo (x + w * 0.5, y + lineHeight - 2);
							gr.Stroke ();
							break;
						case LayoutingType.Width:
							gr.MoveTo (x + 0.5, y + lineHeight * 0.5);
							gr.LineTo (x + w - 1.0, y + lineHeight * 0.5);
							gr.Stroke ();
							break;
						default:
							break;
						}
					}
				} else {
					gr.SetSourceColor (Crow.Color.Yellow);
					gr.MoveTo (x, r.Top);
					gr.LineTo (x, r.Bottom);
					gr.Stroke ();
					string s = evt.type.ToString () [10].ToString ();
					Cairo.TextExtents te = gr.TextExtents (s);
					gr.Rectangle (x, r.Top, te.Width, lineHeight);
					gr.Fill ();
					gr.MoveTo (x, r.Top + gr.FontExtents.Ascent);
					gr.SetSourceColor (Crow.Color.Jet);
					gr.ShowText (s);
				}
			}
			gr.Restore ();
		}
		public override void Paint (ref Cairo.Context ctx)
		{
			base.Paint (ref ctx);
			Rectangle r = new Rectangle(mousePos.X, 0, 1, Slot.Height);
			Rectangle cb = ContextCoordinates (r);

			ctx.Rectangle (cb);
			ctx.SetSourceColor (Color.CornflowerBlue);
			ctx.Fill();

			ctx.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			ctx.SetFontSize (Font.Size);
			ctx.FontOptions = Interface.FontRenderingOptions;
			ctx.Antialias = Interface.Antialias;

			ctx.MoveTo (cb.X, cb.Y + lineHeight);
			ctx.ShowText (currentTick.ToString ());

			if (selStart < 0)
				return;
			double selStartX = (double)(selStart - ScrollX - minTicks) * xScale + leftMargin;
			double selEndX = (selEnd >= 0) ? (double)(selEnd - ScrollX - minTicks) * xScale + leftMargin :
				(double)(currentTick - ScrollX - minTicks) * xScale + leftMargin;

			if (selStartX < selEndX) {
				cb.X = (int)selStartX;
				cb.Width = (int)(selEndX - selStartX);
			} else {
				cb.X = (int)selEndX;
				cb.Width = (int)(selStartX - selEndX);
			}
			ctx.Operator = Cairo.Operator.Add;
			cb.Width = Math.Max (1, cb.Width);
			ctx.Rectangle (cb);
			//ctx.SetSourceColor (Color.LightYellow);
			ctx.SetSourceColor (Color.Jet);
			ctx.Fill();
			ctx.Operator = Cairo.Operator.Over;

		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);
			switch (layoutType) {
			case LayoutingType.Width:
				
				break;
			case LayoutingType.Height:
				if (!loaded)
					break;
				MaxScrollY = objs.Count - (int)Math.Ceiling((double)ClientRectangle.Height / lineHeight);
				break;
			}
		}
		Point mousePos;
		void updateMouseLocalPos(Point mPos){
			Rectangle r = ScreenCoordinates (Slot);
			Rectangle cb = ClientRectangle;
			cb.Left += (int)leftMargin;
			mousePos = mPos - r.Position;

			mousePos.X = Math.Max(cb.X, mousePos.X);
			mousePos.X = Math.Min(cb.Right, mousePos.X);
			mousePos.Y = Math.Max(cb.Y, mousePos.Y);
			mousePos.Y = Math.Min(cb.Bottom, mousePos.Y);

			currentTick = (int)((double)Math.Max(0, mousePos.X - cb.X) / xScale) + minTicks + ScrollX;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			updateMouseLocalPos (e.Position);

			if (selStart >= 0 && IFace.Mouse.IsButtonDown(MouseButton.Left))
				selEnd = currentTick;

			RegisterForRedraw ();
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);

			selStart = currentTick;
			selEnd = -1;

			RegisterForRedraw ();
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			if (selStart == currentTick) {
				selStart = -1;
				selEnd = -1;
			}else
				selEnd = currentTick;
			RegisterForRedraw ();
		}

		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{			
			//base.onMouseWheel (sender, e);

			if (IFace.Keyboard.Shift)
				ScrollX -= (int)((double)(e.Delta * MouseWheelSpeed) / xScale);
			else if (IFace.Keyboard.Ctrl) {
				if (e.Delta > 0) {
					XScale *= 2.0;
				} else {
					XScale *= 0.5;
				}
				int cbx = ClientRectangle.X + (int)leftMargin;
				ScrollX = (int)(currentTick - (int)((double)Math.Max(0, mousePos.X - cbx) / xScale) - minTicks);
			}else
				ScrollY -= e.Delta * MouseWheelSpeed;
		}

		public override void onKeyDown (object sender, KeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			if (e.Key == Key.F3) {
				if (selEnd < 0)
					return;
				if (selEnd < selStart)
					zoom (selEnd, selStart);
				else
					zoom (selStart, selEnd);
				selEnd = selStart = -1;
			}
		}

		void zoom (long start, long end) {			
			ScrollX = (int)(start - minTicks);
			XScale = (ClientRectangle.Width - leftMargin)/(end - start);
		}
	}
}
#endif

