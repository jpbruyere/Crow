// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;

namespace Crow
{
	public static partial class Extensions
	{
		public static string GetIcon(this Widget go){
			return "#Icons." + go.GetType().FullName + ".svg";
		}
		public static List<Widget> GetChildren(this Widget go){
			Type goType = go.GetType();
			if (typeof (Group).IsAssignableFrom (goType))
				return (go as Group).Children;
			if (typeof(Container).IsAssignableFrom (goType))
				return new List<Widget>( new Widget[] { (go as Container).Child });
			if (typeof(TemplatedContainer).IsAssignableFrom (goType))
				return new List<Widget>( new Widget[] { (go as TemplatedContainer).Content });
			if (typeof(TemplatedGroup).IsAssignableFrom (goType))
				return (go as TemplatedGroup).Items;

			return new List<Widget>();
		}
	}
}
