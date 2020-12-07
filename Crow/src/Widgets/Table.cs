// Copyright (c) 2019  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
namespace Crow
{
	/// <summary>
	/// Table column definition
	/// </summary>
	public class Column : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
			=> ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		#endregion

		string caption;
		Measure width = Measure.Inherit;

		public string Caption {
			get => caption;
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);
			}
		}
		/// <summary>
		/// column width, special value 'Inherit' will be used to share table width equaly among columns
		/// </summary>
		/// <value>The column's width.</value>
		public Measure Width {
			get => width;
			set {
				if (width == value)
					return;
				width = value;
				NotifyValueChanged ("Width", width);
			}
		}

		//public string Data {

		//}
	}


	public class Table : TemplatedGroup
	{
		#region CTOR
		public Table ()  {}
		public Table (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		public ObservableList<Column> Columns = new ObservableList<Column> ();
	}
}
