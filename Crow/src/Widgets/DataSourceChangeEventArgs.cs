// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	/// <summary>
	/// Raised when the current data source of a widget has changed.
	/// </summary>
	public class DataSourceChangeEventArgs : EventArgs
	{
		public object OldDataSource;
		public object NewDataSource;

		public DataSourceChangeEventArgs (object oldDataSource, object newDataSource) : base()
		{
			OldDataSource = oldDataSource;
			NewDataSource = newDataSource;
		}
		public override string ToString ()
		{
			return string.Format ("DSChangeEA: {0} => {1}", OldDataSource, NewDataSource);
		}
	}
}

