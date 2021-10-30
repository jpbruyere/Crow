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
				Data = FileSystemEntries;
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
			lock (IFace.UpdateMutex) {
				Template = dvTemplate;
				switch (ViewStyle) {
				case DirectoryViewStyle.Icons:
					ItemTemplate = iconViewItmp;
					break;
				case DirectoryViewStyle.Detailed:
					ItemTemplate = detailedViewITmp;
					break;
				case DirectoryViewStyle.Compact:
					ItemTemplate = compactViewITmp;
					break;
				}
			}
			RegisterForGraphicUpdate();
		}
		/// <summary>
		/// Default template in DirectoryView is set depending on 'ViewStyle'
		/// </summary>
		/// <param name="template"></param>
		protected override void loadTemplate (Widget template = null)
		{
			if (template == null)
				base.loadTemplate (IFace.CreateITorFromIMLFragment (dvTemplate).CreateInstance());
			else
				base.loadTemplate (template);
		}
		string compactViewITmp => $@"
<ItemTemplate DataType='System.IO.FileInfo'>
	<ListItem Fit='true'
				BubbleEvents='All'
				Selected = '{{Background=${{ControlHighlight}}}}'
				Unselected = '{{Background=Transparent}}'>
		<HorizontalStack Margin='1' >
			<Image Width='20' Height='20' Path='${{FileIcon}}'/>
			<Label Text='{{Name}}' />
		</HorizontalStack>
	</ListItem>
</ItemTemplate>
<ItemTemplate DataType='System.IO.DirectoryInfo'>
	<ListItem Fit='true'
				BubbleEvents='All'
				Selected = '{{Background=${{ControlHighlight}}}}'
				Unselected = '{{Background=Transparent}}'>
		<HorizontalStack Margin='1' >
			<Image Width='20' Height='20' Path='${{FolderIcon}}'/>
			<Label Text='{{Name}}' />
			<!--<Label Text='{{LastAccessTime}}' />-->
		</HorizontalStack>
	</ListItem>
</ItemTemplate>
";
		string iconViewItmp => $@"
<ItemTemplate DataType='System.IO.FileInfo'>
	<ListItem Width='70' Height='60'
				BubbleEvents='All'
				Selected = '{{Background=${{ControlHighlight}}}}'
				Unselected = '{{Background=Transparent}}'>
		<VerticalStack>
			<Image Margin='8' Width='Fit' Height='Stretched' Path='${{FileIcon}}' Scaled='true'/>
			<Label Text='{{Name}}' Background='Jet' Width='Stretched' TextAlignment='Center' Multiline='true' Font='sans,9' />
		</VerticalStack>
	</ListItem>
</ItemTemplate>
<ItemTemplate DataType='System.IO.DirectoryInfo'>
	<ListItem Width='90' Height='60'
				BubbleEvents='All'
				Selected = '{{Background=${{ControlHighlight}}}}'
				Unselected = '{{Background=Transparent}}'>
		<VerticalStack>
			<Image Margin='0' Width='Fit' Height='Stretched' Path='${{FolderIcon}}' Scaled='true'/>
			<Label Text='{{Name}}' Background='Jet' Width='Stretched' TextAlignment='Center' Multiline='true' Font='sans,9' />
		</VerticalStack>
	</ListItem>
</ItemTemplate>
";
		string detailedViewITmp => $@"
<ItemTemplate DataType='System.IO.DirectoryInfo'>
	<TableRow Width='Stretched' Height='Fit' Focusable='true'
				BubbleEvents='All' Tooltip='{{Name}}'
				Selected='{{Background=${{ControlHighlight}}}}'
				Unselected='{{Background=Transparent}}'>
		<Image Width='Stretched' Height='Stretched' Path='${{FolderIcon}}' Margin='0'  />
		<Label Text='{{Name}}' Width='Fit'  Font='sans,11' Margin='3'/>
		<Label Text='' Font='sans,9'/>
		<Label Text='{{LastAccessTime}}' Font='sans,9'/>
	</TableRow>
</ItemTemplate>
<ItemTemplate DataType='System.IO.FileInfo'>
	<TableRow Width='Stretched' Height='Fit' Focusable='true'
				BubbleEvents='All' Tooltip='{{Name}}'
				Selected='{{Background=${{ControlHighlight}}}}'
				Unselected='{{Background=Transparent}}'>
		<Image Width='Stretched' Height='Stretched' Path='${{FileIcon}}' Margin='2'  />
		<Label Text='{{Name}}' Width='Fit'  Font='sans,11' Margin='3'/>
		<Label Text='{{Length}}' Font='sans,9' TextAlignment='Right'/>
		<Label Text='{{LastAccessTime}}' Font='sans,9'/>
	</TableRow>
</ItemTemplate>
";

string dvTemplate => ViewStyle == DirectoryViewStyle.Compact ?
$@"
<VerticalStack Margin='5'>
	<Scroller Name='scroller1'>
		<Wrapper Orientation='Horizontal' Height='Stretched' Width='Fit' HorizontalAlignment='Left'
			Name='ItemsContainer' Margin='0' Spacing='2'/>
	</Scroller>
	<ScrollBar Style='HScrollBar'
		Value='{{²../scroller1.ScrollX}}' Maximum='{{../scroller1.MaxScrollX}}'
		CursorRatio='{{../scroller1.ChildWidthRatio}}'
		LargeIncrement='{{../scroller1.PageWidth}}' SmallIncrement='30' />
</VerticalStack>
"
: ViewStyle == DirectoryViewStyle.Detailed ?
$@"
<HorizontalStack Margin='5'>
	<Scroller Name='scroller1'>
		<Table Columns=',20;Name,Stretched;Size,100;Accessed,Fit' Height='Fit' Width='Stretched' VerticalAlignment='Top'
			Name='ItemsContainer' Margin='0' Spacing='0'  RowsMargin='0' ColumnSpacing='10'
			HorizontalLineWidth='0' VerticalLineWidth='1' />
	</Scroller>
	<ScrollBar
		Value='{{²../scroller1.ScrollY}}' Maximum='{{../scroller1.MaxScrollY}}'
		CursorRatio='{{../scroller1.ChildHeightRatio}}'
		LargeIncrement='{{../scroller1.PageHeight}}' SmallIncrement='30'/>
</HorizontalStack>
"
:
$@"
<HorizontalStack Margin='5'>
	<Scroller Name='scroller1'>
		<Wrapper Orientation='Vertical' Height='Fit' VerticalAlignment='Top'
			Name='ItemsContainer' Margin='0' Spacing='2'/>
	</Scroller>
	<ScrollBar
		Value='{{²../scroller1.ScrollY}}' Maximum='{{../scroller1.MaxScrollY}}'
		CursorRatio='{{../scroller1.ChildHeightRatio}}'
		LargeIncrement='{{../scroller1.PageHeight}}' SmallIncrement='30'/>
</HorizontalStack>
"
;
	}
}

