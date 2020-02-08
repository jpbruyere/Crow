// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System.Collections.Generic;

namespace Crow.Coding
{
	public class ImlProjectItem : ProjectFileNode
	{
		#region CTOR
		public ImlProjectItem (ProjectItemNode pi) : base (pi){			
		}
		#endregion

		Widget instance;
		Measure designWidth, designHeight;

		/// <summary>
		/// instance created with an instantiator from the source by a DesignInterface,
		/// for now, the one in ImlVisualEditor
		/// </summary>
		public Widget Instance {
			get { return instance; }
			set {
				if (instance == value)
					return;
				instance = value;
				NotifyValueChanged ("Instance", instance);
			}
		}
			
		public Measure DesignWidth {
			get { return designWidth; }
			set { 
				if (designWidth == value)
					return;
				designWidth = value;
				NotifyValueChanged ("DesignWidth", designWidth);
			}
		}
		public Measure DesignHeight {
			get { return designHeight; }
			set {
				if (designHeight == value)
					return;
				designHeight = value;
				NotifyValueChanged ("DesignHeight", designHeight);
			}
		}


		public List<Widget> GraphicTree { 
			get { return new List<Widget> (new Widget[] {instance}); }
		}

		void GTView_SelectedItemChanged (object sender, SelectionChangeEventArgs e){
			SelectedItem = e.NewValue;
		}
	}
}

