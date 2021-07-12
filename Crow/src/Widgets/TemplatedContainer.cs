// Copyright (c) 2013-2021  Bruyère Jean-Philippe jp_bruyere@hotmail.com
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;

namespace Crow
{
	/// <summary>
	/// base class for new containers that will use templates.
	/// 
	/// TemplatedControl's **must** provide a widget of the [`Container`](Container) class named **_'Content'_** inside their template tree
	/// </summary>
	public class TemplatedContainer : TemplatedControl
	{
#if DESIGN_MODE
		public override void getIML (System.Xml.XmlDocument doc, System.Xml.XmlNode parentElem)
		{
			if (this.design_isTGItem)
				return;
			base.getIML (doc, parentElem);
			if (!HasContent)
				return;
			Content.getIML (doc, parentElem.LastChild);
		}
#endif

#region CTOR
		protected TemplatedContainer() {}
		public TemplatedContainer (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		protected Container _contentContainer;

		/// <summary>
		/// Single child of this templated container.
		/// </summary>
		public virtual Widget Content {
			get {
				return _contentContainer == null ? null : _contentContainer.Child;
			}
			set {
				if (_contentContainer == null)
					throw new Exception ("TemplatedContainer template Must contain a Container named 'Content'");
				_contentContainer.SetChild(value);
				value.LogicalParent = this;
				NotifyValueChanged ("HasContent", HasContent);
			}
		}
		[XmlIgnore]public bool HasContent {
			get { return _contentContainer?.Child != null; }
		}
		//TODO: move loadTemplate and ResolveBinding in TemplatedContainer
		protected override void loadTemplate(Widget template = null)
		{
			base.loadTemplate (template);
			_contentContainer = this.child.FindByName ("Content") as Container;
		}

#region GraphicObject overrides
		public override Widget FindByName (string nameToFind)
		{
			if (Name == nameToFind)
				return this;

			return Content == null ? null : Content.FindByName (nameToFind);
		}
		public override T FindByType<T> ()
		{
			if (this is T t)
				return t;

			return Content == null ? default(T) : Content.FindByType<T> ();
		}
		public override bool Contains (Widget goToFind)
		{
			if (Content == goToFind)
				return true;
			if (Content?.Contains (goToFind) == true)
				return true;
			return base.Contains (goToFind);
		}
#endregion
	}
}

