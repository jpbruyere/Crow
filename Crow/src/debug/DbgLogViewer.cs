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

		double xScale = 1.0/512.0, yScale = 1.0, leftMargin, topMargin = 0.0;
		string logFile;
		DbgWidgetRecord curWidget;
		DbgEvent curEvent;

		List<DbgEvent> events;
		List<DbgWidgetRecord> objs;

		public List<DbgEvent> Events => events;
		public List<DbgWidgetRecord> Widgets => objs;


		long currentTick = 0, selStart = -1, selEnd = -1, minTicks = 0, maxTicks = 0, visibleTicks = 0;
		int currentLine = -1;
		int visibleLines = 1;
		Point mousePos;

		public string LogFile {
			get { return logFile; }
			set {
				if (logFile == value)
					return;
				logFile = value;

				Console.WriteLine ("before load");
				loadDebugFile ();
				Console.WriteLine ("after load");

				NotifyValueChanged ("LogFile", logFile);
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
				loadDebugFile ();
			}
		}
		public override int ScrollY {
			get => base.ScrollY;
			set {
				base.ScrollY = value;

				if (objs == null)
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

		public DbgWidgetRecord CurrentWidget {
			get => curWidget;
			internal set {
				if (curWidget == value)
					return;
				curWidget = value;
				NotifyValueChanged (nameof (CurrentWidget), curWidget);
			}
		}
		public DbgEvent CurrentEvent {
			get => curEvent;
			internal set {
				if (curEvent == value)
					return;

				if (curEvent != null)
					curEvent.IsSelected = false;
				curEvent = value;
				if (curEvent != null) {
					curEvent.IsSelected = true;
					curEvent.IsExpanded = true;
				}

				NotifyValueChanged (nameof (CurrentEvent), curEvent);
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
					double x = xScale * (evt.begin - minTicks - ScrollX);
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
					ctx.ShowText (s);

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

			if (objs == null)
				return;

			gr.LineWidth = 1.0;

			Rectangle cb = ClientRectangle;

			double penY = topMargin + ClientRectangle.Top;

			for (int i = 0; i < visibleLines; i++) {
				if (i + ScrollY >= objs.Count)
					break;
				int gIdx = i + ScrollY;
				DbgWidgetRecord g = objs [gIdx];

				penY += fe.Height;

				gr.SetSource (Crow.Colors.Jet);
				gr.MoveTo (cb.X, penY - 0.5);
				gr.LineTo (cb.Right, penY - 0.5);
				gr.Stroke ();

				double penX = 5.0 * g.xLevel + cb.Left;

				if (g.yIndex == 0)
					gr.SetSource (Crow.Colors.LightSalmon);
				else
					Foreground.SetAsSource (gr);

				gr.MoveTo (penX, penY - gr.FontExtents.Descent);
				gr.ShowText (g.name + gIdx);

				gr.SetSource (Crow.Colors.White);
				gr.MoveTo (cb.X, penY - gr.FontExtents.Descent);
				gr.ShowText ((i + ScrollY).ToString ());
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
		public override void Paint (ref Cairo.Context ctx)
		{
			base.Paint (ref ctx);

			Rectangle r = new Rectangle(mousePos.X, 0, 1, Slot.Height);
			Rectangle ctxR = ContextCoordinates (r);
			Rectangle cb = ClientRectangle;

			ctx.Rectangle (ctxR);
			ctx.SetSource (Colors.CornflowerBlue);
			ctx.Fill();

			ctx.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			ctx.SetFontSize (Font.Size);
			ctx.FontOptions = Interface.FontRenderingOptions;
			ctx.Antialias = Interface.Antialias;

			ctx.MoveTo (ctxR.X, ctxR.Y + fe.Height);
			ctx.ShowText (currentTick.ToString ());

			ctx.Operator = Cairo.Operator.Add;

			if (currentLine >= 0) {
				double y = fe.Height * (currentLine - ScrollY) + topMargin + cb.Top;
				r = new Rectangle (cb.Left,  (int)y, cb.Width, (int)fe.Height);

				ctx.Operator = Cairo.Operator.Add;
				ctx.SetSourceRGBA (0.1, 0.1, 0.1, 0.4);
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
			}

			if (RegisteredLayoutings == LayoutingType.None && !IsDirty)
				IFace.EnqueueForRepaint (this);
			
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);

			if (e.Button == Glfw.MouseButton.Left) {
				CurrentWidget = (currentLine < 0 || currentLine >= objs.Count) ? null : objs [currentLine];
				CurrentEvent = curWidget?.Events.FirstOrDefault (ev => ev.begin <= currentTick && ev.end >= currentTick);
				selStart = currentTick;
				selEnd = -1;
			}

			RegisterForRedraw ();
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);

			selStart = -1;
			selEnd = -1;

			RegisterForRedraw ();
		}

		/// <summary> Process scrolling vertically, or if shift is down, vertically </summary>
		public override void onMouseWheel (object sender, MouseWheelEventArgs e)
		{			
			//base.onMouseWheel (sender, e);

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
		async void loadDebugFile ()
		{
			await loadDebugFileAsync ();
		}



		async Task loadDebugFileAsync ()
		{
			if (!File.Exists (logFile))
				return;

			events = new List<DbgEvent> ();
			objs = new List<DbgWidgetRecord> ();
			minTicks = maxTicks = 0;
			leftMargin = topMargin = 0.0;

			using (Context gr = new Context (IFace.surf)) {
				double maxNameWidth = 0.0;

				gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				gr.SetFontSize (Font.Size);

				using (StreamReader s = new StreamReader (logFile)) {
					if (s.ReadLine () != "[GraphicObjects]")
						return;
					while (!s.EndOfStream) {
						string l = s.ReadLine ();
						if (l == "[Events]")
							break;
						DbgWidgetRecord o = DbgWidgetRecord.Parse (l);
						objs.Add (o);
						double nameWidth = gr.TextExtents (o.name).Width + 5.0 * o.xLevel;
						if (nameWidth > maxNameWidth)
							maxNameWidth = nameWidth;
					}

					Stack<DbgEvent> startedEvents = new Stack<DbgEvent> ();

					if (!s.EndOfStream) {
						while (!s.EndOfStream) {
							int level = 0;
							while (s.Peek () == (int)'\t') {
								s.Read ();
								level++;
							}
							DbgEvent evt = DbgEvent.Parse (s.ReadLine ());
							if (evt.end > maxTicks)
								maxTicks = evt.end;
							if (level == 0) {
								startedEvents.Clear ();
								events.Add (evt);
							} else {
								int levelDiff = level - startedEvents.Count + 1;
								if (levelDiff > 0) {
									if (levelDiff > 1)
										System.Diagnostics.Debugger.Break ();
									startedEvents.Peek ().AddEvent (evt);
								} else {
									startedEvents.Pop ();
									if (-levelDiff > startedEvents.Count)
										System.Diagnostics.Debugger.Break ();
									while (startedEvents.Count > level)
										startedEvents.Pop ();
									startedEvents.Peek ().AddEvent (evt);
								}
							}
							startedEvents.Push (evt);
							if (evt.type.HasFlag(DbgEvtType.Widget))
								objs [(evt as DbgWidgetEvent).InstanceIndex].Events.Add (evt);
						}
						if (events.Count > 0)
							minTicks = events [0].begin;
					}
				}

				leftMargin = 2.5 + maxNameWidth;
				topMargin = 2.0 * fe.Height;

				updateVisibleLines ();
				updateVisibleTicks ();
			}
			NotifyValueChanged ("Widgets", Widgets);
			NotifyValueChanged ("Events", Events);
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
			if (objs == null)
				MaxScrollX = 0;
			else
				MaxScrollX = (int)Math.Max (0L, maxTicks - minTicks - visibleTicks);
		}
		void updateMaxScrollY ()
		{
			if (objs == null)
				MaxScrollY = 0;
			else
				MaxScrollY = Math.Max (0, objs.Count - visibleLines);
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
		}
		void zoom (long start, long end) {						
			//Rectangle cb = ClientRectangle;
			//cb.X += (int)leftMargin;
			XScale = ((double)ClientRectangle.Width - leftMargin)/(end - start);
			ScrollX = (int)(start - minTicks);
		}
	}
}


