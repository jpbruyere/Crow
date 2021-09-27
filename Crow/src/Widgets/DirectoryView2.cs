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
	public enum DirectoryViewStyle {
		Icons,
		Detailed,
		Compact,
	}
	/// <summary>
	/// templated directory viewer
	/// </summary>
	public class DirectoryView2 : TemplatedGroup
	{
		#region CTOR
		protected DirectoryView2() {}
		public DirectoryView2 (Interface iface, string style = null) : base (iface, style) { }
		#endregion

		string currentDirectory = "/";
		bool showFiles, showHidden;
		string fileMask = "*.*";
		int iconSize;
		DirectoryViewStyle viewStyle;

		[DefaultValue(DirectoryViewStyle.Icons)]
		public virtual DirectoryViewStyle ViewStyle {
			get => viewStyle;
			set {
				if (viewStyle == value)
					return;
				viewStyle = value;
				NotifyValueChangedAuto (viewStyle);
				updateItemTemplates ();
			}
		}
		[DefaultValue(32)]
		public virtual int IconSize {
			get => iconSize;
			set {
				if (iconSize == value)
					return;
				iconSize = value;
				NotifyValueChangedAuto (iconSize);
				NotifyValueChanged ("IconSizeMeasure", new Measure(iconSize, Unit.Pixel));
				updateItemTemplates ();
			}
		}
		//public Measure IconSizeMeasure => new Measure(iconSize, Unit.Pixel);
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
		[DefaultValue(".")]
		public virtual string CurrentDirectory {
			get { return currentDirectory; }
			set {
				if (currentDirectory == value)
					return;
				currentDirectory = value;
				NotifyValueChangedAuto (currentDirectory);
				NotifyValueChanged ("FileSystemEntries", FileSystemEntries);
				updateFileSystemEntries();
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
		//set template and itemTemplates depending on view configuration
		void updateItemTemplates () {
			return;
			ItemTemplate = fileItemTemplates;
		}
		void updateFileSystemEntries () {

		}
		string fileItemTemplates => @"

<ItemTemplate DataType='System.IO.FileInfo'>
	<ListItem Fit='true'
				BubbleEvents='All'
				Selected = '{Background=${ControlHighlight}}'
				Unselected = '{Background=Transparent}'>
		<HorizontalStack>
			<Image Margin='2' Width='ICON_SIZE' Height='ICON_SIZE' Path='${FileIcon}'/>
			<Label Text='{Name}' />
		</HorizontalStack>
	</ListItem>
</ItemTemplate>
<ItemTemplate DataType='System.IO.DirectoryInfo'>
	<ListItem Fit='true'
				BubbleEvents='All'
				Selected = '{Background=${ControlHighlight}}'
				Unselected = '{Background=Transparent}'>
		<HorizontalStack>
			<Image Margin='2' Width='ICON_SIZE' Height='ICON_SIZE' Path='${FolderIcon}'/>
			<Label Text='{Name}' />
			<!--<Label Text='{LastAccessTime}' />-->
		</HorizontalStack>
	</ListItem>
</ItemTemplate>
".Replace ("ICON_SIZE", iconSize.ToString());
	}
}

