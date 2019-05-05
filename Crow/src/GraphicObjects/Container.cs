//
// Container.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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

