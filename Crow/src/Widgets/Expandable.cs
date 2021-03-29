// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

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
		[DefaultValue("#Crow.Icons.expandable.svg")]
		public string Image {
			get { return image; }
			set {
				if (image == value)
					return;
				image = value;
				NotifyValueChangedAuto (image);
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

				NotifyValueChangedAuto (_isExpanded);

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
				_contentContainer.IsVisible = true;

			Expand.Raise (this, e);
		}
		public virtual void onCollapse(object sender, EventArgs e)
		{
			if (_contentContainer != null)
				_contentContainer.IsVisible = false;

			Collapse.Raise (this, e);
		}
	}
}
