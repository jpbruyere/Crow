// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	public class SelectionChangeEventArgs: EventArgs
	{		
		public object NewValue;

		public SelectionChangeEventArgs (object _newValue) : base()
		{
			NewValue = _newValue;
		}
	}
}

