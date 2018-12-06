//
// Editor.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
		protected ProjectFile projFile = null;
		Exception error = null;

		public virtual ProjectFile ProjectNode
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

