// Copyright (c) 2013-2025  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;

namespace Crow
{
	public class Style : Dictionary<string, string>
	{
		#if DESIGN_MODE
		public Dictionary<string, FileLocation> Locations = new Dictionary<string, FileLocation>();
		#endif
		//public Dictionary<string, Style> SubStyles;//TODO:implement substyles for all tags inside a style
		public Style () : base()
		{
		}

	}
}

