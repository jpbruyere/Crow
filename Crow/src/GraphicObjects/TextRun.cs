//
// TextRun.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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
using System.Linq;
using System.Text;
using System.Diagnostics;
using Crow.Cairo;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	[DesignIgnore]
	public class TextRun : Widget
	{
		#region CTOR
		protected TextRun () : base(){}
		public TextRun (Interface iface) : base (iface){}
		#endregion

		//TODO:change protected to private

		#region private and protected fields
		protected string _text = "label";
		Alignment _textAlignment = Alignment.Left;
		bool horizontalStretch = false;
		bool verticalStretch = false;
		bool _multiline;
		bool wordWrap;
		protected Rectangle rText;
		protected float widthRatio = 1f;
		protected float heightRatio = 1f;
		protected FontExtents fe;
		protected TextExtents te;
		#endregion


		
		[DefaultValue (Alignment.Left)]
		public Alignment TextAlignment {
			get { return _textAlignment; }
			set { _textAlignment = value; }
		}
		
		[DefaultValue (false)]
		public virtual bool HorizontalStretch {
			get { return horizontalStretch; }
			set {
				if (horizontalStretch == value)
					return;
				horizontalStretch = value;
				RegisterForRedraw ();
				NotifyValueChanged ("HorizontalStretch", horizontalStretch);
			}
		}
		
		[DefaultValue (false)]
		public virtual bool VerticalStretch {
			get { return verticalStretch; }
			set {
				if (verticalStretch == value)
					return;
				verticalStretch = value;
				RegisterForRedraw ();
				NotifyValueChanged ("VerticalStretch", verticalStretch);
			}
		}
		
		[DefaultValue ("label")]
		public string Text {
			get {
				return lines == null ?
					_text : lines.Aggregate ((i, j) => i + Interface.LineBreak + j);
			}
			set {
				if (_text == value)
					return;

				RegisterForGraphicUpdate ();

				_text = value;

				if (string.IsNullOrEmpty (_text))
					_text = "";

				lines = getLines;
			}
		}
		
		[DefaultValue (false)]
		public bool Multiline {
			get { return _multiline; }
			set {
				_multiline = value;
				RegisterForGraphicUpdate ();
			}
		}
		
		[DefaultValue (false)]
		public bool WordWrap {
			get {
				return wordWrap;
			}
			set {
				if (wordWrap == value)
					return;
				wordWrap = value;
				RegisterForGraphicUpdate ();
			}
		}

		List<string> lines;
		List<string> getLines {
			get {
				return _multiline ?
					Regex.Split (_text, "\r\n|\r|\n").ToList () :
					new List<string> (new string [] { _text });
			}
		}

		#region GraphicObject overrides
		protected override int measureRawSize (LayoutingType lt)
		{
			if (lines == null)
				lines = getLines;

			using (ImageSurface img = new ImageSurface (Format.Argb32, 10, 10)) {
				using (Context gr = new Context (img)) {
					//Cairo.FontFace cf = gr.GetContextFontFace ();

					gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
					gr.SetFontSize (Font.Size);


					fe = gr.FontExtents;
					te = new TextExtents ();

					if (lt == LayoutingType.Height) {
						int lc = lines.Count;
						//ensure minimal height = text line height
						if (lc == 0)
							lc = 1;

						return (int)(fe.Height * lc) + Margin * 2;
					}

					foreach (string s in lines) {
						string l = s.Replace("\t", new String (' ', Interface.TabSize));
						TextExtents tmp = gr.TextExtents (l);
						if (tmp.XAdvance > te.XAdvance)
							te = tmp;
					}
					return (int)Math.Ceiling (te.XAdvance) + Margin * 2;
				}
			}
		}
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			rText = new Rectangle (new Size (
				measureRawSize (LayoutingType.Width), measureRawSize (LayoutingType.Height)));
			rText.Width -= 2 * Margin;
			rText.Height -= 2 * Margin;

			widthRatio = 1f;
			heightRatio = 1f;

			Rectangle cb = ClientRectangle;

			//ignore text alignment if size to content = true
			//or if text size is larger than client bounds
			if (Width < 0 || Height < 0 || rText.Width > cb.Width) {
				rText.X = cb.X;
				rText.Y = cb.Y;
			} else {
				if (horizontalStretch) {
					widthRatio = (float)cb.Width / rText.Width;
					if (!verticalStretch)
						heightRatio = widthRatio;
				}
				if (verticalStretch) {
					heightRatio = (float)cb.Height / rText.Height;
					if (!horizontalStretch)
						widthRatio = heightRatio;
				}

				rText.Width = (int)(widthRatio * cb.Width);
				rText.Height = (int)(heightRatio * cb.Height);

				switch (TextAlignment) {
				case Alignment.TopLeft:     //ok
					rText.X = cb.X;
					rText.Y = cb.Y;
					break;
				case Alignment.Top:   //ok						
					rText.Y = cb.Y;
					rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
					break;
				case Alignment.TopRight:    //ok
					rText.Y = cb.Y;
					rText.X = cb.Right - rText.Width;
					break;
				case Alignment.Left://ok
					rText.X = cb.X;
					rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
					break;
				case Alignment.Right://ok
					rText.X = cb.X + cb.Width - rText.Width;
					rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
					break;
				case Alignment.Bottom://ok
					rText.X = cb.Width / 2 - rText.Width / 2;
					rText.Y = cb.Height - rText.Height;
					break;
				case Alignment.BottomLeft://ok
					rText.X = cb.X;
					rText.Y = cb.Bottom - rText.Height;
					break;
				case Alignment.BottomRight://ok
					rText.Y = cb.Bottom - rText.Height;
					rText.X = cb.Right - rText.Width;
					break;
				case Alignment.Center://ok
					rText.X = cb.X + cb.Width / 2 - rText.Width / 2;
					rText.Y = cb.Y + cb.Height / 2 - rText.Height / 2;
					break;
				}
			}

			gr.FontMatrix = new Matrix (widthRatio * Font.Size, 0, 0, heightRatio * Font.Size, 0, 0);


			int curLineCount = 0;
			for (int i = 0; i < lines.Count; i++) {
				string l = lines [i].Replace ("\t", new String (' ', Interface.TabSize));
				List<string> wl = new List<string> ();
				int lineLength = (int)gr.TextExtents (l).XAdvance;

				if (wordWrap && lineLength > cb.Width) {
					string tmpLine = "";
					int curChar = 0;
					while (curChar < l.Length) {
						tmpLine += l [curChar];
						if ((int)gr.TextExtents (tmpLine).XAdvance > cb.Width) {
							tmpLine = tmpLine.Remove (tmpLine.Length - 1);
							wl.Add (tmpLine);
							tmpLine = "";
							continue;
						}
						curChar++;
					}
					wl.Add (tmpLine);
				} else
					wl.Add (l);

				foreach (string ll in wl) {
					lineLength = (int)gr.TextExtents (ll).XAdvance;


					if (string.IsNullOrWhiteSpace (ll)) {
						curLineCount++;
						continue;
					}

					Foreground.SetAsSource (gr);
					gr.MoveTo (rText.X, rText.Y + fe.Ascent + fe.Height * curLineCount);

					gr.ShowText (ll);
					gr.Fill ();

					curLineCount++;
				}
			}
		}
		#endregion
	}
}
