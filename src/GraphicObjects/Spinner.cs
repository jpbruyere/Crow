//
//  Spinner.cs
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
using OpenTK.Input;

namespace go
{
	[DefaultTemplate("#go.Templates.Spinner.goml")]
	public class Spinner : NumericControl
	{
		Label labCpt;

		public Spinner (double minimum, double maximum, double step) : 
		base (minimum, maximum, step)
		{

		}
		public Spinner () : base()
		{
		}

		#region implemented abstract members of TemplatedControl

		protected override void loadTemplate (GraphicObject template = null)
		{
			base.loadTemplate (template);			
		}

		#endregion

		void onUp (object sender, MouseButtonEventArgs e)
		{
//			decimal tmp = 0;
//			if (!decimal.TryParse (labCpt.Text, out tmp))
//				return;

			Value += this.SmallIncrement;
		}
		void onDown (object sender, MouseButtonEventArgs e)
		{
//			decimal tmp = 0;
//			if (!decimal.TryParse (labCpt.Text, out tmp))
//				return;

			Value -= this.SmallIncrement;
		}

	}
}

