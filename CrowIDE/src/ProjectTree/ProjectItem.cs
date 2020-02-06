// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Linq;

namespace Crow.Coding
{
	public class ProjectItem : ProjectNode {
		#region CTOR
		public ProjectItem() {}
		public ProjectItem (ProjectView project, Microsoft.Build.Evaluation.ProjectItem _node) : base (project){
			node = _node;
		}
		#endregion

		public Microsoft.Build.Evaluation.ProjectItem node;

        public override Picture Icon {
            get {
                switch (Extension)
                {
                    case ".cs":
                        return new SvgPicture("#Icons.cs-file.svg");
                    case ".crow":                        
                        return new SvgPicture("#Icons.file-code.svg");
                    case ".xml":
                        return new SvgPicture("#Icons.file-code.svg");
                    default:
                        return base.Icon;
                }
            }
        }

        public string Extension {
			get { return System.IO.Path.GetExtension (Path); }
		}
		public string Path {
			get {
				return node.EvaluatedInclude?.Replace('\\','/');
			}
		}
		public string AbsolutePath {
			get {
				return System.IO.Path.Combine (Project.Path, Path);
			}
		}
		public override ItemType Type => Enum.TryParse (node.ItemType, true, out ItemType tmp) ? tmp : ItemType.Unknown;

		public override string DisplayName {
			get { 
				return Type == ItemType.Reference ?
					Path :
					Path.Split ('/').LastOrDefault();
			}
		}
		public string HintPath {
			get { return "HintPath?"; }
		}

		public override bool IsSelected {
			get {
				return isSelected;
			}
			set {
				if (value == isSelected)
					return;

				isSelected = value;

				NotifyValueChanged ("IsSelected", isSelected);

				if (isSelected) {
					Project.solution.SelectedItem = this;
					Project.IsExpanded = true;
					ProjectNode pn = Parent;
					while (pn != null) {
						pn.IsExpanded = true;
						pn = pn.Parent;
					}
				}
			}
		}
	}
}

