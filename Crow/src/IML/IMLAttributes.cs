// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	public class DesignIgnore : Attribute
	{		
	}

	public class DesignCategory : Attribute
	{
		public string Name { get; set; }

		public DesignCategory (string name)
		{
			Name = name;
		}
	}
}

