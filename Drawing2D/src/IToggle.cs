// Copyright (c) 2020-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
    public interface IToggle
    {
		event EventHandler ToggleOn;
		event EventHandler ToggleOff;
		BooleanTestOnInstance IsToggleable { get; set; }

		bool IsToggled {
			get;
			set;
		}

	}
}
