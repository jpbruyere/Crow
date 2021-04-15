// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	public interface IObservableList {
		event EventHandler<ListChangedEventArg> ListAdd;
		event EventHandler<ListChangedEventArg> ListRemove;
		event EventHandler<ListChangedEventArg> ListEdit;
		event EventHandler<ListClearEventArg> ListClear;

		void Insert ();
		void Remove ();
		void RaiseEdit ();
	}
}

