//
// DbgEventViewer.cs
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
using Cairo;
using System.Linq;
using System.Threading;
using System.ComponentModel;
using System.Reflection;

namespace Crow.Coding
{
	public struct Column {
		public string Data;
		public string Title;
		public int Width;
		public bool Visible;
		public Alignment TextAlign;

		public Column (string data, string caption, int width = -1, Alignment textAlign = Alignment.Left) {
			Data = data;
			Title = caption;
			Width = width;
			Visible = true;
			TextAlign = textAlign;
		}
	}
	public class DbgEventViewer : ScrollingObject
	{
		protected ReaderWriterLockSlim evtsMTX = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

		int visibleLines = 1;
		int visibleColumns = 1;
		int hoverLine = -1;
		int columnMargin = 4;
		List<Column> Columns = new List<Column>();
		int hoverColIdx = -1;
		bool sizingCol = false; //true during resize of column with mouse
		List<int> selectedLines = new List<int>();
		Point mouseLocalPos;
		Color selBackground, selForeground, hoverBackground;

		FontExtents fe;
		List<DebugEvent> debugEvents;
		List<DebugEvent> filteredEvts;

		string threadFilter = "*";
		string objFilter = "*";
		bool objSibling, objParent, objDescendants;

		public string ObjFilter {
			get { return objFilter; }
			set {
				if (objFilter == value)
					return;
				objFilter = value;
				NotifyValueChanged ("ObjFilter", objFilter);
				updateFilteredEvents ();
			}
		}
		public bool ObjSibling {
			get { return objSibling; }
			set {
				if (objSibling == value)
					return;
				objSibling = value;
				NotifyValueChanged ("ObjSibling", objSibling);
				updateFilteredEvents ();
			}
		}
		public bool ObjParent {
			get { return objParent; }
			set {
				if (objParent == value)
					return;
				objParent = value;
				NotifyValueChanged ("ObjParent", objParent);
				updateFilteredEvents ();
			}
		}
		public bool ObjDescendants {
			get { return objDescendants; }
			set {
				if (objDescendants == value)
					return;
				objDescendants = value;
				NotifyValueChanged ("ObjDescendants", objDescendants);
				updateFilteredEvents ();
			}
		}
		public string ThreadFilter {
			get { return threadFilter; }
			set {
				if (threadFilter == value)
					return;
				threadFilter = value;
				NotifyValueChanged ("ThreadFilter", threadFilter);
				updateFilteredEvents ();
			}
		}
		public List<DebugEvent> DebugEvents {
			get { return debugEvents; }
			set {
				if (debugEvents == value)
					return;
				evtsMTX.EnterWriteLock ();
				if (value == null)
					debugEvents = null;
				else
					debugEvents = value.ToList();
				evtsMTX.ExitWriteLock ();
				NotifyValueChanged ("DebugEvent", debugEvents);

				updateFilteredEvents ();
			}
		}
		public int HoverLine {
			get { return hoverLine; }
			set { 
				if (hoverLine == value)
					return;
				hoverLine = value;
				NotifyValueChanged ("HoverLine", hoverLine);
				Tooltip = (filteredEvts [hoverLine] as WidgetDebugEvent)?.FullName;
				evtsMTX.EnterReadLock ();
				if (selectedLines.Count > 0) {
					long elapsed = Math.Abs (filteredEvts [selectedLines.LastOrDefault ()].Ticks -
						filteredEvts [hoverLine].Ticks);
					NotifyValueChanged ("ElapsedTicks", elapsed);
					NotifyValueChanged ("ElapsedMS", 
						((double)(elapsed * Interface.nanosecPerTick) / 1000000.0).ToString("#0.000"));
				}
				evtsMTX.ExitReadLock ();
				RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue("SlateGray")]
		public virtual Color HoverBackground {
			get { return hoverBackground; }
			set {
				if (value == hoverBackground)
					return;
				hoverBackground = value;
				NotifyValueChanged ("HoverBackground", hoverBackground);
				RegisterForRedraw ();
			}
		}
		[DefaultValue("RoyalBlue")]
		public virtual Color SelectionBackground {
			get { return selBackground; }
			set {
				if (value == selBackground)
					return;
				selBackground = value;
				NotifyValueChanged ("SelectionBackground", selBackground);
				RegisterForRedraw ();
			}
		}
		[DefaultValue("White")]
		public virtual Color SelectionForeground {
			get { return selForeground; }
			set {
				if (value == selForeground)
					return;
				selForeground = value;
				NotifyValueChanged ("SelectionForeground", selForeground);
				RegisterForRedraw ();
			}
		}
		public override Font Font {
			get { return base.Font; }
			set {
				base.Font = value;

				using (ImageSurface img = new ImageSurface (Format.Argb32, 1, 1)) {
					using (Context gr = new Context (img)) {
						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);

						fe = gr.FontExtents;
					}
				}
				MaxScrollY = 0;
				RegisterForGraphicUpdate ();
			}
		}


		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height)
				updateVisibleLines ();
			else if (layoutType == LayoutingType.Width)
				updateVisibleColumns ();
		}
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			if (filteredEvts == null)
				return;

			evtsMTX.EnterReadLock ();

			try {
				
				int filteredCount = filteredEvts.Count;

				gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				gr.SetFontSize (Font.Size);
				gr.FontOptions = Interface.FontRenderingOptions;
				gr.Antialias = Interface.Antialias;

				Rectangle cb = ClientRectangle;
				double y = cb.Y, x = cb.X;

				gr.SetSourceColor (Color.Gray);
				gr.Rectangle (x, y, cb.Width, fe.Ascent + fe.Descent);
				gr.FillPreserve ();
				gr.SetSourceColor (Color.Black);
				gr.LineWidth = 1;
				gr.Stroke ();

				foreach (Column c in Columns) {				
					TextExtents te = gr.TextExtents (c.Title);
					gr.MoveTo (x + columnMargin + (c.Width-2*columnMargin) / 2 - te.Width / 2, y + fe.Ascent);
					gr.ShowText (c.Title);
					x += c.Width;
					gr.MoveTo (x + 0.5, cb.Y);
					gr.LineTo (x + 0.5, cb.Bottom);
				}
				gr.Stroke ();

				for (int i = 0; i < visibleLines; i++) {
					int lineIndex = i + ScrollY;
					if (lineIndex >= filteredCount)
						break;				
					y += (fe.Ascent + fe.Descent);
					x = cb.X;

					gr.Operator = Operator.Multiply;
					if (selectedLines.Contains (lineIndex)) {
						gr.SetSourceColor (selBackground);
						gr.Rectangle (x, y, cb.Width, fe.Ascent + fe.Descent);
						gr.Fill ();
						gr.SetSourceColor (selForeground);
					}
					if (lineIndex == hoverLine) {
						gr.SetSourceColor (hoverBackground);
						gr.Rectangle (x, y, cb.Width, fe.Ascent + fe.Descent);
						gr.Fill ();
						gr.SetSourceColor (selForeground);
					}else
						Foreground.SetAsSource (gr);
					gr.Operator = Operator.Over;

					object obj = filteredEvts [i + ScrollY];
					Type t = obj.GetType ();
					foreach (Column c in Columns) {					
						MemberInfo mi = t.GetMember (c.Data).FirstOrDefault();
						if (mi != null) {
							string str = "";
							if (mi.MemberType == MemberTypes.Field) {
								FieldInfo fi = mi as FieldInfo;
								str = fi.GetValue (obj).ToString ();
							} else if (mi.MemberType == MemberTypes.Property) {
								MethodInfo pmi = (mi as PropertyInfo).GetGetMethod ();
								str = pmi.Invoke (obj, null).ToString();
							}

							TextExtents te = gr.TextExtents (str);

							if (c.TextAlign == Alignment.Right)
								gr.MoveTo (x + columnMargin + Math.Max (0,(c.Width - 2*columnMargin) - te.Width), y + fe.Ascent);
							else
								gr.MoveTo (x + columnMargin, y + fe.Ascent);
							
							gr.ShowText (str);
							gr.Stroke ();

						}
						x += c.Width;
					}
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}

			evtsMTX.ExitReadLock ();
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);
			mouseLocalPos = e.Position - ScreenCoordinates(Slot).TopLeft - ClientRectangle.TopLeft;

			if (sizingCol) {
				Column c = Columns[hoverColIdx];
				c.Width += e.XDelta;
				if (c.Width < 1)
					c.Width = 1;
				Columns[hoverColIdx] = c;
				RegisterForGraphicUpdate ();
				return;
			}

			hoverColIdx = -1;
			int x = 0;
			for (int i = 0; i < Columns.Count; i++) {
				x += Columns[i].Width;
				if (mouseLocalPos.X > x - columnMargin) {
					if (mouseLocalPos.X < x + columnMargin) {
						hoverColIdx = i;
						break;
					}	
				} else 
					break;
			}

			if (hoverColIdx < 0)
				IFace.MouseCursor = XCursor.Default;				
			else
				IFace.MouseCursor = XCursor.H;
			
			HoverLine = ScrollY + (int)Math.Max (0, Math.Floor (mouseLocalPos.Y / (fe.Ascent+fe.Descent))- 1);
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			base.onMouseDown (sender, e);

			if (hoverColIdx >= 0) {
				sizingCol = true;
				return;
			}

			if (IFace.Keyboard [Key.ControlLeft]) {
				if (hoverLine >= 0) {
					if (selectedLines.Contains (hoverLine))
						selectedLines.Remove (hoverLine);
					else
						selectedLines.Add (hoverLine);
				}
			}else if (IFace.Keyboard [Key.ShiftLeft]) {
				int curL = selectedLines.LastOrDefault ();
				int inc = 1;
				if (curL > hoverLine)
					inc = -1;
				while (curL != hoverLine) {
					curL += inc;
					if (!selectedLines.Contains(curL))
						selectedLines.Add (curL);					
				}
			} else
				selectedLines = new List<int> () { hoverLine };

			RegisterForGraphicUpdate ();
		}
		public override void onMouseUp (object sender, MouseButtonEventArgs e)
		{
			base.onMouseUp (sender, e);
			if (sizingCol)
				sizingCol = false;
		}

		void updateFilteredEvents () {
			evtsMTX.EnterWriteLock();

			try {
				hoverLine = -1;
				selectedLines.Clear ();

				if (debugEvents == null)
					filteredEvts = null;
				else {
					if (threadFilter == "*")
						filteredEvts = debugEvents.ToList ();
					else {
						string[] tmp = threadFilter.Split (';');
						filteredEvts = debugEvents.Where (de=>tmp.Contains(de.ThreadId.ToString())).ToList();
					}

					if (objFilter != "*"){
						string[] tmp = objFilter.Split (';');
	//					if (objParent) {
	//						List<string> tmpObjs = new List<string> (tmp);
	//						for (int i = 0; i < tmp.Length; i++) {
	//							tmpObjs.Add(tmp[i].Substring(0, tmp[i].LastIndexOf('.')-1));
	//						}
	//					}
						List<WidgetDebugEvent> tmpwde = filteredEvts.OfType<WidgetDebugEvent> ().ToList();

						if (objDescendants)
							filteredEvts = tmpwde.Where (
								wde=>tmp.Count (t => wde.FullName.Split('.').Contains(t)) > 0)
								.Cast<DebugEvent>().ToList();
						else
							filteredEvts = tmpwde.Where (wde=>tmp.Contains(wde.Name)).Cast<DebugEvent>().ToList();
					}
				}
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex);
			}

			evtsMTX.ExitWriteLock ();

			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
		}
		void updateVisibleLines(){
			visibleLines = (int)Math.Max(0, Math.Floor ((double)ClientRectangle.Height / (fe.Ascent+fe.Descent)) - 1);
			NotifyValueChanged ("VisibleLines", visibleLines);
			updateMaxScrollY ();
			RegisterForGraphicUpdate ();
		}
		void updateVisibleColumns(){
			visibleColumns = (int)Math.Floor ((double)(ClientRectangle.Width)/ fe.MaxXAdvance);
			NotifyValueChanged ("VisibleColumns", visibleColumns);
			RegisterForGraphicUpdate ();
		}
		void updateMaxScrollX (int longestTabulatedLineLength) {			
//			MaxScrollX = Math.Max (0, longestTabulatedLineLength - visibleColumns);
//			if (longestTabulatedLineLength > 0)
//				NotifyValueChanged ("ChildWidthRatio", Slot.Width * visibleColumns / longestTabulatedLineLength);
		}
		void updateMaxScrollY () {
			if (filteredEvts == null){
				MaxScrollY = 0;
				return;
			}
			evtsMTX.EnterReadLock ();
			int lc = filteredEvts.Count;
			evtsMTX.ExitReadLock ();

			MaxScrollY = Math.Max (0, lc - visibleLines);
			if (lc > 0)
				NotifyValueChanged ("ChildHeightRatio", Slot.Height * visibleLines / lc);
			

		}

		public override void onKeyDown (object sender, KeyboardKeyEventArgs e)
		{
			base.onKeyDown (sender, e);

			switch (e.Key) {
			case Key.W:
				if (hoverLine < 0 || filteredEvts == null)
					break;
				WidgetDebugEvent wde = filteredEvts [hoverLine] as WidgetDebugEvent;
				if (wde == null)
					break;
				ObjFilter = wde.Name;
				break;
			}
		}

		protected override void onInitialized (object sender, EventArgs e)
		{
			base.onInitialized (sender, e);

			Columns.Add (new Column ("ThreadId", "T", 16, Alignment.Right));
			Columns.Add (new Column ("Ticks", "tks", 60, Alignment.Right));
			Columns.Add (new Column ("EventType", "Event", 120));
			Columns.Add (new Column ("Name", "Widget Name", 120));
			Columns.Add (new Column ("Slot", "Slot", 100, Alignment.Right));
			Columns.Add (new Column ("Message", "Message", 400));
		}
	}
}

