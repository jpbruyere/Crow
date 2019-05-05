//
//  Extensions.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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
using System;
using System.Collections.Generic;

namespace Crow
{
	public static partial class Extensions
	{
		public static string GetIcon(this Widget go){
			return "#CrowIDE.icons.toolbox." + go.GetType().FullName + ".svg";
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
