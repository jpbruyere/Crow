//
// Expandable.cs
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
	/// <summary>
	/// templated control whose content can be hidden and shown on demand
	/// </summary>
    public class Expandable : TemplatedContainer
    {
		#region CTOR
		protected Expandable() : base(){}
		public Expandable (Interface iface) : base(iface){}
		#endregion

		#region Private fields
		bool _isExpanded;
		string image;
		#endregion

		#region Event Handlers
		/// <summary>
		/// Occurs when control is expanded.
		/// </summary>
		public event EventHandler Expand;
		/// <summary>
		/// Occurs when control is collapsed.
		/// </summary>
		public event EventHandler Collapse;
		#endregion

		public BooleanTestOnInstance GetIsExpandable;

		/// <summary>
		/// mouse click event handler for easy expand triggering in IML
		/// </summary>
		public void onClickForExpand (object sender, MouseButtonEventArgs e)
		{
			IsExpanded = !IsExpanded;
		}

		#region Public properties
		[DefaultValue("#Crow.Images.Icons.expandable.svg")]
		public string Image {
			get { return image; }
			set {
				if (image == value)
					return;
				image = value;
				NotifyValueChanged ("Image", image);
			}
		}
		[DefaultValue(false)]
        public bool IsExpanded
        {
			get { return _isExpanded; }
            set
            {
				if (value == _isExpanded)
					return;

				_isExpanded = value;

				bool isExp = IsExpandable;
				NotifyValueChanged ("IsExpandable", isExp);
				if (!isExp)
					_isExpanded = false;

				NotifyValueChanged ("IsExpanded", _isExpanded);

				if (_isExpanded)
					onExpand (this, null);
				else
					onCollapse (this, null);
            }
        }
		[XmlIgnore]public bool IsExpandable {
			get {
				try {
					return GetIsExpandable == null ? true : GetIsExpandable (this);
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine ("Not Expandable error: " + ex.ToString ());
					return false;
				}
			}
		}
		#endregion

		public virtual void onExpand(object sender, EventArgs e)
		{
			if (_contentContainer != null)
				_contentContainer.Visible = true;

			Expand.Raise (this, e);
		}
		public virtual void onCollapse(object sender, EventArgs e)
		{
			if (_contentContainer != null)
				_contentContainer.Visible = false;

			Collapse.Raise (this, e);
		}
	}
}
