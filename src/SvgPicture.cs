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

		#region implemented abstract members of Fill

		public override void SetAsSource (Context ctx, Rectangle bounds = default(Rectangle))
		{
			float widthRatio = 1f;
			float heightRatio = 1f;

			if (Scaled){
				widthRatio = (float)bounds.Width / Dimensions.Width;
				heightRatio = (float)bounds.Height / Dimensions.Height;
			}

			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}

			using (ImageSurface tmp = new ImageSurface (Format.Argb32, bounds.Width, bounds.Height)) {
				using (Cairo.Context gr = new Context (tmp)) {
					gr.Translate (bounds.Left, bounds.Top);
					gr.Scale (widthRatio, heightRatio);
					gr.Translate ((bounds.Width/widthRatio - Dimensions.Width)/2, (bounds.Height/heightRatio - Dimensions.Height)/2);

					hSVG.RenderCairo (gr);
				}
				ctx.SetSource (tmp);
			}	
		}
		#endregion
			
		public override void Paint (Cairo.Context gr, Rectangle rect, string subPart = "")
		{
			float widthRatio = 1f;
			float heightRatio = 1f;

			if (Scaled) {
				widthRatio = (float)rect.Width / Dimensions.Width;
				heightRatio = (float)rect.Height / Dimensions.Height;
			}
			if (KeepProportions) {
				if (widthRatio < heightRatio)
					heightRatio = widthRatio;
				else
					widthRatio = heightRatio;
			}
				
			gr.Save ();

			gr.Translate (rect.Left,rect.Top);
			gr.Scale (widthRatio, heightRatio);
			gr.Translate (((float)rect.Width/widthRatio - Dimensions.Width)/2f, ((float)rect.Height/heightRatio - Dimensions.Height)/2f);

			gr.Restore ();			
		}
	}
}

