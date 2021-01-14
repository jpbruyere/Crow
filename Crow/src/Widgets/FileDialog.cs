// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.ComponentModel;
using System.IO;

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
		[DefaultValue("/home")]
		public virtual string CurrentDirectory {
			get { return curDir; }
			set {
				if (curDir == value)
					return;
				curDir = value;
				NotifyValueChangedAuto (curDir);
				SelectedDirectory = curDir;
			}
		}

		[DefaultValue("*")]
		public virtual string SearchPattern {
			get { return searchPattern; }
			set {
				if (searchPattern == value)
					return;
				searchPattern = value;
				NotifyValueChangedAuto (searchPattern);

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
			}
		}
		public string SelectedFile {
			get { return _selectedFile; }
			set {
				if (value == _selectedFile)
					return;
				_selectedFile = value;
				NotifyValueChangedAuto (_selectedFile);
			}
		}
		public string SelectedDirectory {
			get { return _selectedDir; }
			set {
				if (value == _selectedDir)
					return;
				_selectedDir = value;
				NotifyValueChangedAuto (_selectedDir);
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
			DirectoryInfo parentDir = Directory.GetParent (CurrentDirectory);
			if (parentDir == null)
				return;
			CurrentDirectory = parentDir.FullName;
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

