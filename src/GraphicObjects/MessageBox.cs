//
//  MessageBox.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Generaltitlec License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURtitle See the
//  GNU General Putitleicense for more details.
//titleou should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml.Serialization;
using System.ComponentModel;

namespace Crow
{
	[DefaultTemplate("#Crow.Templates.MessageBox.goml")]
	public class MessageBox : Window
	{
		public MessageBox ():base(){}

		string title;
		string message;

		[XmlAttributeAttribute][DefaultValue("Message box")]
		public virtual string Title
		{
			get { return title; }
			set {
				if (title == value)
					return;
				title = value;
				NotifyValueChanged ("Title", title);
			}
		}
		[XmlAttributeAttribute][DefaultValue("Informations")]
		public virtual string Message
		{
			get { return message; }
			set {
				if (message == value)
					return;
				message = value;
				NotifyValueChanged ("Message", message);
			}
		}
		#region GraphicObject overrides
		[XmlAttributeAttribute][DefaultValue(250)]
		public override int Width {
			get { return base.Width; }
			set { base.Width = value; }
		}
//		[XmlAttributeAttribute][DefaultValue(80)]
//		public override int Height {
//			get { return base.Height; }
//			set { base.Height = value; }
//		}
		[XmlAttributeAttribute()][DefaultValue(true)]
		public override bool Focusable
		{
			get { return base.Focusable; }
			set { base.Focusable = value; }
		}
		[XmlAttributeAttribute()][DefaultValue("150;80")]
		public override Size MinimumSize
		{
			get { return base.MinimumSize; }
			set { base.MinimumSize = value; }
		}
		#endregion
	}
}

