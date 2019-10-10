// Copyright (c) 2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// Convenient widget for selecting value among enum values. This is a templated control
	/// expecting a 'Group' widget named 'Content' inside the template to handle the enum values display.
	/// </summary>
	public class EnumSelector : TemplatedControl
	{
		#region CTOR
		protected EnumSelector () : base(){}
		public EnumSelector (Interface iface) : base(iface){}
		#endregion

		Group enumValueContainer;

		protected override void loadTemplate (Widget template = null)
		{
			base.loadTemplate (template);
			enumValueContainer = this.child.FindByName ("Content") as Group;
			if (enumValueContainer == null)
				throw new Exception("EnumSelector template MUST contain a 'Group' named 'Content'");
		}

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
						enumValueContainer.ClearChildren ();
						enumType = enumValue.GetType ();
						foreach (string en in enumType.GetEnumNames ()) {
							RadioButton rb = new RadioButton (IFace);
							rb.Caption = en;
							rb.Fit = true;
							rb.LogicalParent = this;
							if (enumValue.ToString () == en)
								rb.IsChecked = true;
							rb.Checked += (sender, e) => (((RadioButton)sender).LogicalParent as EnumSelector).EnumValue = (Enum)Enum.Parse (enumType, (sender as RadioButton).Caption);
							enumValueContainer.AddChild (rb);
						}
					
					}
				} else
					enumValueContainer.ClearChildren ();

				NotifyValueChanged ("EnumValue", enumValue);
				RegisterForRedraw ();
			}
		}
		#endregion

	}
}

