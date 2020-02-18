/* Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
 *
 * This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
 */

using System;
using Crow;

namespace test {
	public class TestClass {
		int a = 0;
	}
}

namespace HelloWorld
{
	public class ILView : Widget
	{
		readonly int a;
		#region CTOR
		public ILView ()
		{
			a = 10;
		}
		#endregion
		void test ()
		{
#if DEBUG
Console.WriteLine ("debug" + a.ToString());

#endif
		}

	}
}
