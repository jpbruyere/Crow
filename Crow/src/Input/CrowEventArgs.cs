// Copyright (c) 2014-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
	/// <summary>
	/// Base class for Crow interface events.
	/// </summary>
	/// <remarks>
	/// By default, device (mouse, keyboard) events are bubbled through the logical tree unless the `Handled` field
	/// of the `CrowEventArgs` is set to `true`, or if an event handler is registered for the Event.
	/// For example if you have a templated button that received a mouse
	/// event in a `Label` widget inside its tempate, the event may be bubbled to the `Button` widget where a
	/// `MouseClick` event may be registered which will cause the bubbling to stop at that level.
	/// </remarks>
	public class CrowEventArgs : EventArgs
	{
		/// <summary>
		/// If `true`, bubbling of the event through the logical widget tree is stopped
		/// </summary>
		public bool Handled;
	}
}
