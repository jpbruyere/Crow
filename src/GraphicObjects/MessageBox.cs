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
		public enum Type {
			Information,
			YesNo,
			Alert,
			Error
		}
		public MessageBox (): base(){}

		protected override void loadTemplate (GraphicObject template)
		{
			base.loadTemplate (template);
			NotifyValueChanged ("MsgIcon", "#Crow.Images.Icons.Informations.svg");
		}
		string message, okMessage, cancelMessage;
		Type msgType = Type.Information;

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
		[XmlAttributeAttribute][DefaultValue("Ok")]
		public virtual string OkMessage
		{
			get { return okMessage; }
			set {
				if (okMessage == value)
					return;
				okMessage = value;
				NotifyValueChanged ("OkMessage", okMessage);
			}
		}
		[XmlAttributeAttribute][DefaultValue("Cancel")]
		public virtual string CancelMessage
		{
			get { return cancelMessage; }
			set {
				if (cancelMessage == value)
					return;
				cancelMessage = value;
				NotifyValueChanged ("CancelMessage", cancelMessage);
			}
		}
		[XmlAttributeAttribute][DefaultValue("Information")]
		public virtual Type MsgType
		{
			get { return msgType; }
			set {
				if (msgType == value)
					return;
				msgType = value;
				NotifyValueChanged ("MsgType", msgType);
				switch (msgType) {
				case Type.Information:
					NotifyValueChanged ("MsgIcon", "#Crow.Images.Icons.Informations.svg");
					Caption = "Informations";
					OkMessage = "Ok";
					CancelMessage = "Cancel";
					break;
				case Type.YesNo:
					NotifyValueChanged ("MsgIcon", "#Crow.Icons.question.svg");
					Caption = "Choice";
					OkMessage = "Yes";
					CancelMessage = "No";
					break;
				case Type.Alert:
					NotifyValueChanged ("MsgIcon", "#Crow.Images.Icons.IconAlerte.svg");
					Caption = "Alert";
					OkMessage = "Ok";
					CancelMessage = "Cancel";
					break;
				case Type.Error:
					NotifyValueChanged ("MsgIcon", "#Crow.Images.Icons.exit.svg");
					Caption = "Error";
					OkMessage = "Ok";
					CancelMessage = "Cancel";
					break;
				}
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
		public static MessageBox Show (Type msgBoxType, string message, string okMsg = "", string cancelMsg = ""){
			lock (Interface.CurrentInterface.UpdateMutex) {
				MessageBox mb = new MessageBox ();
				mb.Initialize ();
				mb.CurrentInterface.AddWidget (mb);
				mb.MsgType = msgBoxType;
				mb.Message = message;
				if (!string.IsNullOrEmpty(okMsg))
					mb.OkMessage = okMsg;
				if (!string.IsNullOrEmpty(cancelMsg))
					mb.CancelMessage = cancelMsg;
				return mb;
			}
		}
	}
}

