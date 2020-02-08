// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;

namespace Crow
{
	/// <summary>
	/// templated control for selecting value in a pop up list
	/// </summary>
	public class ComboBox : ListBox
    {
		#region CTOR
		protected ComboBox() : base(){}
		public ComboBox (Interface iface) : base(iface){}
		#endregion

		Size minimumPopupSize = "10,10";
		[XmlIgnore]public Size MinimumPopupSize{
			get { return minimumPopupSize; }
			set {
				minimumPopupSize = value;
				NotifyValueChanged ("MinimumPopupSize", minimumPopupSize);
			}
		}

		public override void OnLayoutChanges (LayoutingType layoutType)
		{
			base.OnLayoutChanges (layoutType);

			if (layoutType == LayoutingType.Width)
				MinimumPopupSize = new Size (this.Slot.Width, minimumPopupSize.Height);			
		}
	}
}
