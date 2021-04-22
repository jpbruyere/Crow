// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
namespace Crow
{
	public class TreeExpandEventArg : EventArgs
	{
		/// <summary>
		/// Source of the expand/collapse event
		/// </summary>
		public IToggle SourceWidget;
		public TreeExpandEventArg (IToggle sourceWidget) {

		}
	}
}
