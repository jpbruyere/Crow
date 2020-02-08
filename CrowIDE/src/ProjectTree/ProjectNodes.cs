// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)


using System.Linq;

namespace Crow.Coding
{
	public enum ItemType {
		Unknown,
		ReferenceGroup,
		Reference,
		PackageReference,
		ProjectReference,
		VirtualGroup,
		Folder,
		None,
		Compile,
		EmbeddedResource,
	}
	public enum CopyToOutputState {
		Never,
		Always,
		PreserveNewest
	}
	public class ProjectNode  : TreeNode 
	{

		#region CTOR
		public ProjectNode () { }
		public ProjectNode (ProjectView project, ItemType _type, string _name) : this(project){			
			type = _type;
			name = _name;
		}
		public ProjectNode (ProjectView project){
			Project = project;
		}
		#endregion

		ItemType type;

		public ProjectView Project;

		//string iconSub = "";

		public virtual Picture Icon {
			get {
				switch (Type) {
				case ItemType.Reference:
					return CrowIDE.IcoReference;
				case ItemType.ProjectReference:
					return new SvgPicture("#Crow.Icons.projectRef.svg");
				case ItemType.PackageReference:
					return CrowIDE.IcoPackageReference;
				case ItemType.ReferenceGroup:
					return new SvgPicture ("#Icons.cubes.svg");
				case ItemType.VirtualGroup:
					return new SvgPicture ("#Icons.folder.svg");
				case ItemType.Folder:
					return new SvgPicture ("#Icons.folder.svg");
				default:
					return new SvgPicture("#Icons.blank-file.svg"); 
				}
			}
		}
		public string IconSub {
			get {
				switch (Type) {
				//case ItemType.ReferenceGroup:
				case ItemType.VirtualGroup:
				case ItemType.Folder:
					return IsExpanded.ToString();
				default:
					return null;
				}
			}
		
		}

		public virtual ItemType Type {
			get { return type; }
		}

		public override bool IsExpanded
		{
			get => base.IsExpanded;
			set
			{
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged ("IsExpanded", isExpanded);
				NotifyValueChanged ("IconSub", IconSub);
			}
		}

		public override string ToString () => $"{Type}: {DisplayName}";
	}
}

