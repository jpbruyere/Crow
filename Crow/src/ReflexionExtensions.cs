// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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

