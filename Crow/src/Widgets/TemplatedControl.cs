// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Xml;
#if VKVG
using vkvg;
#else
using Crow.Cairo;
#endif

namespace Crow
{
	/// <summary>
	/// Base class for all templated widget
	/// </summary>
	public abstract class TemplatedControl : PrivateContainer
	{
		#if DESIGN_MODE
		public bool design_inlineTemplate = false;
		public override void getIML (XmlDocument doc, XmlNode parentElem)
		{
			if (this.design_isTGItem)
				return;
			base.getIML (doc, parentElem);
			if (child == null || !design_inlineTemplate)
				return;
			XmlElement xe = doc.CreateElement("Template");
			child.getIML (doc, xe);
			parentElem.LastChild.AppendChild (xe);
		}
		#endif

		#region CTOR
		protected TemplatedControl() {}
		protected TemplatedControl (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		string _template = "NOT_SET";
		string caption;

		/// <summary>
		/// Template path
		/// </summary>
		//TODO: this property should be renamed 'TemplatePath'
		[DefaultValue(null)]
		public string Template {
			get { return _template; }
			set {
				if (_template == value)
					return;
				_template = value;

				if (string.IsNullOrEmpty(_template))
					loadTemplate ();
				else
					loadTemplate (IFace.CreateInstance (_template));
			}
		}
		/// <summary>
		/// a caption being recurrent need in templated widget, it is declared here.
		/// </summary>
		[DefaultValue("Templated Control")]
		public virtual string Caption {
			get { return caption; }
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChangedAuto (caption);
			}
		}

		#region GraphicObject overrides
		/// <summary>
		/// override search method from GraphicObject to prevent
		/// searching inside template
		/// </summary>
		/// <returns>widget identified by name, or null if not found</returns>
		/// <param name="nameToFind">widget's name to find</param>
		public override Widget FindByName (string nameToFind) => nameToFind == this.Name ? this : null;
		public override Widget FindByType<T> () => this is TemplatedControl ? this : null;
		public Widget FindByNameInTemplate (string nameToFind) => child?.FindByName (nameToFind);
		/// <summary>
		///onDraw is overrided to prevent default drawing of background, template top container
		///may have a binding to root background or a fixed one.
		///this allow applying root background to random template's component
		/// </summary>
		/// <param name="gr">Backend context</param>
		protected override void onDraw (Context gr)
		{
			gr.Save ();

			if (ClipToClientRect) {
				//clip to client zone
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			if (child != null)
				child.Paint (gr);
			gr.Restore ();
		}
		#endregion

		/// <summary>
		/// Loads the template. Each TemplatedControl MUST provide a default template
		/// It must be an embedded ressource with ID = fullTypeName.template
		/// Entry assembly is search first, then the one where the type is defined
		/// </summary>
		/// <param name="template">Optional template instance</param>
		protected virtual void loadTemplate(Widget template = null)
		{
			if (this.child != null)//template change, bindings has to be reset
				this.ClearTemplateBinding();
			
			if (template == null) {
				string defTmpId = this.GetType ().FullName + ".template";
				if (!IFace.DefaultTemplates.ContainsKey (defTmpId)) {
					
					Stream s = IFace.GetStreamFromPath ("#" + defTmpId);
					if (s == null)
						throw new Exception (string.Format ("No default template found for '{0}'", this.GetType ().FullName));
					IFace.DefaultTemplates [defTmpId] = new IML.Instantiator (IFace, s, defTmpId);
				}
				this.SetChild (IFace.DefaultTemplates[defTmpId].CreateInstance());
			}else
				this.SetChild (template);
		}
	}
}

