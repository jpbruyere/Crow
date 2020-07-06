// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Cairo;
using System.Linq;

namespace Crow
{
	/*public class DbgEventTypeColors : Widget
	{
		protected override void onDraw (Context gr)
		{
			base.onDraw (gr);

			gr.SelectFontFace (Font.Name, Font.Slant, Font.Wheight);
			gr.SetFontSize (Font.Size);
			gr.FontOptions = Interface.FontRenderingOptions;
			gr.Antialias = Interface.Antialias;

			DbgEvtType[] types = DbgLogViewer.colors.Keys.ToArray ();
			Color[] colors = DbgLogViewer.colors.Values.ToArray ();

			Rectangle r = ClientRectangle;
			FontExtents fe = gr.FontExtents;

			double penY = fe.Height + r.Top;
			double penX = (double)r.Left;


			for (int i = 0; i < types.Length; i++) {
				string n = types [i].ToString();
				Color c = colors[i];
				Foreground.SetAsSource (gr);

				gr.MoveTo (penX + 25.0, penY - fe.Descent);
				gr.ShowText (n);

				Rectangle rc = new Rectangle((int)penX, (int)(penY - fe.Height), 20, (int)fe.Height);
				rc.Inflate (-2);
				gr.Rectangle (rc);
				gr.StrokePreserve ();

				gr.SetSource (c);
				gr.Fill ();

				penY += fe.Height;

			}
		}
	}*/
}

