//
// FileDialog.cs
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Crow
{
	/// <summary>
	/// templated control for selecting files
	/// </summary>
	public class FileDialog: Window
	{
		#region CTOR
		protected FileDialog() : base(){}
		public FileDialog (Interface iface) : base(iface){}
		#endregion

		string searchPattern, curDir, _selectedFile, _selectedDir;
		bool showHidden, showFiles;

		#region events
		public event EventHandler OkClicked;
		#endregion

		public string SelectedFileFullPath {
			get { return Path.Combine (SelectedDirectory, SelectedFile); }
		}
		[XmlAttributeAttribute][DefaultValue("/home")]
		public virtual string CurrentDirectory {
			get { return curDir; }
			set {
				if (curDir == value)
					return;
				curDir = value;
				NotifyValueChanged ("CurrentDirectory", curDir);
				SelectedDirectory = curDir;
			}
		}

		[XmlAttributeAttribute][DefaultValue("*")]
		public virtual string SearchPattern {
			get { return searchPattern; }
			set {
				if (searchPattern == value)
					return;
				searchPattern = value;
				NotifyValueChanged ("SearchPattern", searchPattern);

			}
		}
		[XmlAttributeAttribute()][DefaultValue(false)]
		public virtual bool ShowHidden {
			get { return showHidden; }
			set {
				if (showHidden == value)
					return;
				showHidden = value;
				NotifyValueChanged ("ShowHidden", showHidden);
			}
		}
		[XmlAttributeAttribute()][DefaultValue(true)]
		public virtual bool ShowFiles {
			get { return showFiles; }
			set {
				if (showFiles == value)
					return;
				showFiles = value;
				NotifyValueChanged ("ShowFiles", showFiles);
			}
		}
		[XmlAttributeAttribute]public string SelectedFile {
			get { return _selectedFile; }
			set {
				if (value == _selectedFile)
					return;
				_selectedFile = value;
				NotifyValueChanged ("SelectedFile", _selectedFile);
			}
		}
		[XmlAttributeAttribute]public string SelectedDirectory {
			get { return _selectedDir; }
			set {
				if (value == _selectedDir)
					return;
				_selectedDir = value;
				NotifyValueChanged ("SelectedDirectory", _selectedDir);
			}
		}

		public void onFVSelectedItemChanged (object sender, SelectionChangeEventArgs e){
			if (e.NewValue != null) {
				if (File.GetAttributes (e.NewValue.ToString ()).HasFlag (FileAttributes.Directory)) {
					SelectedDirectory = e.NewValue.ToString ();
					SelectedFile = "";
				} else {
					SelectedDirectory = Path.GetDirectoryName (e.NewValue.ToString ());
					SelectedFile = Path.GetFileName (e.NewValue.ToString ());
				}
			}
		}
		public void onDVSelectedItemChanged (object sender, SelectionChangeEventArgs e){
			if (e.NewValue != null)
				SelectedDirectory = e.NewValue.ToString();
		}
		public void goUpDirClick (object sender, MouseButtonEventArgs e){
			string root = Directory.GetDirectoryRoot(CurrentDirectory);
			if (CurrentDirectory == root)
				return;
			CurrentDirectory = Directory.GetParent(CurrentDirectory).FullName;
		}
		void onFileSelect(object sender, MouseButtonEventArgs e){
			if (string.IsNullOrEmpty (SelectedFile))
				CurrentDirectory = SelectedDirectory;
			else {
				OkClicked.Raise (this, null);
				IFace.DeleteWidget (this);
			}
		}
		void onCancel(object sender, MouseButtonEventArgs e){
			IFace.DeleteWidget (this);
		}

	}
}

