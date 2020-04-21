// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	/// <summary>
	/// templated container accepting one child
	/// </summary>
    public class GroupBox : TemplatedContainer
    {		
		#region CTOR
		protected GroupBox () : base(){}
		public GroupBox(Interface iface) : base(iface){}
		#endregion
	}
}
