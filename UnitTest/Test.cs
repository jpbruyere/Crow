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

namespace UnitTest
{
	[TestFixture ()]
	public class Test
	{
		NUnitCrowWindow win;

		[SetUp]
		public void Init()
		{
			win = new NUnitCrowWindow (600,600);
		}

		[Test ()]
		[Category("Alignment")]
		public void GraphicObject ()
		{
			string[] tests = new string[] { "0","1","3","4", "5" };

			foreach (string s in tests) {
				win.LoadTest (s);
				win.Update ();
				win.Update ();
				byte[] model = File.ReadAllBytes("ExpectedOutputs/" + s + ".png");
				byte[] result = File.ReadAllBytes(@"tmp.png");

				CollectionAssert.AreEqual (model, result);

				win.ClearInterface ();
			}				
		}

		void testAlignment(GraphicObject g){
			g.HorizontalAlignment = HorizontalAlignment.Left;

		}
	}
}

