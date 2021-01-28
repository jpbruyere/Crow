// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
	/// <summary>
	/// Event argument for layouting events.
	/// </summary>
	public class LayoutingEventArgs: EventArgs
	{
		/// <summary>
		/// The layout type that has changed.
		/// </summary>
		public LayoutingType  LayoutType;
		/// <summary>
		/// Create a new instance of LayoutingEventArgs.
		/// </summary>
		/// <param name="_layoutType">The layout type that trigger the event.</param>
		public LayoutingEventArgs (LayoutingType  _layoutType) : base()
		{
			LayoutType = _layoutType;
		}
	}
}

