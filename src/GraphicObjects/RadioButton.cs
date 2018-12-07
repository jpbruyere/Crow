//
// RadioButton.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.ComponentModel;
using System.Xml.Serialization;

namespace Crow
{
    public class RadioButton : TemplatedControl
    {		        
		bool isChecked;

		#region CTOR
		protected RadioButton () : base(){}
		public RadioButton(Interface iface) : base(iface){}	
		#endregion

		public event EventHandler Checked;
		public event EventHandler Unchecked;

		#region GraphicObject overrides
		public override void onMouseDown (object sender, MouseButtonEventArgs e)
		{						
			Group pg = Parent as Group;
			if (pg != null) {
				for (int i = 0; i < pg.Children.Count; i++) {
					RadioButton c = pg.Children [i] as RadioButton;
					if (c == null)
						continue;
					c.IsChecked = (c == this);
				}
			} else
				IsChecked = !IsChecked;

			base.onMouseDown (sender, e);
		}
		#endregion

        [DefaultValue(false)]
        public bool IsChecked
        {
			get { return isChecked; }
            set
            {
				if (isChecked == value)
					return;
				
				isChecked = value;

				NotifyValueChanged ("IsChecked", value);

				if (isChecked)
					Checked.Raise (this, null);
				else
					Unchecked.Raise (this, null);
            }
        }
	}
}
