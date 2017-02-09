//
//  FileDialog.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text.RegularExpressions;

namespace Crow
{
	public class FileDialog: Window
	{
		string searchPattern, curDirectory;

		#region events
		public event EventHandler OkClicked;
		#endregion

		#region CTOR
		public FileDialog () : base()
		{
		}
		#endregion

		[XmlAttributeAttribute][DefaultValue("/home")]
		public virtual string CurrentDirectory {
			get { return curDirectory; }
			set {
				if (curDirectory == value)
					return;
				curDirectory = value;
				NotifyValueChanged ("CurrentDirectory", curDirectory);

			}
		}

		[XmlAttributeAttribute()][DefaultValue("*")]
		public virtual string SearchPattern {
			get { return searchPattern; }
			set {
				if (searchPattern == value)
					return;
				searchPattern = value;
				NotifyValueChanged ("SearchPattern", searchPattern);

			}
		}
		string _selectedFile;
		[XmlIgnore]public string SelectedFile {
			get {
				return _selectedFile;
			}
			set {
				if (value == _selectedFile)
					return;
				_selectedFile = value;
				NotifyValueChanged ("SelectedFile", _selectedFile);
			}
		}
		public void onSelectedItemChanged (object sender, SelectionChangeEventArgs e){

			string tmp = "";
			if (e.NewValue != null)
				tmp = e.NewValue.ToString();
			if (tmp == SelectedFile)
				return;
			SelectedFile = tmp;
			//SelectedItemChanged.Raise (this, e);
		}
		void onFileSelect(object sender, MouseButtonEventArgs e){
			OkClicked.Raise (this, null);
			unloadDialog ((sender as GraphicObject).CurrentInterface);
		}
		void onCancel(object sender, MouseButtonEventArgs e){
			unloadDialog ((sender as GraphicObject).CurrentInterface);
		}
		void unloadDialog(Interface host){
			host.DeleteWidget (this);
		}
	}
}

