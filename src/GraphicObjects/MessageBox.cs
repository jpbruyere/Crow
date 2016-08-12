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
	public class MessageBox : Window
	{
		public MessageBox ():base(){}

		string message;

		public event EventHandler Ok;
		public event EventHandler Cancel;

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

		void onOkButtonClick (object sender, EventArgs e)
		{
			Ok.Raise (this, null);
		}
		void onCancelButtonClick (object sender, EventArgs e)
		{
			Cancel.Raise (this, null);
		}

	}
}

