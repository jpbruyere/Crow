// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Xml.Serialization;
using System.Reflection;
using System.ComponentModel;
using System.Linq;
using System.Threading;

namespace Crow
{
	/// <summary>
	/// simple container accepting one child
	/// </summary>
    public class Container : PrivateContainer
    {
		#if DESIGN_MODE
		public override void getIML (System.Xml.XmlDocument doc, System.Xml.XmlNode parentElem)
		{
			if (this.design_isTGItem)
				return;
			base.getIML (doc, parentElem);
			if (child == null)
				return;
			child.getIML (doc, parentElem.LastChild);
		}
		#endif

		#region CTOR
		protected Container() : base(){}
		public Container (Interface iface) : base(iface){}
		#endregion

		[XmlIgnore]public Widget Child {
			get { return child; }
			set { base.SetChild(value); }
		}
		/// <summary>
		/// override this to handle specific steps in child addition in derived class,
		/// and don't forget to call the base.SetChild
		/// </summary>
		public virtual void SetChild(Widget _child)
		{
			base.SetChild (_child);
		}
	}
}

