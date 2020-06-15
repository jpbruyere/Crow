// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Crow.Cairo;

#if DEBUG_LOG
namespace Crow
{
	public class DbgLogViewer : ScrollingObject
	{
		public static Dictionary<DbgEvtType,Color> colors;

 		public static Configuration colorsConf = new Configuration("dbgcolor.conf");

		#region debug viewer private classes
		public class DbgData {
			public int objInstanceNum;
			public LayoutingType layout;
			public LayoutingQueueItem.Result result;

			public DbgData (int _obj) {
				objInstanceNum = _obj;
			}
		}
		public class DbgEvent {
			public long begin, end;
			public DbgEvtType type;
			public DbgData data = null;

			public DbgEvent() {}

			public static DbgEvent Parse (string str) {
				if (str == null)
					return null;
				string[] tmp = str.Trim().Split(';');

				DbgEvent evt = new DbgEvent ();
				evt.begin = long.Parse (tmp [0]);
				evt.end = long.Parse (tmp [1]);
				evt.type = (DbgEvtType)Enum.Parse (typeof(DbgEvtType), tmp [2]);

				if (evt.type.HasFlag (DbgEvtType.GraphicObject)) {
					evt.data = new DbgData (int.Parse (tmp [3]));
					if (evt.type.HasFlag (DbgEvtType.GOLayouting)) {
						evt.data.layout = (LayoutingType)Enum.Parse (typeof(LayoutingType), tmp [4]);
						if (evt.type == DbgEvtType.GOProcessLayouting)
							evt.data.result = (LayoutingQueueItem.Result)Enum.Parse (typeof(LayoutingQueueItem.Result), tmp [5]);										
					}
				}
				return evt;
			}
		}
		class DbgGo {
			public int listIndex;//prevent doing an IndexOf on list for each event to know y pos on screen
			public int instanceNum;//class instantiation order, used to bind events to objs
			public string name;
			//0 is the main graphic tree, for other obj tree not added to main tree, it range from 1->n
			//useful to track events for obj shown later, not on start
			public int treeIndex;
			public int yIndex;//index in parenting, the whole main graphic tree is one continuous suite
			public int xLevel;//depth

			public List<DbgEvent> events = new List<DbgEvent>();

			public static DbgGo Parse (string str) {
				DbgGo g = new DbgGo ();
				if (str == null)
					return null;
				string[] tmp = str.Trim().Split(';');
				g.instanceNum = int.Parse (tmp [0]);
				g.name = tmp [1];
				g.yIndex = int.Parse (tmp [2]);
				g.xLevel = int.Parse (tmp [3]);
				return g;
			}

		}
		#endregion

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
		public DbgLogViewer (Interface iface) : base(iface){}
		#endregion

		FontExtents fe;

		double xScale = 1.0/512.0, yScale = 1.0, leftMargin, topMargin = 0.0;
		string logFile;

		List<DbgEvent> events;//global events
		List<DbgGo> objs;

		long currentTick = 0, selStart = -1, selEnd = -1, minTicks = 0, maxTicks = 0, visibleTicks = 0;
		int currentLine = -1;
		int visibleLines = 1;

		public string LogFile {
			get { return logFile; }
			set {
				if (logFile == value)
					return;
				logFile = value;

				loadDebugFile ();

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
			get { return yScale; }
			set {
				if (yScale == value)
					return;
				yScale = value;
				NotifyValueChanged ("YScale", yScale);
				RegisterForGraphicUpdate ();
			}
		}


		void storeEvent (DbgEvent evt) {
			if (evt.data == null)//global events
				events.Add (evt);
			else {
				DbgGo go = objs.Where (o => o.instanceNum == evt.data.objInstanceNum).FirstOrDefault ();
				if (go == null)
					System.Diagnostics.Debug.WriteLine ("Unknown instance: " + evt.data.objInstanceNum);
				else
					go.events.Add (evt);						
			}
		}

		void loadDebugFile() {
			if (!File.Exists (logFile))
				return;

			events = new List<DbgEvent>();
			objs = new List<DbgGo> ();
			minTicks = maxTicks = 0;
			leftMargin = topMargin = 0.0;

			using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
				using (Context gr = new Context (img)) {
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
							DbgGo o = DbgGo.Parse (l);
							objs.Add (o);
							double nameWidth = gr.TextExtents (o.name).Width + 5.0 * o.xLevel;
							if (nameWidth > maxNameWidth)
								maxNameWidth = nameWidth;
						}
						if (!s.EndOfStream) {
							DbgEvent firstEvt = DbgEvent.Parse (s.ReadLine ());
							storeEvent (firstEvt);
							minTicks = firstEvt.begin;
						}

						if (!s.EndOfStream) {
							while (true) {
								DbgLogViewer.DbgEvent evt = DbgEvent.Parse (s.ReadLine ());
								storeEvent (evt);
								if (s.EndOfStream) {
									maxTicks = evt.end;
									break;
								}
							}
						}
					}

					leftMargin = 2.5 + maxNameWidth;

					topMargin = 2.0 * fe.Height;

					updateVisibleLines ();
					updateVisibleTicks ();
				}
			}

		}
		void updateVisibleLines(){
			visibleLines = fe.Height < 1 ? 1 : (int)Math.Floor (((double)ClientRectangle.Height - topMargin) / fe.Height);
			NotifyValueChanged ("VisibleLines", visibleLines);
			updateMaxScrollY ();
		}
		void updateVisibleTicks() {
			visibleTicks = Math.Max (0, (long)((double)(ClientRectangle.Width - leftMargin) / XScale));
			NotifyValueChanged ("VisibleTicks", visibleTicks);
			updateMaxScrollX ();
		}
		/*
		void updateVisibleColumns(){
			visibleColumns = (int)Math.Floor ((double)(ClientRectangle.Width - leftMargin)/ fe.MaxXAdvance);
			NotifyValueChanged ("VisibleColumns", visibleColumns);
			updateMaxScrollX ();
		}
		void updateMaxScrollX () {
			MaxScrollX = Math.Max (0, buffer.longestLineCharCount - visibleColumns);
			if (buffer.longestLineCharCount > 0)
				NotifyValueChanged ("ChildWidthRatio", Slot.Width * visibleColumns / buffer.longestLineCharCount);			
		}*/

		void updateMaxScrollX () {
			if (objs == null)
				MaxScrollX = 0;
			else				
				MaxScrollX =  (int)Math.Max(0L, maxTicks - minTicks - visibleTicks);
		}
		void updateMaxScrollY () {
			if (objs == null)
				MaxScrollY = 0;
			else				
				MaxScrollY =  Math.Max(0, objs.Count - visibleLines);
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
			get {
				return base.ScrollY;
			}
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
				DbgGo g = objs [i + ScrollY];


				foreach (DbgEvent evt in g.events) {
					

					if (evt.end - minTicks <= ScrollX)
						continue;
					if (evt.begin - minTicks > ScrollX + visibleTicks)
						break;

					double x = xScale * (evt.begin - minTicks - ScrollX) ;
					double w = Math.Max (Math.Max(2.0, 2.0 * xScale), (double)(evt.end - evt.begin) * xScale);
					if (x < 0.0) {
						w += x;
						x = 0.0;
					}
					x += leftMargin + cb.Left;
					double rightDiff = x + w - cb.Right;
					if (rightDiff > 0)
						w -= rightDiff;
					//if (x + w > cb.Right)
					//	continue;

					Color c = Colors.Black;

					if (evt.type == DbgEvtType.GOProcessLayouting) {
						switch (evt.data.result) {
						case LayoutingQueueItem.Result.Success:
							c = Crow.Colors.Green;
							break;
						case LayoutingQueueItem.Result.Deleted:
							c = Crow.Colors.Red;
							break;
						case LayoutingQueueItem.Result.Discarded:
							c = Crow.Colors.OrangeRed;
							break;
						case LayoutingQueueItem.Result.Requeued:
							c = Crow.Colors.Orange;
							break;
						}
					} else if (evt.type.HasFlag (DbgEvtType.GOLock))
						c = Colors.BlueViolet;
					else if (colors.ContainsKey (evt.type))
						c = colors [evt.type];
					//else
					//	System.Diagnostics.Debugger.Break ();
					c = c.AdjustAlpha (0.2);
					gr.SetSource (c);

					gr.Rectangle (x, penY, w, fe.Height);
					gr.Fill ();
				}

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
				gr.ShowText (g.name);

				gr.SetSource (Crow.Colors.White);
				gr.MoveTo (cb.X, penY - gr.FontExtents.Descent);
				gr.ShowText ((i+ ScrollY).ToString());

			}


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
			foreach (DbgEvent evt in events) {
				if (evt.begin - minTicks <= ScrollX)
					continue;
				double x = xScale * (evt.begin - minTicks - ScrollX) ;
				x += leftMargin + cb.Left;

				gr.SetSource (Crow.Colors.Yellow);
				gr.MoveTo (x, penY);
				gr.LineTo (x, cb.Bottom);
				gr.Stroke ();
				string s = evt.type.ToString () [10].ToString ();
				Cairo.TextExtents te = gr.TextExtents (s);
				gr.Rectangle (x - 0.5 * te.Width , penY - te.Height, te.Width, te.Height);
				gr.Fill ();
				gr.MoveTo (x- 0.5 * te.Width, penY - gr.FontExtents.Descent);
				gr.SetSource (Crow.Colors.Jet);
				gr.ShowText (s);

			}

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
				r = new Rectangle (cb.Left, (currentLine + 2 - ScrollY) * (int)fe.Height + cb.Top, cb.Width, (int)fe.Height);

				ctx.Operator = Cairo.Operator.Add;
				ctx.SetSourceRGBA (0.1, 0.1, 0.1, 0.4);
				ctx.Rectangle (ContextCoordinates (r));
				ctx.Fill ();
			}

			if (selStart < 0) {
				ctx.Operator = Cairo.Operator.Over;
				return;
			}
			double selStartX = (double)(selStart - ScrollX - minTicks) * xScale + leftMargin + cb.Left;
			double selEndX = (selEnd >= 0) ? (double)(selEnd - ScrollX - minTicks) * xScale + leftMargin + cb.Left :
				(double)(currentTick - ScrollX - minTicks) * xScale + leftMargin + cb.Left;

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

		Point mousePos;
		void updateMouseLocalPos(Point mPos){
			Rectangle r = ScreenCoordinates (Slot);
			Rectangle cb = ClientRectangle;
			cb.Left += (int)leftMargin;
			cb.Width -= (int)leftMargin;
			cb.Y += (int)topMargin;
			cb.Height -= (int)topMargin;

			mousePos = mPos - r.Position;

			mousePos.X = Math.Max(cb.X, mousePos.X);
			mousePos.X = Math.Min(cb.Right, mousePos.X);

			if (mousePos.Y < cb.Top || mousePos.Y > cb.Bottom)
				currentLine = -1;
			else
				currentLine = (int)((double)(mousePos.Y - cb.Top) / fe.Height) + ScrollY;

			NotifyValueChanged ("CurrentLine", currentLine);
			
			mousePos.Y = Math.Max(cb.Y, mousePos.Y);
			mousePos.Y = Math.Min(cb.Bottom, mousePos.Y);

			currentTick = (int)((double)(mousePos.X - cb.X) / xScale) + minTicks + ScrollX;

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
			updateMouseLocalPos (e.Position);

			if (selStart >= 0 && IFace.IsDown (Glfw.MouseButton.Left))
				selEnd = currentTick;

			if (RegisteredLayoutings == LayoutingType.None && !IsDirty)
				IFace.EnqueueForRepaint (this);
			
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

		void zoom (long start, long end) {						
			//Rectangle cb = ClientRectangle;
			//cb.X += (int)leftMargin;
			XScale = ((double)ClientRectangle.Width - leftMargin)/(end - start);
			ScrollX = (int)(start - minTicks);
		}
	}
}
#endif

