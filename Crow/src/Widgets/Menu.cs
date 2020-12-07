// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.ComponentModel;

namespace Crow {
	public class Menu : TemplatedGroup
	{
		#region CTOR
		protected Menu () : base(){}
		public Menu (Interface iface) : base(iface) {}
		#endregion

		Orientation orientation;
		bool autoOpen = false;

		#region Public properties
		[DefaultValue(Orientation.Horizontal)]
		public Orientation Orientation {
			get { return orientation; }
			set {
				if (orientation == value)
					return;
				orientation = value;
				NotifyValueChangedAuto (orientation);
			}
		}
		[XmlIgnore]public bool AutomaticOpening
		{
			get { return autoOpen; }
			set	{
				if (autoOpen == value)
					return;
				autoOpen = value;
				NotifyValueChangedAuto (autoOpen);
			}
		}
		#endregion

		public override void AddItem (Widget g)
		{			
			base.AddItem (g);

			if (orientation == Orientation.Horizontal)
				g.NotifyValueChanged ("PopDirection", Alignment.Bottom);
			else
				g.NotifyValueChanged ("PopDirection", Alignment.Right);
		}
		public override void onMouseLeave (object sender, MouseMoveEventArgs e)
		{
			base.onMouseLeave (sender, e);
			AutomaticOpening = false;
		}
	}
}

