//
// Shape.cs
//
// Author:
//       jp <>
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
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;
using System.Text;
using vkvg;

namespace Crow
{
	public class PathParser : StringReader
	{
		public PathParser (string str) : base (str) {}

		public double ReadDouble () {
			StringBuilder tmp = new StringBuilder();

			while (Peek () >= 0) {				
				char c = (char)Read();
				if (c.IsWhiteSpaceOrNewLine()) {
					if (tmp.Length == 0)
						continue;
					else
						break;
				} else if (c == ',')
					break;				
				tmp.Append (c);
			}
			return double.Parse (tmp.ToString ());
		}
		
	}
	public class Shape : Widget
	{
		#region CTOR
		protected Shape () : base() {}
		public Shape (Interface iface) : base (iface) {}
		#endregion

		string path;
		double strokeWidth;
        Size size;

		public string Path {
			get { return path; }
			set {
				if (path == value)
					return;
				path = value;
				contentSize = default (Size);
				NotifyValueChanged ("Path", path);
				RegisterForGraphicUpdate ();
			}
		}
		[DefaultValue(1.0)]
		public double StokeWidth {
			get { return strokeWidth; }
			set {
				if (strokeWidth == value)
					return;
				strokeWidth = value;
				contentSize = default (Size);
				NotifyValueChanged ("StrokeWidth", strokeWidth);
				RegisterForGraphicUpdate ();
			}
		}
        [DefaultValue("0,0")]
        public Size Size
        {
            get { return size; }
            set
            {
                if (size == value)
                    return;
                size = value;
                contentSize = default(Size);
                NotifyValueChanged("Size", size);
                RegisterForLayouting(LayoutingType.Sizing);
            }
        }
        protected override void onDraw(Context gr)
        {

            if (string.IsNullOrEmpty(path))
                return;

            gr.Save();

            Rectangle r = ClientRectangle;

			double sx = (double)r.Width / (double)(contentSize.Width == 0? size.Width : contentSize.Width);
            double sy = (double)r.Height / (double)(contentSize.Height == 0 ? size.Height : contentSize.Height);            

            gr.Translate(r.Left, r.Top);
            gr.Scale (sx,sy);
            
			executePath (gr);

			Background.SetAsSource (gr, r);
			gr.FillPreserve ();
			gr.LineWidth = strokeWidth;
			Foreground.SetAsSource (gr, r);
			gr.Stroke ();
            gr.Restore();
		}

		void executePath (Context gr){
			using (PathParser sr = new PathParser (path)) {
				while (sr.Peek () >= 0) {
					char c = (char)sr.Read ();
					if (c.IsWhiteSpaceOrNewLine ())
						continue;
					switch (c) {
					case 'M':						
						gr.MoveTo (sr.ReadDouble (), sr.ReadDouble ());
						break;
					case 'm':
						gr.RelMoveTo (sr.ReadDouble (), sr.ReadDouble ());
						break;
					case 'L':
						gr.LineTo (sr.ReadDouble (), sr.ReadDouble ());
						break;
					case 'l':
						gr.RelLineTo (sr.ReadDouble (), sr.ReadDouble ());
						break;
					case 'C':
						gr.CurveTo (sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble ());
						break;
					case 'c':
						gr.RelCurveTo (sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble (), sr.ReadDouble ());
						break;
					case 'Z':
						gr.ClosePath ();
						break;
					case 'F':
						gr.Fill ();
						break;
					case 'G':
						gr.Stroke ();
						break;
					default:
						throw new Exception ("Invalid character in path string of Shape control");
					}
				}
			}
		}
		protected override int measureRawSize (LayoutingType lt)
		{
			//if ((lt == LayoutingType.Width && contentSize.Width == 0) || (lt == LayoutingType.Height && contentSize.Height == 0)) {
			//             if (size != default(Size))
			//                 contentSize = size;
			//             else
			//             {
			//                 using (Surface drawing = new Surface(IFace.dev, 1, 1))
			//                 {
			//                     using (Context ctx = new Context(drawing))
			//                     {
			//                         executePath(ctx);
			//                         Rectangle r = ctx.StrokeExtents();
			//                         contentSize = new Size(r.Right, r.Bottom);
			//                     }
			//                 }
			//             }
			//}
			contentSize = size;
			return lt == LayoutingType.Width ?
				contentSize.Width + 2 * Margin: contentSize.Height + 2 * Margin;
		}
	}
}

