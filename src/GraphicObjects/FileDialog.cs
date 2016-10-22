﻿//
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

		#region CTOR
		public FileDialog () : base()
		{
		}
		#endregion
		//[DefaultValue(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments))]
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

//		public DirectoryInfo[] Directories
//		{
//			get {
//				//currentDir.GetDirectories
//				List<DirectoryInfo> tmp = currentDir.GetDirectories ().Where(fi => !fi.Attributes.HasFlag(FileAttributes.Hidden)).ToList();
//				if (currentDir.Parent != null)
//					tmp.Insert (0, currentDir.Parent);
//				return tmp.ToArray ();
//			}
//		}
//		public FileInfo[] Files
//		{
//			get {
//				string[] exts = searchPattern.Replace("*","").Split ('|');
//				//return currentDir.GetFiles (searchPattern).Where(fi => !fi.Attributes.HasFlag(FileAttributes.Hidden)).ToArray();
//				return currentDir.GetFiles().Where(f => exts.Any
//					(x => f.Name.EndsWith (x, StringComparison.InvariantCultureIgnoreCase))).ToArray();
//			}
//		}

//		void OnSelectedItemChanged (object sender, SelectionChangeEventArgs e)
//		{
//			currentDir = e.NewValue as DirectoryInfo;
//			NotifyValueChanged ("CurrentPath", CurrentPath);
//			NotifyValueChanged ("Directories", Directories);
//			NotifyValueChanged ("Files", Files);
//
//		}
//		void onFileListItemChanged (object sender, SelectionChangeEventArgs e)
//		{
//			selectedFile = e.NewValue as FileInfo;
//		}
//		void onFileSelect(object sender, MouseButtonEventArgs e){
//			//OpenTKGameWindow.currentWindow.DeleteWidget(window);
//		}
	}
//	public class DirContainer: IValueChange
//	{
//		#region IValueChange implementation
//		public event EventHandler<ValueChangeEventArgs> ValueChanged;
//		public void NotifyValueChanged(string name, object value)
//		{
//			ValueChanged.Raise (this, new ValueChangeEventArgs (name, value));
//		}
//		#endregion
//
//		public DirectoryInfo CurDir;
//		public DirContainer(DirectoryInfo _dir){
//			CurDir = _dir;
//		}
//		public string Name {
//			get { return CurDir.Name; }
//		}
//
//		void onDirUp(object sender, MouseButtonEventArgs e)
//		{
//
//		}
//		public void onMouseDown(object sender, MouseButtonEventArgs e)
//		{
//			Debug.WriteLine (sender.ToString ());
//		}
//
//	}
}

