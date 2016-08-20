//
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
using System.Collections.Generic;

namespace Crow
{
	public abstract class TemplatedGroup : TemplatedControl
	{
		#region CTOR
		public TemplatedGroup () : base(){}
		#endregion

		protected Group items;

		public virtual List<GraphicObject> Items{ get { return items.Children; }}

		public virtual void AddItem(GraphicObject g){
			items.AddChild (g);
			NotifyValueChanged ("HasChildren", true);
			//g.LogicalParent = this;
		}
		public virtual void RemoveItem(GraphicObject g)
		{
			items.RemoveChild (g);
			if (items.Children.Count == 0)
				NotifyValueChanged ("HasChildren", false);
		}

		public virtual void ClearItems()
		{
			items.ClearChildren ();
			NotifyValueChanged ("HasChildren", false);
		}

		protected override void loadTemplate(GraphicObject template = null)
		{
			base.loadTemplate (template);

			items = this.child.FindByName ("ItemsContainer") as Group;
			if (items == null)
				throw new Exception ("TemplatedGroup template Must contain a Group named 'ItemsContainer'");
			if (items.Children.Count == 0)
				NotifyValueChanged ("HasChildren", false);
			else
				NotifyValueChanged ("HasChildren", true);
		}

		#region GraphicObject overrides
		public override GraphicObject FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			foreach (GraphicObject w in Items) {
				GraphicObject r = w.FindByName (nameToFind);
				if (r != null)
					return r;
			}
			return null;
		}
		public override bool Contains (GraphicObject goToFind)
		{
			foreach (GraphicObject w in Items) {
				if (w == goToFind)
					return true;
				if (w.Contains (goToFind))
					return true;
			}
			return false;
		}
//		public override void ClearBinding ()
//		{
//			if (items != null)
//				items.ClearBinding ();
//
//			base.ClearBinding ();
//		}
//		public override void ResolveBindings ()
//		{
//			base.ResolveBindings ();
//			if (items != null)
//				items.ResolveBindings ();
//		}
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

						if (xr.Name == "Template" || Name == "ItemTemplate"){
							xr.Skip ();
							if (!xr.IsStartElement ())
								continue;
						}

						Type t = Type.GetType ("Crow." + xr.Name);
						if (t == null) {
							Assembly a = Assembly.GetEntryAssembly ();
							foreach (Type expT in a.GetExportedTypes ()) {
								if (expT.Name == xr.Name)
									t = expT;
							}
						}
						if (t == null)
							throw new Exception (xr.Name + " type not found");

						GraphicObject go = (GraphicObject)Activator.CreateInstance (t);

						(go as IXmlSerializable).ReadXml (xr);

						AddItem (go);

						xr.Read (); //closing tag
					}

				}
			}
		}
		public override void WriteXml(System.Xml.XmlWriter writer)
		{
			throw new NotImplementedException ();
		}
		#endregion
	}
}
