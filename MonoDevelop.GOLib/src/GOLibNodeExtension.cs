// 
// ImageViewerNodeExtension.cs
//  
// Author:
//       Mike Krüger <mkrueger@novell.com>
// 
// Copyright (c) 2010 Novell, Inc (http://www.novell.com)
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
using MonoDevelop.Projects;
using MonoDevelop.Ide.Gui.Components;
using MonoDevelop.Components;
using MonoDevelop.Components.Commands;
using MonoDevelop.Ide;
using System.Linq;
using MonoDevelop.DesignerSupport;

namespace MonoDevelop.GOLib
{
	enum Commands {
		ShowGOLibViewer
	}
	
	class GOLibNodeExtension : NodeBuilderExtension
	{		
		public override Type CommandHandlerType {
			get { return typeof(GOLibCommandHandler); }
		}
		public override bool CanBuildNode (Type dataType)
		{			
			return typeof(ProjectFile).IsAssignableFrom (dataType);
		}
		public override void BuildNode (ITreeBuilder treeBuilder, object dataObject, NodeInfo nodeInfo)
		{
			ProjectFile pf  = dataObject as ProjectFile;

//			string mimeType = DesktopService.GetMimeTypeForUri (pf.FilePath);
//			if (mimeType.StartsWith ("image/", StringComparison.CurrentCultureIgnoreCase)) {
//				Image i;
//				if (pf != null) {				
//					i = Image.FromFile (pf.FilePath);
//					nodeInfo.Icon = i.Scale (16.0 / i.Width, 16.0 / i.Height);
//				}
//			}

			base.BuildNode (treeBuilder, dataObject, nodeInfo);
		} 
	}
	
	class GOLibCommandHandler: NodeCommandHandler //, IPropertyPadProvider
	{
		[CommandHandler (Commands.ShowGOLibViewer)]
		protected void OnShowGOLibViewer () 
		{

			GOLibView view = new GOLibView ();

			ProjectFile file   = CurrentNode.DataItem as ProjectFile;

			if (file != null)
				view.Load (file.FilePath);
			
			
			IdeApp.Workbench.OpenDocument (view, true);
			//IdeApp.Workbench.Documents.Where (d => d.FileName == file.FilePath);
		}

//		public override void ActivateItem ()
//		{
//			ProjectFile o = this.CurrentNode.DataItem as ProjectFile;
//
//			Ide.Gui.Document[] doc = IdeApp.Workbench.Documents.Where (d => d.FileName == o.FilePath).ToArray();
//			var tmp = MonoDevelop.Ide.Gui.DisplayBindingService.GetFileViewers (o.FilePath, o.Project).ToList();
//
//			OnShowGOLibViewer ();
//		}

//		#region IPropertyPadProvider implementation
//		public object GetActiveComponent ()
//		{
//			if (CurrentNodes.Length == 1)
//				return CurrentNode.DataItem;
//			else
//				return null;
//		}
//		public object GetProvider ()
//		{
//			return null;
//		}
//		public void OnEndEditing (object obj)
//		{
//			throw new NotImplementedException ();
//		}
//		public void OnChanged (object obj)
//		{
//			
//		}
//		#endregion
	}

//	class GOLibItemPropertyProvider : IPropertyProvider
//	{
//		#region IPropertyProvider implementation
//		public object CreateProvider (object obj)
//		{
//			var projectFile = obj as ProjectFile;
//			return projectFile;
//		}
//
//		public bool SupportsObject (object obj)
//		{
//			return obj is ProjectFile;
//		}
//		#endregion
//	}
}

