//
//  SvgPicture.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.IO;
using Cairo;

namespace Crow
{
	public class SvgPicture : Picture
	{
		Rsvg.Handle hSVG;
		string _subSvg;

		public SvgPicture ()
		{}
		public SvgPicture (string path) : base(path)
		{}
		protected override void loadFromStream (Stream stream)
		{
			using (MemoryStream ms = new MemoryStream ()) {
				stream.CopyTo (ms);

				hSVG = new Rsvg.Handle (ms.ToArray ());
				Dimensions = new Size (hSVG.Dimensions.Width, hSVG.Dimensions.Height);
			}
		}
			
		public override void Paint (Cairo.Context gr, Rectangle rect, string subPart = "")
		{
			float widthRatio = (float)rect.Width / Dimensions.Width;
			float heightRatio = (float)rect.Height / Dimensions.Height;
			float ratio = Math.Min (widthRatio, heightRatio);

			Rectangle rImg = rect;

			gr.Save ();

			if (KeepProportions)
				gr.Scale (ratio, ratio);
			else
				gr.Scale (widthRatio, heightRatio);

			gr.Translate (rImg.X/widthRatio, rImg.Y/heightRatio);
			if (string.IsNullOrEmpty (subPart))
				hSVG.RenderCairo (gr);
			else
				hSVG.RenderCairoSub (gr, "#" + subPart);			
			gr.Restore ();			
		}
	}
}

