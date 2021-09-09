using System.Linq;
using System.Xml.Linq;
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
	/// <remarks>
	/// Instanced RadioButton's names are set to enum values, and Tags are populated with a string build with
	/// 'IconsPrefix+EnumValueName+IconsExtension to ease associating icons with values.
	/// There's many other way to use this control, for examples see 'testEnumSelector.crow' in  the samples directory.
	/// </remarks>
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
		UInt32 bitFieldExcludeMask;
		Type enumType;
		bool enumTypeIsBitsfield, forceRadioButton;
		string rbStyle, iconsPrefix, iconsExtension;		
		#endregion

		#region public properties
		[DefaultValue ("#Icons.")]
		public string IconsPrefix {
			get => iconsPrefix;
			set {
				if (iconsPrefix == value)
					return;
				iconsPrefix = value;
				forceRefresh ();
				NotifyValueChangedAuto (iconsPrefix);
			}
		}
		[DefaultValue (".svg")]
		public string IconsExtension {
			get => iconsExtension;
			set {
				if (iconsExtension == value)
					return;
				iconsExtension = value;
				forceRefresh ();
				NotifyValueChangedAuto (iconsExtension);
			}
		}
		/// <summary>
		/// Enum values are presented with RadioButton controls. Here you may specify a template to use
		/// for the radio buttons.
		/// </summary>
		[DefaultValue (null)]
		public string RadioButtonStyle {
			get => rbStyle;
			set {
				if (rbStyle == value)
					return;
				rbStyle = value;				
				forceRefresh ();
				NotifyValueChangedAuto (rbStyle);
			}
		}
		/// <summary>
		/// if enum has the 'Flag' attribte, CheckBox will be used. RadioButton may still be forced by
		/// setting 'ForceRadioButton'='true'
		/// </summary>
		/// <value></value>
		[DefaultValue (false)]
		public bool ForceRadioButton {
			get => forceRadioButton;
			set {
				if (forceRadioButton == value)
					return;
				forceRadioButton = value;
				NotifyValueChangedAuto (forceRadioButton);
			}
		}
		/// <summary>
		/// use to define the colors of the 3d border
		/// </summary>
		[DefaultValue(null)]
		public virtual Enum EnumValue {
			get => enumValue;
			set {
				if (enumValue == value)
					return;

				enumValue = value;									

				if (enumValue != null) {

					if (enumType != enumValue.GetType ()) {
						enumValueContainer.ClearChildren ();
						enumType = enumValue.GetType ();						
												
						enumTypeIsBitsfield = enumType.CustomAttributes.Any (ca => ca.AttributeType == typeof(FlagsAttribute));

						if (enumTypeIsBitsfield) {
							IML.Instantiator iTor = IFace.CreateITorFromIMLFragment ($"<CheckBox Style='{rbStyle}'/>");
							UInt32 currentValue = Convert.ToUInt32 (EnumValue);							
							currentValue &= ~bitFieldExcludeMask;
							enumValue = (Enum)Enum.ToObject(enumType, currentValue);

							foreach (Enum en in enumType.GetEnumValues()) {
								UInt32 eni = Convert.ToUInt32 (en);
								if ((eni & bitFieldExcludeMask) != 0)
									continue;

								CheckBox rb = iTor.CreateInstance<CheckBox> ();
								rb.Caption = en.ToString();
								rb.LogicalParent = this;
								rb.Tag = $"{iconsPrefix}{en}{IconsExtension}";								
								rb.Tooltip = $"0x{eni:x8}";

								if (eni == 0) {
									rb.IsChecked = currentValue == 0;
									rb.Checked += (sender, e) => EnumValue = (Enum)Enum.ToObject(enumType, 0);
								} else {								
									rb.IsChecked = currentValue == 0 ? false : EnumValue.HasFlag (en);
									rb.Checked += onChecked;
									rb.Unchecked += onUnchecked;
								}
								/*rb.Checked += (sender, e) => (((CheckBox)sender).LogicalParent as EnumSelector).EnumValue = (Enum)(object)
											(Convert.ToUInt32 ((((CheckBox)sender).LogicalParent as EnumSelector).EnumValue) | Convert.ToUInt32 (en));						
								rb.Unchecked += (sender, e) => (((CheckBox)sender).LogicalParent as EnumSelector).EnumValue = (Enum)(object)
											(Convert.ToUInt32 ((((CheckBox)sender).LogicalParent as EnumSelector).EnumValue) & ~Convert.ToUInt32 (en));						*/

								enumValueContainer.AddChild (rb);
							}

						} else {
							IML.Instantiator iTor = IFace.CreateITorFromIMLFragment ($"<RadioButton Style='{rbStyle}'/>");
							foreach (var en in enumType.GetEnumValues ()) {
								RadioButton rb = iTor.CreateInstance<RadioButton> ();
								rb.Caption = en.ToString();
								rb.LogicalParent = this;
								rb.Tag = $"{iconsPrefix}{en}{IconsExtension}";
								if (enumValue == en)
									rb.IsChecked = true;								
								rb.Checked += (sender, e) => (((RadioButton)sender).LogicalParent as EnumSelector).EnumValue = (Enum)en;
								enumValueContainer.AddChild (rb);
							}
						}
							


					} else if (enumTypeIsBitsfield) {
						UInt32 currentValue = Convert.ToUInt32 (EnumValue);						
						currentValue &= ~bitFieldExcludeMask;
						enumValue = (Enum)Enum.ToObject(enumType, currentValue);

						if (currentValue == 0) {
							foreach (CheckBox rb in enumValueContainer.Children) {
								Enum en = (Enum)Enum.Parse(enumType, rb.Caption);
								UInt32 eni = Convert.ToUInt32 (en);
								if (eni == 0)
									rb.IsChecked = true;
								else
									rb.IsChecked = false;
							}
						} else {
							foreach (CheckBox rb in enumValueContainer.Children) {
								Enum en = (Enum)Enum.Parse(enumType, rb.Caption);
								UInt32 eni = Convert.ToUInt32 (en);
								if (eni == 0)
									rb.IsChecked = false;
								else
									rb.IsChecked = EnumValue.HasFlag (en);
							}
						}
					} else {
						foreach (RadioButton rb in enumValueContainer.Children) {
							if (rb.Caption == enumValue.ToString ())
								rb.IsChecked = true;
							else
								rb.IsChecked = false;
						}
					}
				} else
					enumValueContainer.ClearChildren ();

				NotifyValueChangedAuto (enumValue);
				RegisterForRedraw ();
			}
		}
		#endregion
		/// <summary>
		/// Include mask for bitfields. Used to ignore enum values in display.
		/// </summary>
		/// <value>UInt32 bitfield mask</value>
		public UInt32 BitFieldExcludeMask {
			get => bitFieldExcludeMask;
			set {
				if (bitFieldExcludeMask == value)
					return;
				bitFieldExcludeMask = value;
				NotifyValueChangedAuto(bitFieldExcludeMask);
				forceRefresh();
			}
		}
		void onChecked (object sender, EventArgs e) {			
			Enum en =(Enum)Enum.Parse (enumType, (sender as CheckBox).Caption);
			UInt32 newVal = Convert.ToUInt32 (en);
			if (newVal == 0)
				EnumValue = (Enum)Enum.ToObject(enumType, 0);			 
			else
				EnumValue = (Enum)Enum.ToObject(enumType, newVal | Convert.ToUInt32 (EnumValue));			 
		}
		void onUnchecked (object sender, EventArgs e) {			
			Enum en =(Enum)Enum.Parse (enumType, (sender as CheckBox).Caption);
			EnumValue = (Enum)Enum.ToObject(enumType, Convert.ToUInt32 (EnumValue) & ~Convert.ToUInt32 (en));			 
		}

		//force refresh to use new template if values are already displayed
		void forceRefresh ()
		{
			if (enumValue != null) {
				Enum tmp = enumValue;
				enumValue = null;
				EnumValue = tmp;
			}
		}
	}
}

