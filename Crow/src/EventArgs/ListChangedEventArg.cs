// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
namespace Crow
{
	public class ListChangedEventArg : EventArgs
	{
		public int Index;
		public object Element;
		public ListChangedEventArg (int index, object element)
		{
			Index = index;
			Element = element;
		}
	}
}
