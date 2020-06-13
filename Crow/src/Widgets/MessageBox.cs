// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;

namespace Crow
{
	public class MessageBox : Window
	{
		#region CTOR
		protected MessageBox () {}
		public MessageBox (Interface iface, string style = null) : base(iface, style){}
		#endregion

		public enum Type {
			None,
			Information,
			Alert,
			Error,
			YesNo,
			YesNoCancel,

		}

		protected override void loadTemplate (Widget template)
		{
			base.loadTemplate (template);
			NotifyValueChanged ("MsgIcon", "#Crow.Icons.iconInfo.svg");
		}
		string message, okMessage, cancelMessage, noMessage;
		Type msgType = Type.None;

		public event EventHandler Yes;
		public event EventHandler No;
		public event EventHandler Cancel;

		[DefaultValue("Informations")]
		public virtual string Message
		{
			get { return message; }
			set {
				if (message == value)
					return;
				message = value;
				NotifyValueChangedAuto (message);
			}
		}
		[DefaultValue("Ok")]
		public virtual string OkMessage
		{
			get { return okMessage; }
			set {
				if (okMessage == value)
					return;
				okMessage = value;
				NotifyValueChangedAuto (okMessage);
			}
		}
		[DefaultValue("Cancel")]
		public virtual string CancelMessage
		{
			get { return cancelMessage; }
			set {
				if (cancelMessage == value)
					return;
				cancelMessage = value;
				NotifyValueChangedAuto (cancelMessage);
			}
		}
		[DefaultValue("No")]
		public virtual string NoMessage
		{
			get { return noMessage; }
			set {
				if (noMessage == value)
					return;
				noMessage = value;
				NotifyValueChangedAuto (noMessage);
			}
		}
		[DefaultValue("Information")]
		public virtual Type MsgType
		{
			get { return msgType; }
			set {
				if (msgType == value)
					return;
				msgType = value;
				NotifyValueChangedAuto (msgType);
				switch (msgType) {
				case Type.Information:
					MsgIcon = "#Crow.Icons.iconInfo.svg";
					Caption = "Informations";
					OkMessage = "Ok";
					NotifyValueChanged ("CancelButIsVisible", false);
					NotifyValueChanged ("NoButIsVisible", false);
					break;
				case Type.YesNo:
				case Type.YesNoCancel:
					MsgIcon = "#Crow.Icons.question.svg";
					Caption = "Choice";
					OkMessage = "Yes";
					NoMessage = "No";
					NotifyValueChanged ("CancelButIsVisible", msgType == Type.YesNoCancel);
					NotifyValueChanged ("NoButIsVisible", true);
					break;
				case Type.Alert:
					MsgIcon = "#Crow.Icons.IconAlerte.svg";
					Caption = "Alert";
					OkMessage = "Ok";
					CancelMessage = "Cancel";
					NotifyValueChanged ("CancelButIsVisible", true);
					NotifyValueChanged ("NoButIsVisible", false);
					break;
				case Type.Error:
					MsgIcon = "#Crow.Icons.exit.svg";
					Caption = "Error";
					OkMessage = "Ok";
					NotifyValueChanged ("CancelButIsVisible", false);
					NotifyValueChanged ("NoButIsVisible", false);
					break;
				}
			}
		}

		string msgIcon = null;
		public string MsgIcon {
			get { return msgIcon; }
			set {
				if (value == MsgIcon)
					return;
				msgIcon = value;
				NotifyValueChangedAuto (MsgIcon);
			}
		}
		void onOkButtonClick (object sender, EventArgs e)
		{
			Yes.Raise (this, null);
			close ();
		}
		void onNoButtonClick (object sender, EventArgs e)
		{
			No.Raise (this, null);
			close ();
		}
		void onCancelButtonClick (object sender, EventArgs e)
		{
			Cancel.Raise (this, null);
			close ();
		}
		public static MessageBox Show (Interface iface, Type msgBoxType, string message, string okMsg = "", string cancelMsg = ""){
			lock (iface.UpdateMutex) {
				MessageBox mb = new MessageBox (iface);
				mb.IFace.AddWidget (mb);
				mb.MsgType = msgBoxType;
				mb.Message = message;
				if (!string.IsNullOrEmpty(okMsg))
					mb.OkMessage = okMsg;
				if (!string.IsNullOrEmpty(cancelMsg))
					mb.CancelMessage = cancelMsg;
				return mb;
			}
		}
		public static MessageBox ShowModal (Interface iface, Type msgBoxType, string message){
			lock (iface.UpdateMutex) {
				MessageBox mb = new MessageBox (iface) {
					Modal = true,
					MsgType = msgBoxType,
					Message = message
				};

				iface.AddWidget (mb);
				return mb;
			}
		}
	}
}

