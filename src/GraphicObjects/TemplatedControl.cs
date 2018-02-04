//
// TemplatedControl.cs
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
using System.ComponentModel;
using System.IO;
using System.Xml;
using System.Diagnostics;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using Cairo;

namespace Crow
{
	/// <summary>
	/// Base class for all templated widget
	/// </summary>
	public abstract class TemplatedControl : PrivateContainer
	{
		#region CTOR
		public TemplatedControl() : base(){}
		public TemplatedControl (Interface iface) : base(iface){}
		#endregion

		string _template;
		string caption;

		/// <summary>
		/// Template path
		/// </summary>
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
		/// <summary>
		/// caption property being recurrent in templated widget, it is declared here.
		/// </summary>
		[XmlAttributeAttribute()][DefaultValue("Templated Control")]
		public virtual string Caption {
			get { return caption; }
			set {
				if (caption == value)
					return;
				caption = value;
				NotifyValueChanged ("Caption", caption);
			}
		}

		#region GraphicObject overrides

		public override void Initialize ()
		{
			loadTemplate ();
			base.Initialize ();
		}
		/// <summary>
		/// override search method from GraphicObject to prevent
		/// searching inside template
		/// </summary>
		/// <returns>widget identified by name, or null if not found</returns>
		/// <param name="nameToFind">widget's name to find</param>
		public override GraphicObject FindByName (string nameToFind)
		{
			//prevent name searching in template
			return nameToFind == this.Name ? this : null;
		}
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
				child.Paint (ref gr);
			gr.Restore ();
		}
		#endregion

		/// <summary>
		/// Loads the template.
		/// </summary>
		/// <param name="template">Optional template instance</param>
		protected virtual void loadTemplate(GraphicObject template = null)
		{
			if (this.child != null)//template change, bindings has to be reset
				this.ClearTemplateBinding();
			
			if (template == null) {
				if (!Interface.DefaultTemplates.ContainsKey (this.GetType ().FullName))
					throw new Exception (string.Format ("No default template found for '{0}'", this.GetType ().FullName));
				this.SetChild (CurrentInterface.Load (Interface.DefaultTemplates[this.GetType ().FullName]));
			}else
				this.SetChild (template);
		}
	}
}

