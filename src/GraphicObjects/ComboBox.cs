using System;
using System.Xml.Serialization;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.ComboBox.style")]
	[DefaultTemplate("#Crow.Templates.ComboBox.goml")]
	public class ComboBox : ListBox
    {		
		#region CTOR
		public ComboBox() : base(){	}	
		#endregion

		Size minimumPopupSize = "10;10";
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
