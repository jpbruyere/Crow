// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.Linq;

namespace Crow.Coding
{
	public class EditPane : TemplatedGroup
	{
		public EditPane () {}

		object selectedItemElement = null;

		public override object SelectedItem {
			get => base.SelectedItem;
			set => base.SelectedItem = value;
		}
		public override int SelectedIndex {
			get {
				return base.SelectedIndex;
			}
			set {
				base.SelectedIndex = value;
			}
		}
		public object SelectedItemElement {
			get { return selectedItemElement; }
			set {
				if (selectedItemElement == value)
					return;
				selectedItemElement = value;
				NotifyValueChanged ("SelectedItemElement", selectedItemElement);
			}
		}
	}
}

