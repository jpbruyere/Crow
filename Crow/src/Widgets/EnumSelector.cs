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
		string rbStyle;
		IML.Instantiator radioButtonITor;
		#endregion

		#region public properties
		/// <summary>
		/// Enum values are presented with RadioButton controls. Here you may specify a template to use
		/// for the radio buttons.
		/// </summary>
		[DefaultValue (null)]
		public string RadioButtonStyle {
			get { return rbStyle; }
			set {
				if (rbStyle == value)
					return;
				rbStyle = value;
				radioButtonITor = null;
				//force refresh to use new template if values are already displayed
				if (enumValue != null) {
					Enum tmp = enumValue;
					enumValue = null;
					EnumValue = tmp;
				}
				NotifyValueChanged ("RadioButtonStyle", rbStyle);
			}
		}
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
				if (radioButtonITor == null)
					radioButtonITor = IFace.CreateITorFromIMLFragment ($"<RadioButton Style='{rbStyle}'/>");

				if (enumValue != null) {
					if (enumType != enumValue.GetType ()) {
						enumValueContainer.ClearChildren ();
						enumType = enumValue.GetType ();
						foreach (string en in enumType.GetEnumNames ()) {

							RadioButton rb = radioButtonITor.CreateInstance<RadioButton> ();
							rb.Caption = en;
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

