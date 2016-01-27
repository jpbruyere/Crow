//
//  ReflexionExtensions.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.Reflection;

namespace Crow
{
	public static class ReflexionExtensions
	{
		public static string GetIcon(this MemberInfo mi)
		{
			switch (mi.MemberType) {
			case MemberTypes.Constructor:
				return "ctor";
			case MemberTypes.Event:
				return "event";
			case MemberTypes.Field:
				return "field";
			case MemberTypes.Method:
				return "method";
			case MemberTypes.Property:
				return "property";
			case MemberTypes.TypeInfo:
				break;
			case MemberTypes.Custom:
				break;
			case MemberTypes.NestedType:
				break;
			case MemberTypes.All:
				break;
			default:
				return "pubfld";
			}
			return "notset";
		}
	}
}

