// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Threading;
using System.Xml.Serialization;

namespace Crow.Coding
{
	public abstract class Editor : ScrollingObject
	{
		#region CTOR
		protected Editor ():base(){
			Thread t = new Thread (backgroundThreadFunc);
			t.IsBackground = true;
			t.Start ();
		}
		#endregion

		protected ReaderWriterLockSlim editorMutex = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);
		protected ProjectFileNode projFile = null;
		Exception error = null;

		public virtual ProjectFileNode ProjectNode
		{
			get { return projFile; }
			set
			{
				if (projFile == value)
					return;

				if (projFile != null)
					projFile.UnregisterEditor (this);

				projFile = value;

				if (projFile != null)					
					projFile.RegisterEditor (this);

				NotifyValueChanged ("ProjectNode", projFile);
			}
		}
		[XmlIgnore]public Exception Error {
			get { return error; }
			set {
				if (error == value)
					return;
				error = value;
				NotifyValueChanged ("Error", error);
				NotifyValueChanged ("HasError", HasError);
			}
		}
		[XmlIgnore]public bool HasError {
			get { return error != null; }
		}

		protected abstract void updateEditorFromProjFile ();
		protected abstract void updateProjFileFromEditor ();
		protected abstract bool EditorIsDirty { get; set; }
		protected virtual bool IsReady { get { return true; }}
		protected virtual void updateCheckPostProcess () {}

		protected void backgroundThreadFunc () {
			while (true) {
				if (IsReady) {
					if (Monitor.TryEnter (IFace.UpdateMutex)) {
						if (!projFile.RegisteredEditors [this]) {
							projFile.RegisteredEditors [this] = true;
							updateEditorFromProjFile ();
						} else if (EditorIsDirty) {
							EditorIsDirty = false;
							updateProjFileFromEditor ();
						}
						updateCheckPostProcess ();
						Monitor.Exit (IFace.UpdateMutex);
					}
				}
				Thread.Sleep (100);
			}	
		}
	}
}

