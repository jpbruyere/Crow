// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Text;

namespace Crow
{
	[DesignIgnore]
	public class DockStack : GenericStack
	{		
		#region CTor
		public DockStack ()	{}
		public DockStack (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		/*static int color = 10;

		protected override void onInitialized (object sender, EventArgs e)
		{
			base.onInitialized (sender, e);
			Background = Color.ColorDic.Values.ToList()[color++];
		}*/
		public override void AddChild (Widget g)
		{
			base.AddChild (g);
			if (localLogicalParentIsNull)
				g.LogicalParent = this;
			else
				g.LogicalParent = this.LogicalParent;
		}
		public override void InsertChild (int idx, Widget g)
		{
			base.InsertChild (idx, g);
			g.LogicalParent = this.LogicalParent;
		}

		/*public override bool PointIsIn (ref Point m)
		{			
			if (!base.PointIsIn(ref m))
				return false;

			Group p = Parent as Group;
			if (p != null) {
				childrenRWLock.EnterReadLock ();
				for (int i = p.Children.Count - 1; i >= 0; i--) {
					if (p.Children [i] == this)
						break;
					if (p.Children [i].IsDragged)
						continue;
					if (p.Children [i].Slot.ContainsOrIsEqual (m)) {
						childrenRWLock.ExitReadLock ();
						return false;
					}
				}
				childrenRWLock.ExitReadLock ();
			}

			return Slot.ContainsOrIsEqual(m);
		}*/

//		public override void OnLayoutChanges (LayoutingType layoutType)
//		{
//			base.OnLayoutChanges (layoutType);
//
//			if ((layoutType & LayoutingType.Sizing) > 0)
//				computeRects();			
//		}

		Rectangle rIn = default(Rectangle);
		double dockThresh = 0.2;
		const int dockWidthDivisor = 8;
		internal Widget focusedChild;
		internal Widget stretchedChild;

		void getFocusedChild (Point lm) {
			Rectangle cb = ClientRectangle;

			childrenRWLock.EnterReadLock ();
			try {
				foreach (Widget c in Children) {
					Rectangle bounds = c.Slot + cb.Position;
					if (!bounds.ContainsOrIsEqual (lm))
						continue;
					rIn = bounds;
					focusedChild = c;
					break;
				}
			} finally {
				childrenRWLock.ExitReadLock ();
			}
		}

		public void onDragMouseMove (object sender, MouseMoveEventArgs e)
		{

			//if (IsDropTarget) {				
				DockWindow dw = IFace.DragAndDropOperation.DragSource as DockWindow;
				if (dw == null || dw.IsDocked) {
					base.onMouseMove (sender, e);
					return;
				}

				Alignment curDockPos = dw.DockingPosition;
				dw.DockingPosition = Alignment.Undefined;

				Rectangle cb = ClientRectangle;
				Point lm = ScreenPointToLocal (e.Position);

				if (Children.Count == 0) {
					Rectangle r = cb;
					r.Inflate (r.Width / -5, r.Height / -5);
					if (r.ContainsOrIsEqual(lm))
						dw.DockingPosition = Alignment.Center;
				} else {
					rIn = cb;

					if (Orientation == Orientation.Horizontal || Children.Count == 1) {
						if (lm.Y > cb.Top + cb.Height / dockWidthDivisor && lm.Y < cb.Bottom - cb.Height / dockWidthDivisor) {
							if (lm.X < cb.Left + cb.Width / dockWidthDivisor)
								dw.DockingPosition = Alignment.Left;
							else if (lm.X > cb.Right - cb.Width / dockWidthDivisor)
								dw.DockingPosition = Alignment.Right;							
						} else {
							getFocusedChild (lm);
							if (focusedChild != null) {
								if (lm.Y < rIn.Top + rIn.Height / dockWidthDivisor)
									dw.DockingPosition = Alignment.Top;
								else if (lm.Y > rIn.Bottom - rIn.Height / dockWidthDivisor)
									dw.DockingPosition = Alignment.Bottom;										
							}
						}
					}
					if (Orientation == Orientation.Vertical || Children.Count == 1) {
						if (lm.X > cb.Left + cb.Width / dockWidthDivisor && lm.X < cb.Right - cb.Width / dockWidthDivisor) {
							if (lm.Y < cb.Top + cb.Height / dockWidthDivisor)
								dw.DockingPosition = Alignment.Top;
							else if (lm.Y > cb.Bottom - cb.Height / dockWidthDivisor)
								dw.DockingPosition = Alignment.Bottom;							
						} else {
							getFocusedChild (lm);
							if (focusedChild != null) {
								if (lm.X < rIn.Left + rIn.Width / dockWidthDivisor)
									dw.DockingPosition = Alignment.Left;
								else if (lm.X > rIn.Right - rIn.Width / dockWidthDivisor)
									dw.DockingPosition = Alignment.Right;										
							}
						}
					}
					
				}

				if (curDockPos != dw.DockingPosition)
					RegisterForGraphicUpdate ();
			//}
			//base.onMouseMove (sender, e);
		}

		protected override void onDragEnter (object sender, DragDropEventArgs e)
		{
			base.onDragEnter (sender, e);
			
		}
		public override void onDragLeave (object sender, DragDropEventArgs e)
		{
			DockWindow dw = e.DragSource as DockWindow;
			//if (dw != null)
			//	dw.DockingPosition = Alignment.Undefined;
			base.onDragLeave (sender, e);
			
		}
			
		public void Dock(DockWindow dw){
			DockStack activeStack = this;

			if (Children.Count == 1) {
				Orientation = dw.DockingPosition.GetOrientation ();
				if (Children [0] is DockWindow dwc)
					dwc.DockingPosition = dw.DockingPosition.GetOpposite ();				
			} else if (Children.Count > 0 && dw.DockingPosition.GetOrientation () != Orientation) {
				activeStack = new DockStack (IFace);
				activeStack.Orientation = dw.DockingPosition.GetOrientation ();
				activeStack.Width = focusedChild.Width;
				activeStack.Height = focusedChild.Height;
				int idx = Children.IndexOf (focusedChild);
				RemoveChild (focusedChild);
				focusedChild.Height = Measure.Stretched;
				focusedChild.Width = Measure.Stretched;
				InsertChild (idx, activeStack);
				activeStack.AddChild (focusedChild);
				activeStack.stretchedChild = focusedChild;
				if (focusedChild is DockWindow dwf)
					dwf.DockingPosition = dw.DockingPosition.GetOpposite ();
				focusedChild = null;
			}

			Rectangle r = ClientRectangle;
			int vTreshold = (int)(r.Height * dockThresh);
			int hTreshold = (int)(r.Width * dockThresh);

			System.Diagnostics.Debug.WriteLine ("Docking {0} as {2} in {1}", dw.Name, activeStack.Name, dw.DockingPosition);
			switch (dw.DockingPosition) {
			case Alignment.Top:						
				dw.Height = vTreshold;
				dw.Width = Measure.Stretched;
				activeStack.InsertChild (0, dw);
				activeStack.InsertChild (1, new Splitter(IFace));
				break;
			case Alignment.Bottom:
				dw.Height = vTreshold;
				dw.Width = Measure.Stretched;
				activeStack.AddChild (new Splitter(IFace));
				activeStack.AddChild (dw);
				break;
			case Alignment.Left:
				dw.Width = hTreshold;
				dw.Height = Measure.Stretched;
				activeStack.InsertChild (0, dw);
				activeStack.InsertChild (1, new Splitter(IFace));
				break;
			case Alignment.Right:
				dw.Width = hTreshold;
				dw.Height = Measure.Stretched;
				activeStack.AddChild (new Splitter(IFace));
				activeStack.AddChild (dw);
				break;
			case Alignment.Center:
				dw.Width = dw.Height = Measure.Stretched;
				AddChild (dw);
				stretchedChild = dw;
				break;
			}
			dw.IsDocked = true;
		}
		public void Undock (DockWindow dw){			
			int idx = Children.IndexOf(dw);

			System.Diagnostics.Debug.WriteLine ("undocking child index: {0} ; name={1}; pos:{2} ; childcount:{3}",idx, dw.Name, dw.DockingPosition, Children.Count);

			RemoveChild(dw);

			if (Children.Count == 0)//TODO:empty Stack should be removed if not root stack I guess
				return;
			
			if (dw.DockingPosition == Alignment.Left || dw.DockingPosition == Alignment.Top) {				
				RemoveChild (idx);
				if (stretchedChild == dw) {
					stretchedChild = Children [idx];
					stretchedChild.Width = stretchedChild.Height = Measure.Stretched;
				}
			} else {
				RemoveChild (idx - 1);
				if (stretchedChild == dw) {
					stretchedChild = Children [idx-2];
					stretchedChild.Width = stretchedChild.Height = Measure.Stretched;
				}
			}

			if (Children.Count == 1) {
				DockStack dsp = Parent as DockStack;
				if (dsp == null) {
					Children [0].Width = Children [0].Height = Measure.Stretched;
					return;
				}				
				//remove level and move remaining obj to level above
				Widget g = Children [0];
				RemoveChild (g);
				idx = dsp.Children.IndexOf (this);
				dsp.RemoveChild (this);
				dsp.InsertChild (idx, g);
				g.Width = this.Width;
				g.Height = this.Height;
				if (dsp.stretchedChild == this)
					dsp.stretchedChild = g;
				dsp.checkAlignments ();
			} else
				checkAlignments ();
		}

		internal void checkAlignments () {
			DockWindow dw = Children[0] as DockWindow;
			if (dw != null)
				dw.DockingPosition = (Orientation == Orientation.Horizontal ? Alignment.Left : Alignment.Top);
			dw = Children[Children.Count - 1] as DockWindow;
			if (dw != null)
				dw.DockingPosition = (Orientation == Orientation.Horizontal ? Alignment.Right : Alignment.Bottom);
		}

		//read next value in config string until next ';'
		ReadOnlySpan<char> getConfAttrib (ReadOnlySpan<char> conf, ref int i) {
			int nextI = conf.Slice (i).IndexOf (';');
			ReadOnlySpan<char> tmp = conf.Slice (i, nextI);
			i += nextI + 1;
			return tmp;
		}
		/// <summary>
		/// Imports the config.
		/// </summary>
		/// <param name="conf">Conf.</param>
		/// <param name="dataSource">Data source for the docked windows</param>
		public void ImportConfig (ReadOnlySpan<char> conf, object dataSource = null) {
			lock (IFace.UpdateMutex) {
				ClearChildren ();
				stretchedChild = null;
				int i = 0;
				Orientation = EnumsNET.Enums.Parse<Orientation> (getConfAttrib (conf, ref i).ToString());
				importConfig (conf, ref i, dataSource);
			}
		}
		public string ExportConfig () {
			return Orientation.ToString() + ";" + exportConfig();
		}

		DockWindow importDockWinConfig (ReadOnlySpan<char> conf, ref int i, object dataSource){
			DockWindow dw = null;
			string wName = getConfAttrib (conf, ref i).ToString();
			try {
				dw = IFace.CreateInstance (wName) as DockWindow;	
			} catch (Exception ex){
				Console.WriteLine ($"[importDockWinConfig]{ex}");
				dw = new DockWindow (IFace);						
			}

			dw.Name = wName;
			dw.Width = Measure.Parse (getConfAttrib (conf, ref i).ToString());
			dw.Height = Measure.Parse (getConfAttrib (conf, ref i).ToString());
			dw.DockingPosition = EnumsNET.Enums.Parse<Alignment> (getConfAttrib (conf, ref i).ToString());
			dw.savedSlot = Rectangle.Parse (getConfAttrib (conf, ref i).ToString());
			dw.wasResizable = Boolean.Parse (getConfAttrib (conf, ref i).ToString());
			dw.Resizable = false;
			
			dw.DataSource = dataSource;
			return dw;
		}
		void importConfig (ReadOnlySpan<char> conf, ref int i, object dataSource) {						
			if (conf [i++] != '(')
				return;
			DockWindow dw = null;			
			while (i < conf.Length - 4) {
				string sc = conf.Slice (i, 4).ToString();
				i += 4;
				switch (sc) {
				case "TVI;":
					TabView tv = new TabView (IFace, "DockingTabView");
					tv.Width = Measure.Parse (getConfAttrib (conf, ref i).ToString());
					tv.Height = Measure.Parse (getConfAttrib (conf, ref i).ToString());
					this.AddChild (tv);
					i++;					
					while (conf [i] != ')') {
						dw = importDockWinConfig (conf, ref i, dataSource);
						tv.AddItem (dw);
						dw.IsDocked = true;
					}
					i++;
					break;
				case "WIN;":		
					dw = importDockWinConfig (conf, ref i, dataSource);			
					this.AddChild (dw);
					dw.IsDocked = true;
					break;
				case "STK;":
					DockStack ds = new DockStack (IFace);
					ds.Width = Measure.Parse (getConfAttrib (conf, ref i).ToString());
					ds.Height = Measure.Parse (getConfAttrib (conf, ref i).ToString());
					ds.Orientation = EnumsNET.Enums.Parse<Orientation> (getConfAttrib (conf, ref i).ToString());

					this.AddChild (ds);

					ds.importConfig (conf, ref i, dataSource);
					break;
				case "SPL;":
					Splitter sp = new Splitter (IFace);
					sp.Width = Measure.Parse (getConfAttrib (conf, ref i).ToString());
					sp.Height = Measure.Parse (getConfAttrib (conf, ref i).ToString());
					sp.Thickness = int.Parse (getConfAttrib (conf, ref i));
					this.AddChild (sp);
					break;
				}
				char nextC = conf [i++];
				if (nextC == ')')
					break;
			}
		}
		string exportConfig () {
			StringBuilder tmp = new StringBuilder("(");

			for (int i = 0; i < Children.Count; i++) {
				if (Children [i] is DockWindow dw)					
					tmp.Append (dw.GetDockConfigString());
				else if (Children [i] is TabView tv) {
					tmp.Append ($"TVI;{tv.Width};{tv.Height};(");
					foreach (DockWindow d in tv.Items)
						tmp.Append (d.GetDockConfigString().Substring(4));					
					tmp.Append (")");
				}else if (Children [i] is DockStack ds)
					tmp.Append ($"STK;{ds.Width};{ds.Height};{ds.Orientation};{ds.exportConfig()}");
				else if (Children [i] is Splitter sp) 
					tmp.Append (string.Format("SPL;{0};{1};{2};", sp.Width, sp.Height, sp.Thickness));
				if (i < Children.Count - 1)
					tmp.Append ("|");				
			}

			tmp.Append (")");
			return tmp.ToString ();
		}
	}
}

