// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Crow.DebugLogger
{
	//base class for both events and widgetRecord having an event list
	public abstract class DbgEventSource
	{
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