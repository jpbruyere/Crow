// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	/// <summary> Test func on data, return yes if there's children </summary>
	public delegate bool BooleanTestOnInstance(object instance);
	public class XmlIgnoreAttribute : Attribute
	{ 
	}

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

