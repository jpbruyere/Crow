//
//  DirectoryView.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml.Serialization;
using System.ComponentModel;
using System.IO;

namespace Crow
{
	public class DirectoryView : TemplatedControl
	{
		#region CTOR
		public DirectoryView ()
			: base()
		{}
		#endregion

		#region events
		public event EventHandler<SelectionChangeEventArgs> SelectedItemChanged;
		#endregion

		string currentDirectory = "/";
		bool showFiles;

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
		[XmlAttributeAttribute()][DefaultValue(true)]
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
		[XmlAttributeAttribute][DefaultValue("/")]
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
				return string.IsNullOrEmpty(CurrentDirectory) ? null :
					showFiles ?					
					new DirectoryInfo (CurrentDirectory).GetFileSystemInfos () :
					new DirectoryInfo(CurrentDirectory).GetDirectories(); 
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

