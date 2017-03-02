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
    public class Container : PrivateContainer, IXmlSerializable
    {
		#region CTOR
		public Container()
			: base()
		{
		}
		#endregion

		[XmlIgnore]
		public GraphicObject Child {
			get { return child; }
			set { child = value; }
		}
		public virtual void SetChild(GraphicObject _child)
		{
			base.SetChild (_child);
		}

		#region IXmlSerializable

        public override System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }
        public override void ReadXml(System.Xml.XmlReader reader)
        {
			//only read attributes in GraphicObject IXmlReader implementation
            base.ReadXml(reader);


            using (System.Xml.XmlReader subTree = reader.ReadSubtree())
            {
                subTree.Read(); //skip current node
                subTree.Read(); //read first child

                if (!subTree.IsStartElement())
                    return;

                Type t = Type.GetType("Crow." + subTree.Name);
				if (t == null) {
					Assembly a = Assembly.GetEntryAssembly ();
					foreach (Type expT in a.GetExportedTypes ()) {
						if (expT.Name == subTree.Name) {
							t = expT;
							break;
						}
					}
				}
				GraphicObject go = (GraphicObject)Activator.CreateInstance(t);

				(go as IXmlSerializable).ReadXml(subTree);

				SetChild(go);

                subTree.Read();//closing tag
            }
        }
        public override void WriteXml(System.Xml.XmlWriter writer)
        {
            base.WriteXml(writer);

            if (Child == null)
                return;

            writer.WriteStartElement(Child.GetType().Name);
            (Child as IXmlSerializable).WriteXml(writer);
            writer.WriteEndElement();
        }
    
		#endregion
	}
}

