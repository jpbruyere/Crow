// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;

namespace Crow
{
	/// <summary>
	/// templated directory viewer
	/// </summary>
	public class DirectoryView : TemplatedControl
	{
		#region CTOR
		protected DirectoryView() {}
		public DirectoryView (Interface iface, string style = null) : base (iface, style) { }
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
				NotifyValueChangedAuto (_selectedItem);
			}
		}
		[DefaultValue(true)]
		public virtual bool ShowFiles {
			get { return showFiles; }
			set {
				if (showFiles == value)
					return;
				showFiles = value;
				NotifyValueChangedAuto (showFiles);
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
				NotifyValueChangedAuto (showHidden);
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
				NotifyValueChangedAuto (fileMask);
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
				NotifyValueChangedAuto (currentDirectory);
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
						fi.OrderBy(f=>f.Attributes).ThenBy(f=>f.Name).ToArray() :
						fi.Where(f=>!f.Attributes.HasFlag (FileAttributes.Hidden)).OrderBy (f => f.Attributes).ThenBy (f => f.Name).ToArray();
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

