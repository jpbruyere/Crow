//
//  SaturationValueSelector.cs
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
using Cairo;

namespace Crow
{
	public class SaturationValueSelector : ColorSelector
	{
		public SaturationValueSelector () : base()
		{
		}

		protected override void onDraw (Cairo.Context gr)
		{
			base.onDraw (gr);

			Rectangle r = ClientRectangle;
			Rectangle rGrad = r;
			rGrad.Inflate (-1);

			Foreground.SetAsSource (gr, r);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();

			Crow.Gradient grad = new Gradient (Gradient.Type.Horizontal);
			grad.Stops.Add (new Gradient.ColorStop (0, new Color (1, 1, 1, 1)));
			grad.Stops.Add (new Gradient.ColorStop (1, new Color (1, 1, 1, 0)));
			grad.SetAsSource (gr, rGrad);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();
			grad = new Gradient (Gradient.Type.Vertical);
			grad.Stops.Add (new Gradient.ColorStop (0, new Color (0, 0, 0, 0)));
			grad.Stops.Add (new Gradient.ColorStop (1, new Color (0, 0, 0, 1)));
			grad.SetAsSource (gr, rGrad);
			CairoHelpers.CairoRectangle (gr, r, CornerRadius);
			gr.Fill();

			updateColorFromPicking (false);
		}
		public override void Paint (ref Context ctx)
		{
			base.Paint (ref ctx);

			Rectangle rb = Slot + Parent.ClientRectangle.Position;
			ctx.Save ();

			ctx.Translate (rb.X, rb.Y);

			ctx.SetSourceColor (Color.White);
			ctx.Arc (mousePos.X, mousePos.Y, 3.0, 0, Math.PI * 2.0);
			ctx.LineWidth = 1.0;
			ctx.Stroke ();

			ctx.Restore ();
		}
	}
}

