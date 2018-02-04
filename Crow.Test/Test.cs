//
//  Test.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using NUnit.Framework;
using System;
using Crow;
using System.IO;
[assembly: Description ("Assembly description here")]
namespace UnitTest
{
	[TestFixture, Description ("Fixture description here")]
	public class Test
	{
		Interface iface;
		Rectangle bounds = new Rectangle (0, 0, 600, 600);


		[SetUp]
		public void Init ()
		{
			iface = new Interface ();
			iface.ProcessResize (bounds);
		}

		[Test , Description("My really cool test")]
		public void GraphicObject ()
		{
			string [] tests = new string [] { "0", "1", "3", "4", "5" };

			foreach (string s in tests) {
				string fileName = Path.Combine ("Interfaces", s + ".crow");
				iface.LoadInterface (fileName);

				iface.Update ();
				iface.Update ();

				using (Cairo.Surface surf = new Cairo.ImageSurface (iface.bmp,
					  Cairo.Format.Argb32, iface.ClientRectangle.Width, iface.ClientRectangle.Height, iface.ClientRectangle.Width * 4)) {
					surf.WriteToPng (@"tmp.png");
					surf.WriteToPng (fileName + ".png");
				}


				byte [] model = File.ReadAllBytes ("ExpectedOutputs/" + s + ".png");
				byte [] result = File.ReadAllBytes (@"tmp.png");

				//CollectionAssert.AreEqual (model, result);

				iface.ClearInterface ();
			}
		}
	}
}

