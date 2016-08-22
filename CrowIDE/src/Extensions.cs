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
	public static class Extensions
	{
		public static List<GraphicObject> GetChildren(this GraphicObject go){
			if (go is Group)
				return (go as Group).Children;
			if (go is Container)
				return new List<GraphicObject>( new GraphicObject[] { (go as Container).Child });
			return new List<GraphicObject>();
		}
	}
}
