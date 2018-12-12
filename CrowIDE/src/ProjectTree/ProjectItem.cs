//
// ProjectNodes.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.IO;
using Crow;
using System.Threading;

namespace Crow.Coding
{	
	public class ProjectItem : ProjectNode {
		#region CTOR
		public ProjectItem() {}
		public ProjectItem (Project project, XmlNode _node) : base (project){
			node = _node;
		}
		#endregion

		public XmlNode node;

        public override Picture Icon {
            get {
                switch (Extension)
                {
                    case ".cs":
                        return new SvgPicture("#icons.cs-file.svg");
                    case ".crow":                        
                        return new SvgPicture("#icons.xml-file.svg");
                    case ".xml":
                        return new SvgPicture("#icons.xml-file.svg");
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
				return node.Attributes["Include"]?.Value.Replace('\\','/');
			}
		}
		public string AbsolutePath {
			get {
				return System.IO.Path.Combine (Project.RootDir, Path);
			}
		}
		public override ItemType Type {
			get { 
				return (ItemType)Enum.Parse (typeof(ItemType), node.Name, true);
			}
		}
		public override string DisplayName {
			get { 
				return Type == ItemType.Reference ?
					Path :
					Path.Split ('/').LastOrDefault();
			}
		}
		public string HintPath {
			get { return node.SelectSingleNode ("HintPath")?.InnerText; }
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

