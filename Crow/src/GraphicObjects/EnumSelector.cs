// Copyright (c) 2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// Convenient widget for selecting value from enum
	/// </summary>
	public class EnumSelector : GenericStack
	{
		#region CTOR
		protected EnumSelector () : base(){}
		public EnumSelector (Interface iface) : base(iface){}
		#endregion

		#region private fields
		Enum enumValue;
		Type enumType;
		#endregion

		#region public properties
		/// <summary>
		/// use to define the colors of the 3d border
		/// </summary>
		[DefaultValue(null)]
		public virtual Enum EnumValue {
			get { return enumValue; }
			set {
				if (enumValue == value)
					return;

				enumValue = value;

				if (enumValue != null) {
					if (enumType != enumValue.GetType ()) {
						ClearChildren ();
						enumType = enumValue.GetType ();
						foreach (string en in enumType.GetEnumNames ()) {
							RadioButton rb = new RadioButton (IFace);
							rb.Caption = en;
							if (enumValue.ToString() == en)
								rb.IsChecked = true;
							rb.Checked += (sender, e) => (((RadioButton)sender).Parent as EnumSelector).EnumValue = (Enum)Enum.Parse (enumType, (sender as RadioButton).Caption);
							AddChild (rb);
							RegisterForLayouting (LayoutingType.All);
						}
					}
				} else 
					ClearChildren ();

				NotifyValueChanged ("EnumValue", enumValue);
				RegisterForRedraw ();
			}
		}
		#endregion

	}
}

