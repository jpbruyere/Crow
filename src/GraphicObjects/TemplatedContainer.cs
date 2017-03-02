//
// TemplatedContainer.cs
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
using System.Xml;
using System.Reflection;

namespace Crow
{
	public abstract class TemplatedContainer : TemplatedControl
	{
		#region CTOR
		public TemplatedContainer () : base(){}
		#endregion

		[XmlAttributeAttribute]public virtual GraphicObject Content{ get; set;}

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

