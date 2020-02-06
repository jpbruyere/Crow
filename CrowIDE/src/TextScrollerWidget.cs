// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.Collections;
using Crow.Cairo;

namespace Crow
{
	public class TextScrollerWidget : ScrollingObject
	{
		ObservableList<string> lines;
		bool scrollOnOutput;
		int visibleLines = 1;
		FontExtents fe;

		[DefaultValue(true)]
		public virtual bool ScrollOnOutput {
			get { return scrollOnOutput; }
			set {
				if (scrollOnOutput == value)
					return;
				scrollOnOutput = value;
				NotifyValueChanged ("ScrollOnOutput", scrollOnOutput);

			}
		}
		public virtual ObservableList<string> Lines {
			get { return lines; }
			set {
				if (lines == value)
					return;
				if (lines != null) {
					lines.ListAdd -= Lines_ListAdd;
					lines.ListRemove -= Lines_ListRemove;
				}
				lines = value;
				if (lines != null) {
					lines.ListAdd += Lines_ListAdd;
					lines.ListRemove += Lines_ListRemove;
				}
				NotifyValueChanged ("Lines", lines);
				RegisterForGraphicUpdate ();
			}
		}

		void Lines_ListAdd (object sender, ListChangedEventArg e)
		{
			MaxScrollY = lines.Count - visibleLines;
			if (scrollOnOutput)
				ScrollY = MaxScrollY;
		}

		void Lines_ListRemove (object sender, ListChangedEventArg e)
		{
			MaxScrollY = lines.Count - visibleLines;
		}


		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Height) {
				using (ImageSurface img = new ImageSurface (Format.Argb32, 10, 10)) {
					using (Context gr = new Context (img)) {
						//Cairo.FontFace cf = gr.GetContextFontFace ();

						gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
						gr.SetFontSize (Font.Size);

						fe = gr.FontExtents;
					}
				}
				visibleLines = (int)Math.Floor ((double)ClientRectangle.Height / fe.Height);
				MaxScrollY = lines.Count - visibleLines;
			}
		}
		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			if (lines == null)
				return;

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);

			Rectangle r = ClientRectangle;

			Foreground.SetAsSource (gr);

			double y = ClientRectangle.Y;
			double x = ClientRectangle.X;

			for (int i = 0; i < visibleLines; i++) {
				if (i + ScrollY >= Lines.Count)
					break;
				//if ((lines [i + Scroll] as string).StartsWith ("error", StringComparison.OrdinalIgnoreCase)) {
				//	errorFill.SetAsSource (gr);
				//	gr.Rectangle (x, y, (double)r.Width, fe.Height);
				//	gr.Fill ();
				//	Foreground.SetAsSource (gr);
				//}
				gr.MoveTo (x, y + fe.Ascent);
				gr.ShowText (lines[i+ScrollY] as string);
				y += fe.Height;
				gr.Fill ();
			}
		}

	}
}

