//
// GraphicObjectDesignContainer.cs
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
using Cairo;

namespace Crow.Coding
{
	public class GraphicObjectDesignContainer
	{
		#region CTOR
		public GraphicObjectDesignContainer (Type crowType)
		{
			CrowType = crowType;
		}
		#endregion

		int dragIconSize = 32;
		public Type CrowType;

		public string IconPath {
			get { return "#Crow.Coding.icons.toolbox." + CrowType.FullName + ".svg"; }
		}
		public string DisplayName {
			get { return CrowType.Name; }
		}
		void onStartDrag (object sender, EventArgs e)
		{
			GraphicObject go = sender as GraphicObject;

			lock (go.IFace.UpdateMutex) {				
				go.IFace.DragImageHeight = dragIconSize;
				go.IFace.DragImageWidth = dragIconSize;
				SvgPicture pic = new SvgPicture ();
				pic.Load (go.IFace, IconPath);
				ImageSurface img = new ImageSurface (Format.Argb32, dragIconSize, dragIconSize);
				using (Context ctx = new Context (img)) {
					Rectangle r = new Rectangle (0, 0, dragIconSize, dragIconSize);
					pic.Paint (ctx, r);	
					ctx.Operator = Operator.In;
					ctx.SetSourceRGBA (1.0, 1.0, 1.0, 1.0);
					ctx.Rectangle (r);
					ctx.Fill ();

				}
				go.IFace.DragImage = img;
			}
		}
		void onEndDrag (object sender, DragDropEventArgs e)
		{
			(sender as GraphicObject).IFace.ClearDragImage ();

		}
		void onDrop (object sender, DragDropEventArgs e)
		{
			ImlVisualEditor imlVE = e.DropTarget as ImlVisualEditor;
			if (imlVE != null)
				imlVE.ClearDraggedObj (false);
			(sender as GraphicObject).IFace.ClearDragImage ();
		}
	}
}

