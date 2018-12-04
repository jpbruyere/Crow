//
//  ImlVisualEditor.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using Crow;
using System.Threading;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;
using System.Collections.Generic;
using Crow.IML;
using System.Text;
using System.Xml;
using System.Diagnostics;
using Cairo;

namespace Crow.Coding
{
	public class ImlVisualEditor : Editor
	{
		#region CTOR
		public ImlVisualEditor () : base()
		{
			imlVE = new DesignInterface ();
			initCommands ();
		}
		#endregion

		protected override void onInitialized (object sender, EventArgs e)
		{
			initIcons ();
			base.onInitialized (sender, e);
		}
		DesignInterface imlVE;
		GraphicObject selectedItem;
		ImlProjectItem imlProjFile;

		bool editorIsDirty = false;//needed when tree is empty
		bool drawGrid, snapToGrid;
		int gridSpacing, zoom = 100;
		Measure designWidth, designHeight;
		bool updateEnabled;

		Picture icoMove, icoStyle;
		Rectangle rIcons = default(Rectangle);
		Size iconSize = new Size(11,11);

		public List<Crow.Command> Commands;
		Crow.Command cmdDelete;

		void initCommands () {
			cmdDelete = new Crow.Command (new Action (() => deleteObject (SelectedItem)))
				{ Caption = "Delete", Icon = new SvgPicture ("#Crow.Coding.icons.save.svg"), CanExecute = true };
			Commands = new List<Crow.Command> (new Crow.Command[] { cmdDelete });
		}

		void initIcons () {
			icoMove = new SvgPicture ("#Crow.Coding.icons.move-arrows.svg");

//			icoStyle = new SvgPicture ();
//			icoStyle.Load (IFace, "#Crow.Coding.icons.palette.svg");
		}

		[DefaultValue(true)]
		public bool DrawGrid {
			get { return drawGrid; }
			set {
				if (drawGrid == value)
					return;
				drawGrid = value;
				NotifyValueChanged ("DrawGrid", drawGrid);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(true)]
		public bool SnapToGrid {
			get { return snapToGrid; }
			set {
				if (snapToGrid == value)
					return;
				snapToGrid = value;
				NotifyValueChanged ("SnapToGrid", snapToGrid);
			}
		}
		[DefaultValue(10)]
		public int GridSpacing {
			get { return gridSpacing; }
			set {
				if (gridSpacing == value)
					return;
				gridSpacing = value;
				NotifyValueChanged ("GridSpacing", gridSpacing);
				RegisterForRedraw ();
			}
		}
		[DefaultValue(100)]
		public int Zoom {
			get { return zoom; }
			set {
				if (zoom == value)
					return;
				
				zoom = value;
				NotifyValueChanged ("Zoom", zoom);
				Width = (int)(designWidth * zoom / 100.0);
				Height = (int)(designHeight * zoom / 100.0);
			}
		}
		[DefaultValue("512")]
		public Measure DesignWidth {
			get { return designWidth; }
			set { 
				if (designWidth == value)
					return;
				designWidth = value;
				NotifyValueChanged ("DesignWidth", designWidth);
				Width = (int)(designWidth * zoom / 100.0);
			}
		}
		[DefaultValue("400")]
		public Measure DesignHeight {
			get { return designHeight; }
			set {
				if (designHeight == value)
					return;
				designHeight = value;
				NotifyValueChanged ("DesignHeight", designHeight);
				Height = (int)(designHeight * zoom / 100.0);
			}
		}

		public GraphicObject SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem == value)
					return;
				selectedItem = value;

				if (selectedItem == null)
					cmdDelete.CanExecute = false;
				else
					cmdDelete.CanExecute = true;
				
				NotifyValueChanged ("SelectedItem", selectedItem);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>PoinprojFilever the widget</summary>
		public virtual GraphicObject HoverWidget
		{
			get { return imlVE.HoverWidget; }
			set {
				if (HoverWidget == value)
					return;

				imlVE.HoverWidget = value;

				NotifyValueChanged ("HoverWidget", HoverWidget);
			}
		}
		/// <summary>
		/// use to disable update if tab is not the visible one
		/// </summary>
		public bool UpdateEnabled {
			get { return updateEnabled; }
			set { 
				if (value == updateEnabled)
					return;
				updateEnabled = value;
				NotifyValueChanged ("UpdateEnabled", updateEnabled);
			}
		}

		public List<GraphicObject> GraphicTree {
			get { return imlVE.GraphicTree; }
		}

		[XmlIgnore]public List<LQIList> LQIs {
			get { return imlVE.LQIs; }
		}

		#region editor overrides
		public override ProjectFile ProjectNode {
			get {
				return base.ProjectNode;
			}
			set {
				base.ProjectNode = value;
				imlProjFile = projFile as ImlProjectItem;
				imlVE.ProjFile = imlProjFile;
			}
		}

		protected override bool EditorIsDirty {
			get { return imlProjFile == null ? false :
				imlProjFile.Instance == null ? editorIsDirty :
				imlProjFile.Instance.design_HasChanged | editorIsDirty; }
			set {
				editorIsDirty = value;
				if (GraphicTree.Count == 0)
					return;
				if (GraphicTree [0] != null)
					GraphicTree [0].design_HasChanged = value;			
			}
		}
		protected override bool IsReady {
			get { return updateEnabled && imlVE != null && imlProjFile != null; }
		}

		protected override void updateProjFileFromEditor ()
		{
			Debug.WriteLine("\t\tImlEditor updateProjFileFromEditor");
			try {
				if (imlProjFile.Instance == null)
					projFile.UpdateSource(this, @"<?xml version=""1.0""?>");
				else
					projFile.UpdateSource(this, imlProjFile.Instance.GetIML());
			} catch (Exception ex) {
				Error = ex.InnerException;
				if (Monitor.IsEntered(imlVE.UpdateMutex))
					Monitor.Exit (imlVE.UpdateMutex);
			}
		}
		protected override void updateEditorFromProjFile () {
			Debug.WriteLine("\t\tImlEditor updateEditorFromProjFile");
			try {
				string selItemDesignID = null;
				if (SelectedItem!=null)
					selItemDesignID = SelectedItem.design_id;
				imlVE.ClearInterface();
				Instantiator.NextInstantiatorID = 0;
				imlVE.Styling = projFile.Project.solution.Styling;
				imlVE.DefaultValuesLoader.Clear();
				imlVE.DefaultTemplates = projFile.Project.solution.DefaultTemplates;
				imlVE.Instantiators = new Dictionary<string, Instantiator>();

				//prevent error on empty file
				bool emptyFile = true;
				string src = projFile.Source;
				using (Stream s = new MemoryStream (Encoding.UTF8.GetBytes (src))) {
					using (XmlReader itr = XmlReader.Create (s)) {
						while(itr.Read()){
							if (itr.NodeType == XmlNodeType.Element){
								emptyFile = false;
								break;
							}
						}
					}
				}
				GraphicObject go = null;
				Error = null;

				if (emptyFile){
					imlProjFile.Instance = null;
				}else{
					imlVE.LoadIMLFragment(src);
					imlProjFile.Instance = imlVE.GraphicTree[0];
					if (selItemDesignID!=null)
						imlProjFile.Instance.FindByDesignID(selItemDesignID,out go);						

				}
				SelectedItem = go;
			} catch (Exception ex) {
				Error = ex;
				Debug.WriteLine ("Error Loading ui in Design iface\n" + ex.ToString ());
				if (Monitor.IsEntered(imlVE.UpdateMutex))
					Monitor.Exit (imlVE.UpdateMutex);
			}
		}
		protected override void updateCheckPostProcess ()
		{
			if (Error != null) {
				RegisterForRedraw ();
				return;
			}
			imlVE.Update ();
			bool isDirty = false;

			lock (imlVE.RenderMutex)
				isDirty = imlVE.IsDirty;

			if (isDirty) {				
				lock (IFace.UpdateMutex)
					RegisterForRedraw ();				
			}
		}
		#endregion

		#region GraphicObject overrides
		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);
			switch (layoutType) {
			case LayoutingType.Width:
			case LayoutingType.Height:
				imlVE.ProcessResize (new Size(designWidth,designHeight));
				break;
			}
		}

		public override void onMouseMove (object sender, MouseMoveEventArgs e)
		{
			base.onMouseMove (sender, e);

			GraphicObject oldHW = HoverWidget;
			Rectangle scr = this.ScreenCoordinates (this.getSlot ());
			ProcessMouseMove (e.X - scr.X, e.Y - scr.Y);

			GraphicObject newHW = HoverWidget;

			if (draggedObj != null) {
				if (draggedObj.Parent == null) {
					if (tryAddObjectTo (newHW, draggedObj)) {
						RegisterForRedraw ();
						return;
					}
				} else if (newHW != draggedObj) {
					//lock (imlVE.UpdateMutex) {
					ILayoutable possibleParent = getPossibleParent (newHW, draggedObj);
					if (possibleParent == null) {
						Group g = newHW.Parent as Group;
						if (g != null && g != draggedObj) {
							removeObject (draggedObj);
							g.InsertChild (g.Children.IndexOf (newHW), draggedObj);
							RegisterForRedraw ();
							return;
						}
					} else if (possibleParent != draggedObj.Parent) {
						removeObject (draggedObj);
						if (tryAddObjectTo (possibleParent, draggedObj)) {
							RegisterForRedraw ();
							return;
						}
					}
					//}
				}
			}			

			if (oldHW == newHW)
				return;
			//RegisterForRedraw ();

		}
		public override void onMouseEnter (object sender, MouseMoveEventArgs e)
		{
			base.onMouseEnter (sender, e);
			if (IFace.DragAndDropOperation != null && draggedObj == null)
				return;
			IFace.FocusedWidget = this;
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
//			IFace.FocusedWidget = null;
		}
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{
			if (e.Mouse.RightButton == ButtonState.Pressed) {
				base.onMouseDown (sender, e);
				return;
			}
			SelectedItem = HoverWidget;

//			if (SelectedItem != null && projFile != null) {
//				projFile.CurrentLine = SelectedItem.design_line;
//				projFile.CurrentColumn = SelectedItem.design_column;
//			}

		}

		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			Rectangle cb = new Rectangle (0, 0, designWidth, designHeight);// ClientRectangle;

			gr.Save ();

			double z = zoom / 100.0;

			gr.Scale (z, z);

			if (drawGrid) {
				double gridLineWidth = 0.2 / z;
				double glhw = gridLineWidth / 2.0;
				int nbLines = cb.Width / gridSpacing;
				double d = cb.Left + gridSpacing;
				for (int i = 0; i < nbLines; i++) {
					gr.MoveTo (d - glhw, cb.Y);
					gr.LineTo (d - glhw, cb.Bottom);
					d += gridSpacing;
				}
				nbLines = cb.Height / gridSpacing;
				d = cb.Top + gridSpacing;
				for (int i = 0; i < nbLines; i++) {
					gr.MoveTo (cb.X, d - glhw);
					gr.LineTo (cb.Right, d - glhw);
					d += gridSpacing;
				}
				gr.LineWidth = gridLineWidth;
				Foreground.SetAsSource (gr, cb);
				gr.Stroke ();
			}

			lock (imlVE.RenderMutex) {
				gr.SetSourceSurface (imlVE.surf, cb.Left, cb.Top);
				gr.Paint ();
				imlVE.IsDirty = false;
			}
			if (Error == null) {
				gr.SetSourceColor (Color.Black);
				gr.Rectangle (cb, 1.0 / z);
			} else {
				gr.SetSourceColor (Color.LavenderBlush);
				gr.Rectangle (cb, 2.0 / z);
				string[] lerrs = Error.ToString ().Split ('\n');
				Point p = cb.Center;
				p.Y -= lerrs.Length * 20;
				foreach (string le in lerrs) {
					drawCenteredTextLine(gr, p, le);
					p.Y += 20;

				}
			}
			gr.Stroke ();

			Rectangle hr;

			if (SelectedItem?.Parent != null) {

				gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
				gr.SetFontSize (Font.Size);
				gr.FontOptions = Interface.FontRenderingOptions;
				gr.Antialias = Interface.Antialias;

				GraphicObject g = SelectedItem;
				hr = g.ScreenCoordinates (g.getSlot ());

//				Rectangle rIcons = new Rectangle (iconSize);
//				rIcons.Width *= 4; 
//				rIcons.Top = hr.Bottom;
//				rIcons.Left = hr.Right - rIcons.Width + iconSize.Width;
				Rectangle rIcoMove = new Rectangle (hr.BottomRight, iconSize);
//				Rectangle rIcoStyle = rIcoMove;
//				rIcoStyle.Left += iconSize.Width + 4;

				using (Surface mask = new ImageSurface (Format.Argb32, cb.Width, cb.Height)) {
					using (Context ctx = new Context (mask)) {
						ctx.Save();
						ctx.SetSourceRGBA(1.0,1.0,1.0,0.4);
						ctx.Paint ();
						ctx.Rectangle (hr);
						ctx.Operator = Operator.Clear;
						ctx.Fill ();
					}

					gr.SetSourceSurface (mask, 0, 0);
					gr.Paint ();

					using (Surface ol = new ImageSurface (Format.Argb32, cb.Width, cb.Height)) {
						using (Context ctx = new Context (ol)) {
							ctx.SetSourceColor (Color.Black);
							drawDesignOverlay (ctx, g, cb, hr, 0.4 / z, 6.5);
						}
							
						gr.SetSourceSurface (ol, 0, 0);
						gr.Paint ();
					}

					drawIcon (gr, icoMove, rIcoMove);
					//drawIcon (gr, icoStyle, rIcoStyle);

				}						
			}
			if (HoverWidget != null) {
				hr = HoverWidget.ScreenCoordinates (HoverWidget.getSlot ());
				gr.SetSourceColor (Color.SkyBlue);
				//gr.SetDash (new double[]{ 5.0, 3.0 }, 0.0);
				gr.Rectangle (hr, 0.4 / z);
			}
			gr.Restore ();
		}

		void drawIcon (Context gr, Picture pic, Rectangle r) {
//			gr.SetSourceColor (Color.Black);
//			CairoHelpers.CairoRectangle (gr, r.Inflated (1), 2, 1.0);
			gr.SetSourceColor (Color.White);
			CairoHelpers.CairoRectangle (gr, r.Inflated (1), 2);
			gr.Fill ();
			gr.Operator = Operator.Clear;
			pic.Paint (gr, r);
			gr.Operator = Operator.Over;
		}
		void drawDesignOverlay (Context gr, GraphicObject g, Rectangle cb, Rectangle hr, double coteStroke, double space = 6.5){
			double z = zoom / 100.0;
			double coteW = 3, coteL = 5;
			bool fill = true;
			Cairo.PointD p1 = new Cairo.PointD (hr.X + 0.5, hr.Y - space);
			Cairo.PointD p2 = new Cairo.PointD (hr.Right - 0.5, hr.Y - space);

			if (p1.Y < cb.Top) {
				if (hr.Bottom > cb.Bottom - space)
					p1.Y = p2.Y = hr.Bottom - space;
				else
					p1.Y = p2.Y = hr.Bottom + space;
			}

			if (g.Width.IsFit)
				gr.DrawCoteInverse (p1, p2, coteStroke, fill, coteW, coteL);
			else if (g.Width.IsRelativeToParent) {
				gr.DrawCote (p1, p2, coteStroke, fill, coteW, coteL);
				if (g.Width.Value < 100)
					drawCenteredTextLine (gr, p1.Add(p2.Substract(p1).Divide (2)), g.Width.ToString());
			}else if (g.Width.IsFixed) {
				gr.DrawCoteFixed (p1, p2, coteStroke * 2.0, coteW);
				drawCenteredTextLine (gr, p1.Add(p2.Substract(p1).Divide (2)), g.Width.Value.ToString());
			}

			p1 = new Cairo.PointD (hr.X - space, hr.Top + 0.5);
			p2 = new Cairo.PointD (hr.X - space, hr.Bottom - 0.5);

			if (p1.X < cb.Left) {
				if (hr.Right > cb.Right - space)
					p1.X = p2.X = hr.Right - space;
				else
					p1.X = p2.X = hr.Right + space;
			} 
			if (g.Height.IsFit)
				gr.DrawCoteInverse (p1, p2, coteStroke, fill, coteW, coteL);
			else if (g.Height.IsRelativeToParent){
				gr.DrawCote (p1, p2, coteStroke, fill, coteW, coteL);
				if (g.Height.Value < 100)
					drawCenteredTextLine (gr, p1.Add(p2.Substract(p1).Divide (2)), g.Height.ToString());
			}else if (g.Width.IsFixed) {
				gr.DrawCoteFixed (p1, p2, coteStroke * 2.0, coteW);
				drawCenteredTextLine (gr, p1.Add(p2.Substract(p1).Divide (2)), g.Height.Value.ToString());
			}

			//				hr.Inflate (2);
			//gr.SetDash (new double[]{ 1.0, 4.0 }, 0.0);
			//gr.SetSourceColor (Color.Grey);
//			gr.Rectangle (hr,coteStroke);
//			gr.Stroke ();
			gr.Operator = Operator.Over;			
		}

		void drawCenteredTextLine (Context gr, Point center, string txt){
			drawCenteredTextLine (gr, new PointD(center.X,center.Y), txt);
		}
		void drawCenteredTextLine (Context gr, PointD center, string txt){
			if (string.IsNullOrEmpty(txt))
				return;
			FontExtents fe = gr.FontExtents;
			TextExtents te = gr.TextExtents (txt);

			Rectangle rText = new Rectangle(
				(int)(center.X - te.Width / 2), (int)(center.Y - (fe.Ascent + fe.Descent) / 2),
				(int)te.Width, (int)(fe.Ascent + fe.Descent));

			gr.Operator = Operator.Clear;
			Rectangle r = rText;
			r.Inflate (2);
			gr.Rectangle (r);
			gr.Fill ();
			gr.Operator = Operator.Over;

			gr.MoveTo (rText.X, rText.Y + fe.Ascent);
			gr.ShowText (txt);
			gr.Fill ();

		}
		protected override void onDragEnter (object sender, DragDropEventArgs e)
		{
			base.onDragEnter (sender, e);
			GraphicObjectDesignContainer godc = e.DragSource.DataSource as GraphicObjectDesignContainer;
			if (godc == null)
				return;
			createDraggedObj (godc.CrowType);
		}
		protected override void onDragLeave (object sender, DragDropEventArgs e)
		{
			base.onDragLeave (sender, e);
			GraphicObjectDesignContainer godc = e.DragSource.DataSource as GraphicObjectDesignContainer;
			if (godc == null)
				return;
			ClearDraggedObj ();
		}

		protected override void onStartDrag (object sender, DragDropEventArgs e)
		{
			base.onStartDrag (sender, e);
			if (SelectedItem == null)
				return;
			
			GraphicObject dumy = new GraphicObject (IFace);
			dumy.EndDrag += dumyOnEndDrag;
			dumy.Drop += dumyOnDrop;
			dumy.IsDragged = true;
			IFace.ActiveWidget = dumy;
			e.DragSource.IsDragged = false;
			IFace.DragAndDropOperation.DragSource = dumy;
			draggedObj = SelectedItem;
			int dragIconSize = 48;
			lock (IFace.UpdateMutex) {
				IFace.DragImageHeight = dragIconSize;
				IFace.DragImageWidth = dragIconSize;
				IFace.DragImage = draggedObj.CreateIcon(dragIconSize);
			}					
			removeObject (draggedObj);
			SelectedItem = null;
			HoverWidget = null;
		}
		void dumyOnEndDrag (object sender, DragDropEventArgs e)
		{			
			IFace.ClearDragImage ();
		}
		void dumyOnDrop (object sender, DragDropEventArgs e)
		{
			ClearDraggedObj (false);
			IFace.ClearDragImage ();
		}
		#endregion


		#region draggedObj handling

		public GraphicObject draggedObj = null;

		void createDraggedObj (Type crowType) {
			lock (imlVE.UpdateMutex) {
				draggedObj = imlVE.CreateITorFromIMLFragment ("<" + crowType.Name + "/>").CreateInstance ();
			}
		}

		public void ClearDraggedObj (bool removeFromTree = true) {
			if (removeFromTree)
				deleteObject (draggedObj);			
			draggedObj = null;
		}
		#endregion

		void removeObject (GraphicObject go) {
			if (go == null)
				return;
			if (go.Parent == null)
				return;		
//			lock (imlVE.UpdateMutex) {
				Interface i = go.Parent as Interface;
				if (i != null) {
					i.RemoveWidget (go);
					imlProjFile.Instance = null;
				} else {
					Container c = go.Parent as Container;
					if (c != null) 
						c.SetChild (null);
					else {
						TemplatedContainer tc = go.Parent as TemplatedContainer;
						if (tc != null)
							tc.Content = null;
						else {
							Group g = go.Parent as Group;
							if (g != null)
								g.RemoveChild (go);
						}
					}
				}					
				EditorIsDirty = true;
			//}
		}
		void deleteObject (GraphicObject go) {
			if (go == null)
				return;
			//lock (imlVE.UpdateMutex) {
				removeObject (go);
				go.Dispose ();
			//}
		}

		ILayoutable getPossibleParent (ILayoutable parent, GraphicObject go) {
			if (go == null)
				return null;
//			lock (imlVE.UpdateMutex) {
				
				Interface i = null;
				if (parent == null)
					i = imlVE;
				else
					i = parent as Interface;
				if (i != null)
					return i.GraphicTree.Count > 0 ? null : i;
				
				Container c = parent as Container;
				if (c != null)
					return c.Child == null || c.Child == go ? c : null;						
				
				TemplatedContainer tc = parent as TemplatedContainer;
				if (tc != null)
					return tc.Content == null || tc.Content == go? tc : null;
				
				return parent as Group;
//			}
		}
		bool tryAddObjectTo (ILayoutable parent, GraphicObject go) {
			if (go == null)
				return false;
//			lock (imlVE.UpdateMutex) {
			Interface i = null;
			if (parent == null)
				i = imlVE;
			else
				i = parent as Interface;
			if (i != null) {
				if (i.GraphicTree.Count > 0)
					return false;
				i.AddWidget (go);
				imlProjFile.Instance = go;
				EditorIsDirty = true;
				return true;
			}
			Container c = parent as Container;
			if (c != null) {
				if (c.Child != null)
					return false;
					//return tryAddObjectTo (c.Parent, go);
				c.SetChild (go);
				EditorIsDirty = true;
				return true;
			}
			TemplatedContainer tc = parent as TemplatedContainer;
			if (tc != null) {
				if (tc.Content != null)
					return false;
				//return tryAddObjectTo (c.Parent, go);
				tc.Content = (go);
				EditorIsDirty = true;
				return true;
			}
			Group g = parent as Group;
			if (g != null) {
				g.AddChild (go);
				EditorIsDirty = true;
				return true;
			}
			return false;//tryAddObjectTo (parent.Parent, go);
//			}
		}



		void WidgetCheckOver (GraphicObject go, MouseMoveEventArgs e){
			Type tGo = go.GetType();
			if (typeof(TemplatedGroup).IsAssignableFrom (tGo)) {
				
			} else if (typeof(TemplatedContainer).IsAssignableFrom (tGo)) {
				TemplatedContainer c = go as TemplatedContainer;
				if (c.Content?.MouseIsIn (e.Position) == true) {					
					WidgetCheckOver (c.Content, e);
					return;
				}
			} else if (typeof(TemplatedControl).IsAssignableFrom (tGo)) {
			} else if (typeof(Group).IsAssignableFrom (tGo)) {
				Group c = go as Group;
				for (int i = c.Children.Count -1; i >= 0; i--) {
					if (c.Children[i].MouseIsIn (e.Position)) {					
						WidgetCheckOver (c.Children[i], e);
						return;
					}
				}
			} else if (typeof(Crow.Container).IsAssignableFrom (tGo)) {
				Crow.Container c = go as Crow.Container;
				if (c.Child?.MouseIsIn (e.Position)==true) {					
					WidgetCheckOver (c.Child, e);
					return;
				}
			}
			HoverWidget = go;
			WidgetMouseEnter (go, e);
		}
		void WidgetMouseLeave (GraphicObject go, MouseMoveEventArgs e){

		}
		void WidgetMouseEnter (GraphicObject go, MouseMoveEventArgs e){

		}
		void WidgetMouseMove (GraphicObject go, MouseMoveEventArgs e){}

		public bool ProcessMouseMove(int x, int y)
		{
			int deltaX = x - imlVE.Mouse.X;
			int deltaY = y - imlVE.Mouse.Y;
			imlVE.Mouse.X = x;
			imlVE.Mouse.Y = y;
			MouseMoveEventArgs e = new MouseMoveEventArgs (x, y, deltaX, deltaY);
			e.Mouse = imlVE.Mouse;

			if (imlVE.ActiveWidget != null) {
				//TODO, ensure object is still in the graphic tree
				//send move evt even if mouse move outside bounds
				WidgetMouseMove (imlVE.ActiveWidget, e);
				return true;
			}

			if (HoverWidget != null) {
				//TODO, ensure object is still in the graphic tree
				//check topmost graphicobject first
				GraphicObject tmp = HoverWidget;
				GraphicObject topc = null;
				while (tmp is GraphicObject) {
					topc = tmp;
					tmp = tmp.LogicalParent as GraphicObject;
				}
				int idxhw = imlVE.GraphicTree.IndexOf (topc);
				if (idxhw != 0) {
					int i = 0;
					while (i < idxhw) {
						if (imlVE.GraphicTree [i].LogicalParent == imlVE.GraphicTree [i].Parent) {
							if (imlVE.GraphicTree [i].MouseIsIn (e.Position)) {
								while (imlVE.HoverWidget != null) {
									WidgetMouseLeave (imlVE.HoverWidget, e);
									imlVE.HoverWidget = imlVE.HoverWidget.LogicalParent as GraphicObject;
								}

								WidgetCheckOver (GraphicTree [i], e);
								return true;
							}
						}
						i++;
					}
				}


				if (imlVE.HoverWidget.MouseIsIn (e.Position)) {
					WidgetCheckOver (imlVE.HoverWidget, (e));
					return true;
				} else {
					WidgetMouseLeave (imlVE.HoverWidget, e);
					//seek upward from last focused graph obj's
					while (imlVE.HoverWidget.LogicalParent as GraphicObject != null) {
						imlVE.HoverWidget = imlVE.HoverWidget.LogicalParent as GraphicObject;
						if (imlVE.HoverWidget.MouseIsIn (e.Position)) {
							WidgetCheckOver (imlVE.HoverWidget, e);
							return true;
						} else
							WidgetMouseLeave (imlVE.HoverWidget, e);
					}
				}
			}

			//top level graphic obj's parsing
			lock (imlVE.GraphicTree) {
				for (int i = 0; i < imlVE.GraphicTree.Count; i++) {
					GraphicObject g = imlVE.GraphicTree [i];
					if (g.MouseIsIn (e.Position)) {
						WidgetCheckOver (g, e);
						return true;
					}
				}
			}
			imlVE.HoverWidget = null;
			return false;

		}

		void GTView_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			SelectedItem = e.NewValue as GraphicObject;
		}


		public override void onKeyDown (object sender, KeyEventArgs e)
		{
		
			switch (e.Key) {
			case Key.Delete:
				if (selectedItem == null)
					return;
				deleteObject (selectedItem);
				break;
			case Key.Escape:
				SelectedItem = null;
				break;
			}
		}
	}
}
