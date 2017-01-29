﻿//
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
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace Crow
{
	public abstract class TemplatedControl : PrivateContainer, IXmlSerializable
	{
		#region CTOR
		public TemplatedControl () : base()
		{
		}
		#endregion

		internal protected override void initialize ()
		{
			loadTemplate ();
			base.initialize ();
		}

		string _template;
		[XmlAttributeAttribute][DefaultValue(null)]
		public string Template {
			get { return _template; }
			set {
				if (Template == value)
					return;
				_template = value;

				if (string.IsNullOrEmpty(_template))
					loadTemplate ();
				else
					loadTemplate (CurrentInterface.Load (_template));
			}
		}

		#region GraphicObject overrides
		public override GraphicObject FindByName (string nameToFind)
		{
			//prevent name searching in template
			return nameToFind == this.Name ? this : null;
		}
		protected override void onDraw (Cairo.Context gr)
		{
			//onDraw is overrided to prevent default drawing of background, template top container
			//may have a binding to root background or a fixed one.
			//this allow applying root background to random template's component
			gr.Save ();

			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			if (child != null)
				child.Paint (ref gr);
			gr.Restore ();
		}
		#endregion

		protected virtual void loadTemplate(GraphicObject template = null)
		{
			if (template == null) {
				if (!Interface.DefaultTemplates.ContainsKey (this.GetType ().FullName))
					throw new Exception (string.Format ("No default template found for '{0}'", this.GetType ().FullName));
				this.SetChild (CurrentInterface.Load (Interface.DefaultTemplates[this.GetType ().FullName]));
			}else
				this.SetChild (template);
		}

		//TODO:IXmlSerializable is not used anymore
		#region IXmlSerializable
		public override System.Xml.Schema.XmlSchema GetSchema(){ return null; }
		public override void ReadXml(System.Xml.XmlReader reader)
		{
			//Template could be either an attribute containing path or expressed inlined
			//as a Template Element
			using (System.Xml.XmlReader subTree = reader.ReadSubtree())
			{
				subTree.Read ();

				string template = reader.GetAttribute ("Template");
				string tmp = subTree.ReadOuterXml ();

				//Load template from path set as attribute in templated control
				if (string.IsNullOrEmpty (template)) {
					//seek for template tag first
					using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
						//load template first if inlined

						xr.Read (); //read first child
						xr.Read (); //skip root node

						while (!xr.EOF) {
							if (!xr.IsStartElement ()) {
								xr.Read ();
								continue;
							}
							if (xr.Name == "ItemTemplate") {
								string dataType = "default", datas = "", itemTmp;
								while (xr.MoveToNextAttribute ()) {
									if (xr.Name == "DataType")
										dataType = xr.Value;
									else if (xr.Name == "Data")
										datas = xr.Value;
								}
								xr.MoveToElement ();
								itemTmp = xr.ReadInnerXml ();

//								if (ItemTemplates == null)
//									ItemTemplates = new Dictionary<string, ItemTemplate> ();
//
//								using (IMLReader iTmp = new IMLReader (null, itemTmp)) {
//									ItemTemplates [dataType] =
//										new ItemTemplate (iTmp.RootType, iTmp.GetLoader (), dataType, datas);
//								}
//								if (!string.IsNullOrEmpty (datas))
//									ItemTemplates [dataType].CreateExpandDelegate(this);

								continue;
							}
							if (xr.Name == "Template") {
								xr.Read ();

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
								GraphicObject go = (GraphicObject)Activator.CreateInstance (t);
								(go as IXmlSerializable).ReadXml (xr);

								loadTemplate (go);
								continue;
							}
							xr.ReadInnerXml ();
						}
					}
				} else
					loadTemplate (CurrentInterface.Load (template));

				//if no template found, load default one
				if (this.child == null)
					loadTemplate ();

				//normal xml read
				using (XmlReader xr = new XmlTextReader (tmp, XmlNodeType.Element, null)) {
					xr.Read ();
					base.ReadXml(xr);
				}
			}
		}
		public override void WriteXml(System.Xml.XmlWriter writer)
		{
			//TODO:
			throw new NotImplementedException();
		}
		#endregion
	}
}

