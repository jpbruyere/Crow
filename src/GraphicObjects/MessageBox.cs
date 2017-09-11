//
// MessageBox.cs
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
			close ();
		}
		void onCancelButtonClick (object sender, EventArgs e)
		{
			Cancel.Raise (this, null);
			close ();
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

