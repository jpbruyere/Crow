// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;

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
	public class ListClearEventArg : EventArgs
	{
		public IEnumerable<object> Elements;
		public ListClearEventArg (IEnumerable<object> elements)	{			
			Elements = elements;
		}
	}
}
