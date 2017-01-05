﻿//
//  DataSourceChangeEventArg.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;

namespace Crow
{
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

