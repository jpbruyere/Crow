// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using Crow.Cairo;

namespace Crow
{
	public class PathParser : StringReader
	{
		public PathParser (string str) : base (str) {}

		double readDouble () {
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
		public void Draw (Context gr) {
			while (Peek () >= 0) {
				char c = (char)Read ();
				if (c.IsWhiteSpaceOrNewLine ())
					continue;
				switch (c) {
				case 'M':
					gr.MoveTo (readDouble (), readDouble ());
					break;
				case 'm':
					gr.RelMoveTo (readDouble (), readDouble ());
					break;
				case 'L':
					gr.LineTo (readDouble (), readDouble ());
					break;
				case 'l':
					gr.RelLineTo (readDouble (), readDouble ());
					break;
				case 'C':
					gr.CurveTo (readDouble (), readDouble (), readDouble (), readDouble (), readDouble (), readDouble ());
					break;
				case 'c':
					gr.RelCurveTo (readDouble (), readDouble (), readDouble (), readDouble (), readDouble (), readDouble ());
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

			using (PathParser parser = new PathParser (path))
				parser.Draw (gr);
				
			Background.SetAsSource (gr, r);
			gr.FillPreserve ();
			gr.LineWidth = strokeWidth;
			Foreground.SetAsSource (gr, r);
			gr.Stroke ();
            gr.Restore();
		}


		protected override int measureRawSize (LayoutingType lt)
		{
			if ((lt == LayoutingType.Width && contentSize.Width == 0) || (lt == LayoutingType.Height && contentSize.Height == 0)) {
                if (size != default(Size))
                    contentSize = size;
                else
                {
                    using (Surface drawing = new ImageSurface(Format.A1, 1, 1))
                    {
                        using (Context ctx = new Context(drawing))
                        {
							using (PathParser parser = new PathParser (path))
								parser.Draw (ctx);								
                            Rectangle r = ctx.StrokeExtents();
                            contentSize = new Size(r.Right, r.Bottom);
                        }
                    }
                }
			}
			return lt == LayoutingType.Width ?
				contentSize.Width + 2 * Margin: contentSize.Height + 2 * Margin;
		}
	}
}

