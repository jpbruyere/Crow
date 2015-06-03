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

namespace go
{
	public abstract class TemplatedControl : Container, IXmlSerializable
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


		protected abstract void loadTemplate();

		protected override void loadDefaultValues ()
		{
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
			base.ReadXml(reader);

//			using (System.Xml.XmlReader subTree = reader.ReadSubtree())
//			{
//				subTree.Read(); //skip current node
//				subTree.Read(); //read first child
//
//				if (!subTree.IsStartElement())
//					return;
//
//				Type t = Type.GetType("go." + subTree.Name);
//				GraphicObject go = (GraphicObject)Activator.CreateInstance(t);                                
//
//				(go as IXmlSerializable).ReadXml(subTree);
//
//				setChild(go);
//
//				subTree.Read();
//
//				if (!subTree.IsStartElement())
//					return;
//
//			}
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

