// Copyright (c) 2013-2019  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.IO;
using System.Xml;
using Drawing2D;

namespace Crow
{
	/// <summary>
	/// Base class for all templated widget.
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
		/// Template path or IML fragment.
		/// </summary>
		/// <remark>
		/// If both Template property and inline template are present, the second has priority.
		/// </remark>
		[DefaultValue(null)]
		public string Template {
			get => _template;
			set {
				//The 'null' default value with the 'NOT_SET' field init value  force a loading
				// of the default template by passing the first equality check.
				if (_template == value)
					return;
				_template = value;

				if (string.IsNullOrEmpty(_template))
					loadTemplate ();
				else if (_template.Trim().StartsWith('<'))//imlfragment
					loadTemplate (IFace.CreateITorFromIMLFragment (_template).CreateInstance());
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

		#region Widget overrides
		/// <summary>
		/// override search method from Widget to prevent
		/// searching inside template
		/// </summary>
		/// <returns>widget identified by name, or null if not found</returns>
		/// <param name="nameToFind">widget's name to find</param>
		public override Widget FindByName (string nameToFind) => nameToFind == this.Name ? this : null;
		//public override T FindByType<T> () => this is TemplatedControl tg ? tg : default (T);
		public Widget FindByNameInTemplate (string nameToFind) => child?.FindByName (nameToFind);
		/// <summary>
		///onDraw is overriden to prevent default drawing of background, template top container
		///may have a binding to root background or a fixed one.
		///this allow applying root background to random template's component
		/// </summary>
		/// <param name="gr">Backend context</param>
		protected override void onDraw (IContext gr)
		{
			DbgLogger.StartEvent (DbgEvtType.GODraw, this);

			if (ClipToClientRect) {
				//clip to client zone
				gr.Save ();
				CairoHelpers.CairoRectangle (gr, ClientRectangle, CornerRadius);
				gr.Clip ();
			}

			child?.Paint (gr);

			if (ClipToClientRect)
				gr.Restore ();

			DbgLogger.EndEvent (DbgEvtType.GODraw);
		}
		#endregion

		/// <summary>
		/// Loads the template. Each TemplatedControl should provide a default template
		/// otherwise it must have an inlined template in iml.
		/// It must be an embedded ressource with ID = fullTypeName.template
		/// </summary>
		/// <Remark>
		/// </Remark>
		/// <param name="template">Optional template instance</param>
		protected virtual void loadTemplate(Widget template = null)
		{
			// Setting the default template path in style will provide an interned string for itor search.
			if (this.child != null)//template change, bindings has to be reset
				this.ClearTemplateBinding();

			if (template == null) {
				try {
					string defaultTemplatePath = $"#{this.GetType().FullName}.template";
					this.SetChild (IFace.GetInstantiator (defaultTemplatePath).CreateInstance());
				} catch (Exception ex) {
					throw new Exception ($"Default template loading error for '{this.GetType ().FullName}'", ex);
				}
			}else
				this.SetChild (template);
		}
	}
}

