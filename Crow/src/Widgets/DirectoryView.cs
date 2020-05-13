//
// DirectoryView.cs
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
using System.Linq;

namespace Crow
{
	/// <summary>
	/// templated directory viewer
	/// </summary>
	public class DirectoryView : TemplatedControl
	{
		#region CTOR
		protected DirectoryView() : base(){}
		public DirectoryView (Interface iface) : base(iface){}
		#endregion

		#region events
		public event EventHandler<SelectionChangeEventArgs> SelectedItemChanged;
		#endregion

		string currentDirectory = "/";
		bool showFiles, showHidden;
		string fileMask = "*.*";

		object _selectedItem;
		[XmlIgnore]public object SelectedItem {
			get {
				return _selectedItem;
			}
			set { 
				if (value == _selectedItem)
					return;
				_selectedItem = value;
				NotifyValueChanged ("SelectedItem", _selectedItem);
			}
		}
		[DefaultValue(true)]
		public virtual bool ShowFiles {
			get { return showFiles; }
			set {
				if (showFiles == value)
					return;
				showFiles = value;
				NotifyValueChanged ("ShowFiles", showFiles);
				NotifyValueChanged ("FileSystemEntries", FileSystemEntries);
			}
		}
		[DefaultValue(false)]
		public virtual bool ShowHidden {
			get { return showHidden; }
			set {
				if (showHidden == value)
					return;
				showHidden = value;
				NotifyValueChanged ("ShowHidden", showHidden);
				NotifyValueChanged ("FileSystemEntries", FileSystemEntries);
			}
		}
		[DefaultValue("*.*")]
		public virtual string FileMask {
			get { return fileMask; }
			set {
				if (fileMask == value)
					return;
				fileMask = value;
				NotifyValueChanged ("FileMask", fileMask);
				NotifyValueChanged ("FileSystemEntries", FileSystemEntries);
			}
		}
		[DefaultValue("/")]
		public virtual string CurrentDirectory {
			get { return currentDirectory; }
			set {
				if (currentDirectory == value)
					return;
				currentDirectory = value;
				NotifyValueChanged ("CurrentDirectory", currentDirectory);
				NotifyValueChanged ("FileSystemEntries", FileSystemEntries);
			}
		}
		[XmlIgnore]public FileSystemInfo[] FileSystemEntries {
			get {
				try {
					if (string.IsNullOrEmpty(CurrentDirectory))
						return null;
					DirectoryInfo di = new DirectoryInfo(CurrentDirectory);
					List<FileSystemInfo> fi = new List<FileSystemInfo> (di.GetDirectories());
					if (showFiles && !string.IsNullOrEmpty(fileMask))
						fi.AddRange(di.GetFiles(fileMask));
					return showHidden ?
						fi.ToArray() :
						fi.Where(f=>!f.Attributes.HasFlag (FileAttributes.Hidden)).ToArray();
				} catch (Exception ex) {
					System.Diagnostics.Debug.WriteLine (ex.ToString ());
					return null;
				}
			}
		}
		public void onSelectedItemChanged (object sender, SelectionChangeEventArgs e){
			if (e.NewValue == SelectedItem)
				return;
			SelectedItem = e.NewValue;
			SelectedItemChanged.Raise (this, e);
		}
	}
}

