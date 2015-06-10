//
//  TemplatedControl.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;
using System.Xml;

namespace go
{
	public abstract class TemplatedControl : PrivateContainer, IXmlSerializable
	{
		public TemplatedControl () : base()
		{
		}

//		string _templatePath;
//		[XmlAttributeAttribute()]
//		public virtual string Template {
//			get { return _templatePath; }
//			set { _templatePath = value; }
//		}


		protected abstract void loadTemplate(GraphicObject template = null);

		protected override void loadDefaultValues ()
		{
			if (child == null)//trigger loading of default template if child is empty
				loadTemplate ();
			base.loadDefaultValues ();
			this.Focusable = true;
		}

		public override GraphicObject FindByName (string nameToFind)
		{
			//prevent name searching in template
			return nameToFind == this.Name ? this : null;
		}

		#region IXmlSerializable

		public override System.Xml.Schema.XmlSchema GetSchema()
		{
			return null;
		}
		public override void ReadXml(System.Xml.XmlReader reader)
		{
			using (System.Xml.XmlReader subTree = reader.ReadSubtree())
			{
				subTree.Read ();
				string tmp = subTree.ReadOuterXml ();

					//seek for template tag first
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					//load template first if inlined

					xr.Read (); //skip current node

					while (!xr.EOF) {
						xr.Read (); //read first child
						if (!xr.IsStartElement ())
							continue;
						if (xr.Name == "Template") {
							xr.Read ();

							Type t = Type.GetType ("go." + xr.Name);
							GraphicObject go = (GraphicObject)Activator.CreateInstance (t);                                
							(go as IXmlSerializable).ReadXml (xr);

							loadTemplate (go);

							xr.Read ();//go close tag
							xr.Read ();//Template close tag
						} else {
							xr.ReadInnerXml ();
						}
					}
				}
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read ();
					base.ReadXml(xr);
				}
			}
		}
		public override void WriteXml(System.Xml.XmlWriter writer)
		{
			base.WriteXml(writer);
//
//			if (child == null)
//				return;
//
//			writer.WriteStartElement(child.GetType().Name);
//			(child as IXmlSerializable).WriteXml(writer);
//			writer.WriteEndElement();
		}

		#endregion
	}
}

