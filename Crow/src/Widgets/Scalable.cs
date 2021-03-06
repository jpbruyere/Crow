﻿// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// Base class for controling scalable content behaviour.
	/// </summary>
	public abstract class Scalable : Widget
	{
		#region CTOR
		protected Scalable () { }
		public Scalable (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		protected bool scaled, keepProps;
		/// <summary>
		/// If true, content will be scalled to fit widget client area.
		/// </summary>
		[DefaultValue (true)]
		public virtual bool Scaled {
			get { return scaled; }
			set {
				if (scaled == value)
					return;
				scaled = value;
				NotifyValueChangedAuto (scaled);
				RegisterForGraphicUpdate ();
			}
		}
		/// <summary>
		/// If image is scaled, proportions will be preserved.
		/// </summary>
		[DefaultValue (true)]
		public virtual bool KeepProportions {
			get { return keepProps; }
			set {
				if (keepProps == value)
					return;
				keepProps = value;
				NotifyValueChangedAuto (keepProps);
				RegisterForGraphicUpdate ();
			}
		}


	}
}
