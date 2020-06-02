// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
namespace Crow
{
	/// <summary>
	/// Add this attribute to an assembly to have it search for Crow ressources (.style, images, templates,...)
	/// </summary>
	/// <remarks>
	/// By default, only the entry assembly and the crow assembly will be searched for resources.
	/// </remarks>
	[AttributeUsage (AttributeTargets.Assembly)]
	public class CrowAttribute : Attribute
	{
	}
}
