using System.Diagnostics;
using System.Runtime.InteropServices.ComTypes;
// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Crow.Cairo;
using Crow.DebugLogger;
using DebugLogAnalyzer;

namespace Crow
{
	public class DbgLogViewer : Widget
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
			//reloadColors ();
			
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
		

		public DbgEvtType Filter {
			get => Configuration.Global.Get<DbgEvtType> ("DbgLogViewFilter");
			set {
				if (Filter == value)
					return;				
				Configuration.Global.Set ("DbgLogViewFilter", value);				
				NotifyValueChangedAuto(Filter);
				RegisterForGraphicUpdate();
			}
		}
		public List<DbgEvent> Events {
			get => events;
			set {
				if (events == value)
					return;
				events = value;
				NotifyValueChanged (nameof (Events), events);

				maxTicks = minTicks = 0;
				if (events != null && events.Count > 0) {				
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
				} else {
					maxTicks = 1;
					XScale = 1.0/1024.0;					
				}


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
				if (CurrentWidget == null)
					return;
				if (CurrentWidget.listIndex < scrollY || CurrentWidget.listIndex > scrollY + visibleLines)
					ScrollY = CurrentWidget.listIndex - (visibleLines / 2);
				
				currentLine = CurrentWidget.listIndex;
				RegisterForRedraw();
			}
		}
		public DbgEvent CurrentEvent {
			get => curEvent;
			set {
				if (curEvent == value)
					return;
				/*if (curEvent != null)
					curEvent.IsSelected = false;*/
				curEvent = value;
				if (curEvent != null) {
					//curEvent.IsSelected = true;
					if (curEvent is DbgWidgetEvent we) {
						//CurrentWidget = Widgets [we.InstanceIndex];
						hoverLine = we.InstanceIndex;
					}
					currentTick = curEvent.begin;
					if (curEvent.begin > minTicks + ScrollX + visibleTicks ||
						curEvent.end < minTicks + ScrollX) 						
							ScrollX = curEvent.begin - minTicks - visibleTicks / 2;											
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

		long hoverTick = 0, currentTick, selStart = -1, selEnd = -1, minTicks = 0, maxTicks = 0, visibleTicks = 0;
		int hoverLine = -1, currentLine = -1;
		int visibleLines = 1;
		Point mousePos;

		public double XScale {
			get => xScale;
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

		void drawEvents (Context ctx, List<DbgEvent> evts)
		{
			if (evts == null || evts.Count == 0)
				return;
			Rectangle cb = ClientRectangle;

			foreach (DbgEvent evt in evts) {
				if ((evt.Category & currentFilter) == currentFilter) {
					if (evt.end - minTicks <= ScrollX)
						continue;
					if (evt.begin - minTicks > ScrollX + visibleTicks)
						break;
					double penY = topMargin + ClientRectangle.Top;

					if (evt.type.HasFlag (DbgEvtType.Widget)) {
						DbgWidgetEvent eW = evt as DbgWidgetEvent;
						int lIdx = eW.InstanceIndex - ScrollY;
						if (lIdx >= 0 && lIdx <= visibleLines) {
							
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
							RectangleD r = new RectangleD(x, penY, w, fe.Height);
							ctx.Rectangle (r);
							ctx.Fill ();
							/*if (evt == CurrentEvent) {
								r.Inflate(2,2);
								ctx.SetSource(Colors.White);
								ctx.Rectangle(r);
								ctx.Stroke();
							}*/
						}
					} else if (evt.type.HasFlag (DbgEvtType.IFace)) {
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
						//ctx.SetSource (0.9,0.9,0.0,0.1);					
						ctx.SetSource (evt.Color.AdjustAlpha(0.15));
						ctx.Rectangle (x, cb.Top + topMargin, w, cb.Height);
						ctx.Fill ();
					}
				}
				drawEvents (ctx, evt.Events);
			}
		}

		DbgEvtType currentFilter;
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			setFontForContext (gr);

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

				if (g.xLevel == 0)
					gr.SetSource (Crow.Colors.LightSalmon);
				else if (currentLine == g.listIndex)
					gr.SetSource(Colors.RoyalBlue);
				else
					Foreground.SetAsSource (IFace, gr);

				gr.MoveTo (penX, penY - gr.FontExtents.Descent);
				gr.ShowText (g.name + gIdx);
			}

			currentFilter = Filter;
			drawEvents (gr, events);

			gr.MoveTo (cb.Left, topMargin - 0.5 + cb.Top);
			gr.LineTo (cb.Right, topMargin - 0.5 + cb.Top);

			gr.MoveTo (leftMargin + cb.Left, cb.Top);
			gr.LineTo (leftMargin + cb.Left, cb.Bottom);
			gr.SetSource (Crow.Colors.Grey);

			penY = topMargin + ClientRectangle.Top;

			//graduation
			long largeGrad = long.Parse ("1" + new string ('0', visibleTicks.ToString ().Length - 1));
			long smallGrad = Math.Max (1, largeGrad / 10);

			long firstVisibleTicks = minTicks + ScrollX;
			long curGrad = firstVisibleTicks - firstVisibleTicks % smallGrad + smallGrad;

			long gg = curGrad - ScrollX - minTicks;
			while (gg < visibleTicks ) {
				double x = (double)gg * xScale + leftMargin + cb.Left;

				gr.MoveTo (x, penY - 0.5);
				if (curGrad % largeGrad == 0) { 
					gr.LineTo (x, penY - 8.5);
					string str = ticksToMS(curGrad);
					TextExtents te = gr.TextExtents (str);
					gr.RelMoveTo (-0.5 * te.Width, -2.0);
					gr.ShowText (str);
				}else
					gr.LineTo (x, penY - 2.5);

				curGrad += smallGrad;
				gg = curGrad - ScrollX - minTicks;
			}

			gr.Stroke ();



		}
		string ticksToMS(long ticks) => Math.Round ((double)ticks / Stopwatch.Frequency * 1000.0, 2).ToString();
		public override void Paint (Cairo.Context ctx)
		{
			base.Paint (ctx);

			Rectangle r = new Rectangle(mousePos.X, 0, 1, Slot.Height);
			Rectangle ctxR = ContextCoordinates (r);
			Rectangle cb = ClientRectangle;
			ctx.LineWidth = 1.0;
			if (hoverTick >= 0) {
				double x = xScale * (hoverTick - minTicks - ScrollX) + leftMargin;
				if (x - Math.Truncate (x) > 0.5)
					x = Math.Truncate (x) + 0.5;
				else
					x = Math.Truncate (x) - 0.5;
				ctx.MoveTo (x, cb.Top + topMargin - 4.0);
				ctx.LineTo (x, cb.Bottom);				
				ctx.SetSource (0.7,0.7,0.7,0.5);
				ctx.Stroke();
			}
			if (currentTick >= 0) {
				double x = xScale * (currentTick - minTicks - ScrollX) + leftMargin;
				if (x > leftMargin && x < cb.Right) {
					if (x - Math.Truncate (x) > 0.5)
						x = Math.Truncate (x) + 0.5;
					else
						x = Math.Truncate (x) - 0.5;
					ctx.MoveTo (x, cb.Top);
					ctx.LineTo (x, cb.Bottom);				
					ctx.SetSource (0.2,0.7,1.0,0.6);
					ctx.Stroke();
				}
			}

			setFontForContext (ctx);

			string str = ticksToMS(hoverTick);

			ctx.MoveTo (ctxR.X - ctx.TextExtents (str).Width / 2, ctxR.Y + fe.Height);
			ctx.ShowText (str);

			ctx.Operator = Cairo.Operator.Add;

			if (hoverLine >= 0) {
				double y = fe.Height * (hoverLine - ScrollY) + topMargin + cb.Top;
				r = new Rectangle (cb.Left,  (int)y, cb.Width, (int)fe.Height);

				ctx.SetSource (0.1, 0.1, 0.1, 0.4);
				ctx.Rectangle (ContextCoordinates (r));
				ctx.Fill ();
			}

			if (currentLine >= ScrollY && currentLine < scrollY + visibleLines) {
				double y = fe.Height * (currentLine - ScrollY) + topMargin + cb.Top;
				r = new Rectangle (cb.Left,  (int)y, cb.Width, (int)fe.Height);

				ctx.SetSource (0.1, 0.1, 0.7, 0.2);
				ctx.Rectangle (ContextCoordinates (r));
				ctx.Fill ();
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
			ctx.SetSource (0.0,0.2,0.8,0.15);
			//ctx.SetSource (Colors.Jet);
			ctx.Fill();
			ctx.Operator = Cairo.Operator.Over;

			str = $"{ticksToMS(Math.Abs (selEnd - selStart))} (ms)";

			ctx.MoveTo (ctxR.Center.X - ctx.TextExtents (str).Width / 2, ctxR.Y + fe.Height);
			ctx.SetSource (Colors.Black);
			ctx.ShowText (str);

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
			hoverLine = -1;
			hoverTick = 0;
		}
		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{			
			long lastTick = hoverTick;
			int lastLine = hoverLine;
			updateMouseLocalPos (e.Position);

			if ((IFace.IsDown (Glfw.MouseButton.Left) || IFace.IsDown (Glfw.MouseButton.Middle)) && selStart >= 0)
				selEnd = hoverTick;
			else if (IFace.IsDown(Glfw.MouseButton.Right)) {
				if (lastTick >= 0 && hoverTick >= 0)
					ScrollX += lastTick - hoverTick;
				if (lastLine >= 0 && hoverLine >= 0)
					ScrollY += lastLine - hoverLine;
				updateMouseLocalPos (e.Position);
			} else {
				HoverWidget = (hoverLine < 0 || hoverLine >= widgets.Count) ? null : widgets [hoverLine];
				//HoverEvent = hoverWidget?.Events.FirstOrDefault (ev => ev.begin <= hoverTick && ev.end >= hoverTick);
				double tickPerPixel = (double)visibleTicks / ClientRectangle.Width;
				//Console.WriteLine ($"ticks per pixel: {tickPerPixel}");
				Task.Run (() => findHoverEvent (hoverWidget, hoverTick, (int)tickPerPixel));
			}

			RegisterForRepaint();
			
			e.Handled = true;
			base.onMouseMove (sender, e);
		}		
		void findHoverEvent (DbgWidgetRecord widget, long tick, long precision = 0) {
			DbgEvent tmp = widget?.Events.FirstOrDefault (ev => ev.begin - precision <= tick && ev.end + precision >= tick);
			if (tmp == null) {
				tmp = Events.Where(e=>e.type.HasFlag(DbgEvtType.IFace)).Where (ev => ev.begin - precision <= tick && ev.end + precision >= tick).FirstOrDefault();				
				while(tmp != null) {
					DbgEvent che = tmp.Events?.Where(e=>e.type.HasFlag(DbgEvtType.IFace)).Where (ev => ev.begin - precision <= tick && ev.end + precision >= tick).FirstOrDefault();
					if (che == null)
						break;
					tmp = che;
				}
			} else {
				while(tmp != null) {
					DbgEvent che = tmp.Events?.OfType<DbgWidgetEvent>()?.Where(ev=>ev.InstanceIndex == widget.listIndex && ev.begin - precision <= tick && ev.end + precision >= tick).FirstOrDefault();
					if (che == null)
						break;
					tmp = che;
				}
			}
			HoverEvent = tmp;
		}
		public override void onMouseClick(object sender, MouseButtonEventArgs e)
		{
			if (e.Button == Glfw.MouseButton.Left) {
				if (selEnd < 0) {
					currentTick = hoverTick;
					currentLine = hoverLine;
					CurrentWidget = hoverWidget;
					CurrentEvent = hoverEvent;
				}
				selStart = -1;
				selEnd = -1;
			}

			e.Handled = true;
			base.onMouseClick(sender, e);
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (e.Button == Glfw.MouseButton.Left || e.Button == Glfw.MouseButton.Middle) {
				selStart = hoverTick;
				selEnd = -1;
			}

			RegisterForRedraw ();
			e.Handled = true;
			base.onMouseDown (sender, e);
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{

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
				ScrollX = scrX;
			}

			RegisterForRedraw ();
			e.Handled = true;
			base.onMouseUp (sender, e);
		}

		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{			
			//base.onMouseWheel (sender, e);

			if (IFace.Shift)
				ScrollX -= (int)((double)(e.Delta * MouseWheelSpeed) / xScale);
			else if (IFace.Ctrl)
				ScrollY -= e.Delta * MouseWheelSpeed;
			else {
				if (e.Delta > 0) {
					XScale *= 2.0;
				} else {
					if (MaxScrollX > 0)
						XScale *= 0.5;
				}
				ScrollX = (long)(hoverTick - (long)((double)Math.Max(0, mousePos.X - (long)leftMargin) / xScale) - minTicks);
			}
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

				setFontForContext (gr);

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
			visibleLines = fe.Height < 1 ? 1 : (int)Math.Ceiling (((double)ClientRectangle.Height - topMargin) / fe.Height);
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
			if (widgets == null) {
				MaxScrollX = 0;				
			} else {
				long tot = maxTicks - minTicks;
				MaxScrollX = Math.Max (0L, tot - visibleTicks);
				NotifyValueChanged ("ChildWidthRatio", (double)visibleTicks / tot);
			}
		}
		void updateMaxScrollY ()
		{
			if (widgets == null)
				MaxScrollY = 0;
			else {
				MaxScrollY = Math.Max (0, widgets.Count + 1 - visibleLines);
				NotifyValueChanged ("ChildHeightRatio", (double)visibleLines / (widgets.Count + 1));
			}
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
				hoverLine = -1;
			else
				hoverLine = (int)((double)(mousePos.Y - cb.Top) / fe.Height) + ScrollY;

			NotifyValueChanged ("CurrentLine", hoverLine);

			mousePos.Y = Math.Max (cb.Y, mousePos.Y);
			mousePos.Y = Math.Min (cb.Bottom, mousePos.Y);

			hoverTick = (long)((double)(mousePos.X - cb.X) / xScale) + minTicks + ScrollX;
			RegisterForRedraw ();
		}
		void zoom (long start, long end) {						
			//Rectangle cb = ClientRectangle;
			//cb.X += (int)leftMargin;
			XScale = ((double)ClientRectangle.Width - leftMargin)/(end - start);
			ScrollX = (int)(start - minTicks);
		}


		long scrollX, maxScrollX;
		int scrollY, maxScrollY, mouseWheelSpeed;

		/// <summary>
		/// if true, key stroke are handled in derrived class
		/// </summary>
		protected bool KeyEventsOverrides = false;

		/// <summary> Horizontal Scrolling Position </summary>
		[DefaultValue(0)]
		public virtual long ScrollX {
			get => scrollX;
			set {
				if (scrollX == value)
					return;

				long newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollX)
					newS = maxScrollX;

				if (newS == scrollX)
					return;

				scrollX = newS;

				NotifyValueChangedAuto (scrollX);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling Position </summary>
		[DefaultValue(0)]
		public virtual int ScrollY {
			get => scrollY;
			set {
				if (scrollY == value)
					return;

				int newS = value;
				if (newS < 0)
					newS = 0;
				else if (newS > maxScrollY)
					newS = maxScrollY;

				if (newS == scrollY)
					return;

				scrollY = newS;

				NotifyValueChangedAuto (scrollY);
				RegisterForGraphicUpdate ();

				if (widgets == null)
					return;

				Rectangle cb = ClientRectangle;
				cb.Left += (int)leftMargin;
				cb.Width -= (int)leftMargin;
				cb.Y += (int)topMargin;
				cb.Height -= (int)topMargin;

				if (mousePos.Y < cb.Top || mousePos.Y > cb.Bottom)
					hoverLine = -1;
				else
					hoverLine = (int)((double)(mousePos.Y - cb.Top) / fe.Height) + ScrollY;

				NotifyValueChanged ("CurrentLine", hoverLine);				
			}
		}
		/// <summary> Horizontal Scrolling maximum value </summary>
		[DefaultValue(0)]
		public virtual long MaxScrollX {
			get => maxScrollX;
			set {
				if (maxScrollX == value)
					return;

				maxScrollX = Math.Max(0, value);

				if (scrollX > maxScrollX)
					ScrollX = maxScrollX;

				NotifyValueChangedAuto (maxScrollX);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Vertical Scrolling maximum value </summary>
		[DefaultValue(0)]
		public virtual int MaxScrollY {
			get => maxScrollY;
			set {
				if (maxScrollY == value)
					return;

				maxScrollY = Math.Max (0, value);

				if (scrollY > maxScrollY)
					ScrollY = maxScrollY;

				NotifyValueChangedAuto (maxScrollY);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary> Mouse Wheel Scrolling multiplier </summary>
		[DefaultValue(1)]
		public virtual int MouseWheelSpeed {
			get => mouseWheelSpeed;
			set {
				if (mouseWheelSpeed == value)
					return;
				
				mouseWheelSpeed = value;

				NotifyValueChangedAuto (mouseWheelSpeed);
			}
		}
	}
}


