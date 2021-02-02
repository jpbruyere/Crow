// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crow.Cairo;

namespace Crow
{
	public class DbgLogViewer : ScrollingObject
	{
		public static Dictionary<DbgEvtType, Color> colors;

		public static Configuration colorsConf = new Configuration ("dbgcolor.conf");//, Interface.GetStreamFromPath("#Crow.dbgcolor.conf"));

		public static void reloadColors () {
			colors = new Dictionary<DbgEvtType, Color>();
			foreach (string n in colorsConf.Names) {
				DbgEvtType t = (DbgEvtType)Enum.Parse (typeof(DbgEvtType), n);
				Color c = colorsConf.Get<Color> (n);
				colors.Add (t, c);
			}

		}
		#region CTOR
		static DbgLogViewer() {
			reloadColors ();
		}
		protected DbgLogViewer () : base(){}
		public DbgLogViewer (Interface iface, string style = null) : base(iface, style){}
		#endregion

		FontExtents fe;

		double xScale = 1.0/1024.0, yScale = 1.0, leftMargin, topMargin = 0.0;
		DbgWidgetRecord curWidget, hoverWidget;
		DbgEvent curEvent, hoverEvent;

		List<DbgEvent> events = new List<DbgEvent> ();
		List<DbgWidgetRecord> widgets = new List<DbgWidgetRecord> ();

		public List<DbgEvent> Events {
			get => events;
			set {
				if (events == value)
					return;
				events = value;
				NotifyValueChanged (nameof (Events), events);
				if (events == null)
					return;

				maxTicks = 0;
				minTicks = long.MaxValue;
				foreach (DbgEvent e in events) {
					if (e.begin < minTicks)
						minTicks = e.begin;
					if (e.end > maxTicks)
						maxTicks = e.end;
				}

				visibleTicks = maxTicks - minTicks;
				XScale = (ClientRectangle.Width - leftMargin)/visibleTicks;
				ScrollX = 0;
				ScrollY = 0;

				RegisterForGraphicUpdate ();
			}
		}
		public List<DbgWidgetRecord> Widgets {
			get => widgets;
			set {
				if (widgets == value)
					return;
				widgets = value;
				NotifyValueChanged (nameof (Widgets), widgets);
				updateMargins ();
				updateMaxScrollX ();
				updateMaxScrollY ();
			}
		}
		public DbgWidgetRecord CurrentWidget {
			get => curWidget;
			set {
				if (curWidget == value)
					return;
				curWidget = value;
				NotifyValueChanged (nameof (CurrentWidget), curWidget);
			}
		}
		public DbgEvent CurrentEvent {
			get => curEvent;
			set {
				if (curEvent == value)
					return;
				if (curEvent != null)
					curEvent.IsSelected = false;
				curEvent = value;
				if (curEvent != null) {
					curEvent.IsSelected = true;
					if (curEvent is DbgWidgetEvent we) {
						//CurrentWidget = Widgets [we.InstanceIndex];
						currentLine = we.InstanceIndex;
					}
					currentTick = curEvent.begin;
					if (curEvent.begin > minTicks + ScrollX + visibleTicks ||
						curEvent.end < minTicks + ScrollX) {
						ScrollX = (int)(currentTick - visibleTicks / 2);
					}
				}
				NotifyValueChanged (nameof (CurrentEvent), curEvent);
				RegisterForRedraw ();
			}
		}
		public DbgWidgetRecord HoverWidget {
			get => hoverWidget;
			internal set {
				if (hoverWidget == value)
					return;
				hoverWidget = value;
				NotifyValueChanged (nameof (HoverWidget), hoverWidget);
			}
		}

		public DbgEvent HoverEvent {
			get => hoverEvent;
			set {
				if (hoverEvent == value)
					return;
				hoverEvent = value;
				NotifyValueChanged (nameof (HoverEvent), hoverEvent);
			}
		}

		long currentTick = 0, selStart = -1, selEnd = -1, minTicks = 0, maxTicks = 0, visibleTicks = 0;
		int currentLine = -1;
		int visibleLines = 1;
		Point mousePos;

		public double XScale {
			get { return xScale; }
			set {
				if (xScale == value)
					return;
				xScale = value;
				NotifyValueChanged ("XScale", xScale);
				updateVisibleTicks ();
				RegisterForGraphicUpdate ();
			}
		}
		public double YScale {
			get => yScale;
			set {
				if (yScale == value)
					return;
				yScale = value;
				NotifyValueChanged ("YScale", yScale);
				RegisterForGraphicUpdate ();
			}
		}
		public override Font Font {
			get { return base.Font; }
			set {
				base.Font = value;
				using (Context gr = new Context (IFace.surf)) {
					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);

					fe = gr.FontExtents;
				}
				updateMargins ();
			}
		}
		public override int ScrollY {
			get => base.ScrollY;
			set {
				base.ScrollY = value;

				if (widgets == null)
					return;

				Rectangle cb = ClientRectangle;
				cb.Left += (int)leftMargin;
				cb.Width -= (int)leftMargin;
				cb.Y += (int)topMargin;
				cb.Height -= (int)topMargin;

				if (mousePos.Y < cb.Top || mousePos.Y > cb.Bottom)
					currentLine = -1;
				else
					currentLine = (int)((double)(mousePos.Y - cb.Top) / fe.Height) + ScrollY;

				NotifyValueChanged ("CurrentLine", currentLine);
			}
		}

		void drawEvents (Context ctx, List<DbgEvent> evts)
		{
			if (evts == null || evts.Count == 0)
				return;
			Rectangle cb = ClientRectangle;

			foreach (DbgEvent evt in evts) {
				if (evt.end - minTicks <= ScrollX)
					continue;
				if (evt.begin - minTicks > ScrollX + visibleTicks)
					break;
				double penY = topMargin + ClientRectangle.Top;
				if (evt.type.HasFlag (DbgEvtType.Widget)) {
					DbgWidgetEvent eW = evt as DbgWidgetEvent;
					int lIdx = eW.InstanceIndex - ScrollY;
					if (lIdx < 0 || lIdx > visibleLines)
						continue;
					penY += (lIdx) * fe.Height; 
				
					ctx.SetSource (evt.Color);

					double x = xScale * (evt.begin - minTicks - ScrollX);
					double w = Math.Max (Math.Max (2.0, 2.0 * xScale), (double)(evt.end - evt.begin) * xScale);
					if (x < 0.0) {
						w += x;
						x = 0.0;
					}
					x += leftMargin + cb.Left;
					double rightDiff = x + w - cb.Right;
					if (rightDiff > 0)
						w -= rightDiff;

					ctx.Rectangle (x, penY, w, fe.Height);
					ctx.Fill ();
				} else {
					/*double x = xScale * (evt.begin - minTicks - ScrollX);
					x += leftMargin + cb.Left;

					double trunc = Math.Truncate (x);
					if (x - trunc > 0.5)
						x = trunc + 0.5;
					else
						x = trunc - 0.5;


					ctx.SetSource (Colors.Yellow);
					ctx.MoveTo (x, penY);
					ctx.LineTo (x, cb.Bottom);
					ctx.Stroke ();
					string s = evt.type.ToString () [5].ToString ();
					TextExtents te = ctx.TextExtents (s);
					ctx.Rectangle (x - 0.5 * te.Width, penY - te.Height, te.Width, te.Height);
					ctx.Fill ();
					ctx.MoveTo (x - 0.5 * te.Width, penY - ctx.FontExtents.Descent);
					ctx.SetSource (Colors.Jet);
					ctx.ShowText (s);*/

				}
				drawEvents (ctx, evt.Events);
			}
		}

		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Cairo.Antialias.None;

			if (widgets == null)
				return;

			gr.LineWidth = 1.0;

			Rectangle cb = ClientRectangle;

			double penY = topMargin + ClientRectangle.Top;

			for (int i = 0; i < visibleLines; i++) {
				if (i + ScrollY >= widgets.Count)
					break;
				int gIdx = i + ScrollY;
				DbgWidgetRecord g = widgets [gIdx];

				penY += fe.Height;

				gr.SetSource (Crow.Colors.Jet);
				gr.MoveTo (cb.X, penY - 0.5);
				gr.LineTo (cb.Right, penY - 0.5);
				gr.Stroke ();

				double penX = 5.0 * g.xLevel + cb.Left;

				if (g.yIndex == 0)
					gr.SetSource (Crow.Colors.LightSalmon);
				else
					Foreground.SetAsSource (IFace, gr);

				gr.MoveTo (penX, penY - gr.FontExtents.Descent);
				gr.ShowText (g.name + gIdx);
			}

			drawEvents (gr, events);
			/*
			for (int i = 0; i < visibleLines; i++) { 
				foreach (DbgEvent evt in events) {
					if (evt.end - minTicks <= ScrollX)
						continue;
					if (evt.begin - minTicks > ScrollX + visibleTicks)
						break;
					
					
				}

				
			}
			*/

			gr.MoveTo (cb.Left, topMargin - 0.5 + cb.Top);
			gr.LineTo (cb.Right, topMargin - 0.5 + cb.Top);

			gr.MoveTo (leftMargin + cb.Left, cb.Top);
			gr.LineTo (leftMargin + cb.Left, cb.Bottom);
			gr.SetSource (Crow.Colors.Grey);

			penY = topMargin + ClientRectangle.Top;

			//graduation
			int largeGrad = int.Parse ("1" + new string ('0', visibleTicks.ToString ().Length - 1));
			int smallGrad = Math.Max (1, largeGrad / 10);

			long firstVisibleTicks = minTicks + ScrollX;
			long curGrad = firstVisibleTicks - firstVisibleTicks % smallGrad + smallGrad;

			long gg = curGrad - ScrollX - minTicks;
			while (gg < visibleTicks ) {
				double x = (double)gg * xScale + leftMargin + cb.Left;

				gr.MoveTo (x, penY - 0.5);
				if (curGrad % largeGrad == 0) { 
					gr.LineTo (x, penY - 8.5);
					string str = curGrad.ToString ();
					TextExtents te = gr.TextExtents (str);
					gr.RelMoveTo (-0.5 * te.Width, -2.0);
					gr.ShowText (str);
				}else
					gr.LineTo (x, penY - 2.5);

				curGrad += smallGrad;
				gg = curGrad - ScrollX - minTicks;
			}

			gr.Stroke ();

			//global events
/*			foreach (DbgEvent evt in events) {
				if (evt.begin - minTicks <= ScrollX)
					continue;
				double x = xScale * (evt.begin - minTicks - ScrollX) ;
				x += leftMargin + cb.Left;


			}*/

		}
		public override void Paint (Cairo.Context ctx)
		{
			base.Paint (ctx);

			Rectangle r = new Rectangle(mousePos.X, 0, 1, Slot.Height);
			Rectangle ctxR = ContextCoordinates (r);
			Rectangle cb = ClientRectangle;
			ctx.LineWidth = 1.0;
			double x = xScale * (currentTick - minTicks - ScrollX) + leftMargin;
			if (x - Math.Truncate (x) > 0.5)
				x = Math.Truncate (x) + 0.5;
			else
				x = Math.Truncate (x) - 0.5;
			ctx.MoveTo (x, cb.Top + topMargin - 4.0);
			ctx.LineTo (x, cb.Bottom);

			//ctx.Rectangle (ctxR);
			ctx.SetSource (Colors.CornflowerBlue);
			ctx.Stroke();

			ctx.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			ctx.SetFontSize (Font.Size);
			ctx.FontOptions = Interface.FontRenderingOptions;
			ctx.Antialias = Interface.Antialias;

			ctx.MoveTo (ctxR.X - ctx.TextExtents (currentTick.ToString ()).Width / 2, ctxR.Y + fe.Height);
			ctx.ShowText (currentTick.ToString ());

			ctx.Operator = Cairo.Operator.Add;

			if (currentLine >= 0) {
				double y = fe.Height * (currentLine - ScrollY) + topMargin + cb.Top;
				r = new Rectangle (cb.Left,  (int)y, cb.Width, (int)fe.Height);

				ctx.Operator = Cairo.Operator.Add;
				ctx.SetSource (0.1, 0.1, 0.1, 0.4);
				ctx.Rectangle (ContextCoordinates (r));
				ctx.Fill ();
			}

			if (CurrentWidget != null) {

			}

			if (selStart < 0 || selEnd < 0) {
				ctx.Operator = Cairo.Operator.Over;
				return;
			}
			double selStartX = (double)(selStart - ScrollX - minTicks) * xScale + leftMargin + cb.Left;
			double selEndX = (double)(selEnd - ScrollX - minTicks) * xScale + leftMargin + cb.Left;

			if (selStartX < selEndX) {
				ctxR.X = (int)selStartX;
				ctxR.Width = (int)(selEndX - selStartX);
			} else {
				ctxR.X = (int)selEndX;
				ctxR.Width = (int)(selStartX - selEndX);
			}

			ctxR.Width = Math.Max (1, ctxR.Width);
			ctx.Rectangle (ctxR);
			//ctx.SetSourceColor (Color.LightYellow);
			ctx.SetSource (Colors.Jet);
			ctx.Fill();
			ctx.Operator = Cairo.Operator.Over;

		}
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);
			switch (layoutType) {
			case LayoutingType.Width:
				if (xScale < 0) {
					visibleTicks = maxTicks - minTicks;
					XScale = (ClientRectangle.Width - leftMargin) / visibleTicks;
				}
				updateVisibleTicks ();
				break;
			case LayoutingType.Height:
				updateVisibleLines ();
				break;
			}
		}

		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			currentLine = -1;
			currentTick = 0;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			long lastTick = currentTick;
			updateMouseLocalPos (e.Position);

			if (IFace.IsDown (Glfw.MouseButton.Left) && selStart >= 0)
				selEnd = currentTick;
			else if (IFace.IsDown(Glfw.MouseButton.Right)) {
				ScrollX += (int)(lastTick - currentTick);
				updateMouseLocalPos (e.Position);
			} else {
				HoverWidget = (currentLine < 0 || currentLine >= widgets.Count) ? null : widgets [currentLine];
				HoverEvent = hoverWidget?.Events.FirstOrDefault (ev => ev.begin <= currentTick && ev.end >= currentTick);
			}

			if (RegisteredLayoutings == LayoutingType.None && !IsDirty)
				IFace.EnqueueForRepaint (this);
			
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);

			if (e.Button == Glfw.MouseButton.Left) {
				CurrentWidget = hoverWidget;
				CurrentEvent = hoverEvent;
				selStart = currentTick;
				selEnd = -1;
			}

			RegisterForRedraw ();
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			if (e.Button == Glfw.MouseButton.Left && selEnd > 0 && selEnd != selStart) {
				long scrX = 0;
				if (selStart < selEnd) {
					visibleTicks = selEnd - selStart;
					scrX = selStart - minTicks;
				} else {
					visibleTicks = selStart - selEnd;
					scrX = selEnd - minTicks;
				}
				XScale = (ClientRectangle.Width - leftMargin) / visibleTicks;
				ScrollX = (int)scrX;
			}
			selStart = -1;
			selEnd = -1;

			RegisterForRedraw ();
		}

		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{			
			base.onMouseWheel (sender, e);

			if (IFace.Shift)
				ScrollX -= (int)((double)(e.Delta * MouseWheelSpeed) / xScale);
			else if (IFace.Ctrl) {
				if (e.Delta > 0) {
					XScale *= 2.0;
				} else {
					if (MaxScrollX > 0)
						XScale *= 0.5;
				}
				ScrollX = (int)(currentTick - (int)((double)Math.Max(0, mousePos.X - (int)leftMargin) / xScale) - minTicks);
			}else
				ScrollY -= e.Delta * MouseWheelSpeed;
		}

		public override void onKeyDown (object sender, KeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			if (e.Key == Glfw.Key.F3) {
				if (selEnd < 0)
					return;
				if (selEnd < selStart)
					zoom (selEnd, selStart);
				else
					zoom (selStart, selEnd);
				selEnd = selStart = -1;
			}
		}

		void updateMargins ()
		{
			leftMargin = topMargin = 0.0;

			if (widgets == null)
				return;

			using (Context gr = new Context (IFace.surf)) {
				double maxNameWidth = 0.0;

				gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				gr.SetFontSize (Font.Size);

				foreach (DbgWidgetRecord o in widgets) {
					double nameWidth = gr.TextExtents (o.name).Width + 5.0 * o.xLevel;
					if (nameWidth > maxNameWidth)
						maxNameWidth = nameWidth;
				}

				leftMargin = 10.5 + maxNameWidth;
				topMargin = 2.0 * fe.Height;

				RegisterForGraphicUpdate ();
			}
		}

		void updateVisibleLines ()
		{
			visibleLines = fe.Height < 1 ? 1 : (int)Math.Floor (((double)ClientRectangle.Height - topMargin) / fe.Height);
			NotifyValueChanged ("VisibleLines", visibleLines);
			updateMaxScrollY ();
		}
		void updateVisibleTicks ()
		{
			visibleTicks = Math.Max (0, (long)((double)(ClientRectangle.Width - leftMargin) / XScale));
			NotifyValueChanged ("VisibleTicks", visibleTicks);
			updateMaxScrollX ();
		}

		void updateMaxScrollX ()
		{
			if (widgets == null)
				MaxScrollX = 0;
			else
				MaxScrollX = (int)Math.Max (0L, maxTicks - minTicks - visibleTicks);
		}
		void updateMaxScrollY ()
		{
			if (widgets == null)
				MaxScrollY = 0;
			else
				MaxScrollY = Math.Max (0, widgets.Count - visibleLines);
		}

		void updateMouseLocalPos (Point mPos)
		{
			Rectangle r = ScreenCoordinates (Slot);
			Rectangle cb = ClientRectangle;
			cb.Left += (int)leftMargin;
			cb.Width -= (int)leftMargin;
			cb.Y += (int)topMargin;
			cb.Height -= (int)topMargin;

			mousePos = mPos - r.Position;

			mousePos.X = Math.Max (cb.X, mousePos.X);
			mousePos.X = Math.Min (cb.Right, mousePos.X);

			if (mousePos.Y < cb.Top || mousePos.Y > cb.Bottom)
				currentLine = -1;
			else
				currentLine = (int)((double)(mousePos.Y - cb.Top) / fe.Height) + ScrollY;

			NotifyValueChanged ("CurrentLine", currentLine);

			mousePos.Y = Math.Max (cb.Y, mousePos.Y);
			mousePos.Y = Math.Min (cb.Bottom, mousePos.Y);

			currentTick = (int)((double)(mousePos.X - cb.X) / xScale) + minTicks + ScrollX;
			RegisterForRedraw ();
		}
		void zoom (long start, long end) {						
			//Rectangle cb = ClientRectangle;
			//cb.X += (int)leftMargin;
			XScale = ((double)ClientRectangle.Width - leftMargin)/(end - start);
			ScrollX = (int)(start - minTicks);
		}
	}
}


