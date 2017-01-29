﻿//
//  TemplatedContainer.cs
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
using System.Xml;
using System.Reflection;

namespace Crow
{
	public abstract class TemplatedContainer : TemplatedControl
	{
		#region CTOR
		public TemplatedContainer () : base(){}
		#endregion

		[XmlIgnore]public abstract GraphicObject Content{ get; set;}

		#region GraphicObject overrides
		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			return Content == null ? null : Content.FindByName (nameToFind);
		}
		public override bool Contains (GraphicObject goToFind)
		{
			if (Content == null)
				return base.Contains (goToFind);

			if (Content == goToFind)
				return true;
			return Content.Contains (goToFind);
		}
		#endregion

		#region IXmlSerialisation Overrides
		public override void ReadXml(System.Xml.XmlReader reader)
		{
			using (System.Xml.XmlReader subTree = reader.ReadSubtree ()) {
				subTree.Read ();
				string tmp = subTree.ReadOuterXml ();

				//seek for template tag
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read ();
					base.ReadXml (xr);
				}
				//process content
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read (); //skip current node

					while (!xr.EOF) {
						xr.Read (); //read first child

						if (!xr.IsStartElement ())
							continue;

						if (xr.Name == "Template"){
							xr.Skip ();
							if (!xr.IsStartElement ())
								continue;
						}

						Type t = Type.GetType ("Crow." + xr.Name);
						if (t == null) {
							Assembly a = Assembly.GetEntryAssembly ();
							foreach (Type expT in a.GetExportedTypes ()) {
								if (expT.Name == xr.Name) {
									t = expT;
									break;
								}
							}
						}
						if (t == null)
							throw new Exception (xr.Name + " type not found");

						GraphicObject go = (GraphicObject)Activator.CreateInstance (t);

						(go as IXmlSerializable).ReadXml (xr);

						Content = go;

						xr.Read (); //closing tag
					}

				}
			}
		}
		public override void WriteXml(System.Xml.XmlWriter writer)
		{
			base.WriteXml(writer);

			if (Content == null)
				return;
			//TODO: if template is not the default one, we have to save it
			writer.WriteStartElement(Content.GetType().Name);
			(Content as IXmlSerializable).WriteXml(writer);
			writer.WriteEndElement();
		}
		#endregion
	}
}

