using System;

namespace Crow
{
	[DefaultStyle("#Crow.Styles.ComboBox.style")]
	[DefaultTemplate("#Crow.Templates.ComboBox.goml")]
	public class ComboBox : ListBox
    {		
		#region CTOR
		public ComboBox() : base(){	}	
		#endregion
	}
}
