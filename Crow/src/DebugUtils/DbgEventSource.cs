// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Crow
{
	//base class for both events and widgetRecord having an event list
	public abstract class DbgEventSource : IValueChange
	{
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
			=> ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		public void NotifyValueChangedAuto (object _value, [CallerMemberName] string caller = null)
			=> NotifyValueChanged (caller, _value);

		//flattened event list of this widget
		public List<DbgEvent> Events;
		public virtual long Duration {
			get {
				if (Events == null)
					return 0;
				long tot = 0;
				foreach (DbgEvent e in Events) 
					tot += e.Duration;
				return tot;
			}
		}


	}
}