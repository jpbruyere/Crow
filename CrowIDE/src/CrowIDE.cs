//
//  HelloCube.cs
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
using OpenTK;
using OpenTK.Graphics.OpenGL;
using Crow;
using System.Reflection;
using System.Collections.Generic;
using System.Collections;
using System.Xml.Serialization;
using System.IO;
using Crow.IML;
using System.Xml;
using System.Linq;

namespace CrowIDE
{
	public enum ItemType {
		ReferenceGroup,
		Reference,
		ProjectReference,
		VirtualGroup,
		Folder,
		None,
		Compile,
		EmbeddedResource,
	}

	public class ProjectNode {
		ItemType type;
		string name;

		public Project Project;

		List<ProjectNode> childNodes = new List<ProjectNode>();

		public virtual ItemType Type {
			get { return type; }
		}
		public virtual string DisplayName {
			get { return name; }
		}
		public List<ProjectNode> ChildNodes {
			get { return childNodes;	}
		}

		public void SortChilds () {
			foreach (ProjectNode pn in childNodes)
				pn.SortChilds ();			
			childNodes = childNodes.OrderBy(c=>c.Type).ThenBy(cn=>cn.DisplayName).ToList();
		}

		#region CTOR
		public ProjectNode (Project project, ItemType _type, string _name) : this(project){			
			type = _type;
			name = _name;
		}
		public ProjectNode (Project project){
			Project = project;
		}
		#endregion
	}
	public class ProjectItem : ProjectNode {
		public XmlNode node;
		public string Path {
			get {
				return node.Attributes["Include"]?.Value.Replace('\\','/');
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
					Path.Split ('\\', '/').LastOrDefault();
			}
		}

		public ProjectItem (Project project, XmlNode _node) : base (project){
			node = _node;
		}
	}


	class CrowIDE : CrowWindow
	{
		public Command CMDSave = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Save"))) { Caption = "Save", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
//		public Command CMDSave = new Command(actionOpenFile) { Caption = "Open...", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
//		public Command CMDQuit = new Command(actionOpenFile) { Caption = "Open...", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
		public Command CMDCut = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Cut"))) { Caption = "Cut", Icon = new SvgPicture("#Crow.Icons.scissors.svg")};
		public Command CMDCopy = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Copy"))) { Caption = "Copy", Icon = new SvgPicture("#Crow.Icons.copy-file.svg")};
		public Command CMDPaste = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Paste"))) { Caption = "Paste", Icon = new SvgPicture("#Crow.Icons.paste-on-document.svg")};
		public Command CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Help"))) { Caption = "Help", Icon = new SvgPicture("#Crow.Icons.question.svg")};

		public Command CMDLoad, CMDQuit, CMDViewGTExp, CMDViewProps, CMDViewProj, CMDViewProjProps;

		[STAThread]
		static void Main ()
		{
			CrowIDE win = new CrowIDE ();
			win.Run (30);
		}

		public CrowIDE ()
			: base(1024, 800,"UIEditor")
		{
		}
		ImlVisualEditor imlVE;

		Instantiator instFileDlg;

		Solution currentSolution;
		Project currentProject;
		TreeView tv;

		public IList<Project> Projects { get {return null;}}

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			ReopenLastSolution = true;

			instFileDlg = Instantiator.CreateFromImlFragment
				("<FileDialog Caption='Open File' CurrentDirectory='{²CurrentDirectory}' SearchPattern='*.*' OkClicked='onFileOpen'/>");

			CMDLoad = new Command(new Action(()=>openFileDialog())) { Caption = "Open", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
			CMDQuit = new Command(new Action(() => Quit (null, null))) { Caption = "Quit", Icon = new SvgPicture("#Crow.Icons.exit-symbol.svg")};
			CMDViewGTExp = new Command(new Action(() => loadWindow ("#CrowIDE.ui.GTreeExplorer.crow"))) { Caption = "Graphic Tree Explorer"};
			CMDViewProps = new Command(new Action(() => loadWindow ("#CrowIDE.ui.MemberView.crow"))) { Caption = "Properties View"};
			CMDViewProj = new Command(new Action(() => loadWindow ("#CrowIDE.ui.CSProjExplorer.crow"))) { Caption = "Project Explorer"};
			CMDViewProjProps = new Command(new Action(loadProjProps) ){ Caption = "Project Properties"};
			this.KeyDown += CrowIDE_KeyDown;

			//this.CrowInterface.LoadInterface ("#CrowIDE.ui.imlEditor.crow").DataSource = this;
			//GraphicObject go = this.CrowInterface.LoadInterface (@"ui/test.crow");
			GraphicObject go = CurrentInterface.AddWidget (@"#CrowIDE.ui.imlEditor.crow");
			imlVE = go.FindByName ("crowContainer") as ImlVisualEditor;
			go.DataSource = this;

			if (ReopenLastSolution && !string.IsNullOrEmpty(LastOpenSolution))
				CurrentSolution = Solution.LoadSolution (LastOpenSolution);
		}

		void loadProjProps () {
			loadWindow ("#CrowIDE.ui.ProjectProperties.crow", currentProject);
		}

		public string CurrentDirectory {
			get { return Crow.Configuration.Get<string>("CurrentDirectory");}
			set {
				Crow.Configuration.Set ("CurrentDirectory", value);
			}
		}
		public Solution CurrentSolution {
			get { return CurrentSolution; }
			set {
				if (currentSolution == value)
					return;
				currentSolution = value;
				NotifyValueChanged ("CurrentSolution", currentSolution);
			}
		}
		public string LastOpenProject {
			get { return Crow.Configuration.Get<string>("LastOpenProject");}
			set {
				Crow.Configuration.Set ("LastOpenProject", value);
			}
		}
		public string LastOpenSolution {
			get { return Crow.Configuration.Get<string>("LastOpenSolution");}
			set {
				if (LastOpenSolution == value)
					return;
				Crow.Configuration.Set ("LastOpenSolution", value);
				NotifyValueChanged ("LastOpenSolution", value);
			}
		}
		public bool ReopenLastSolution {
			get { return Crow.Configuration.Get<bool>("ReopenLastSolution");}
			set {
				if (ReopenLastSolution == value)
					return;
				Crow.Configuration.Set ("ReopenLastSolution", value);
				NotifyValueChanged ("ReopenLastSolution", value);
			}
		}
		void openFileDialog () {			
			AddWidget (instFileDlg.CreateInstance(CurrentInterface)).DataSource = this;
		}

		public void onFileOpen (object sender, EventArgs e)
		{
			FileDialog fd = sender as FileDialog;

			string filePath = fd.SelectedFileFullPath;

			try {
				string ext = Path.GetExtension (filePath);
				if (string.Equals (ext, ".sln", StringComparison.InvariantCultureIgnoreCase)) {					
					CurrentSolution = Solution.LoadSolution (filePath);
					LastOpenSolution = filePath;
//				}else if (string.Equals (ext, ".csproj", StringComparison.InvariantCultureIgnoreCase)) {
//					currentProject = new Project (filePath);
				}
			} catch (Exception ex) {
				LoadIMLFragment ("<MessageBox Message='"+ ex.Message + "\n" + "' MsgType='Error'/>");	
			}				
		}

		void CrowIDE_KeyDown (object sender, OpenTK.Input.KeyboardKeyEventArgs e)
		{
			if (e.Key == OpenTK.Input.Key.Escape) {
				Quit (null, null);
				return;
			} else if (e.Key == OpenTK.Input.Key.F4) {
				loadWindow ("#CrowIDE.ui.MemberView.crow");
			} else if (e.Key == OpenTK.Input.Key.F5) {
				loadWindow ("#CrowIDE.ui.GTreeExplorer.crow");
			} else if (e.Key == OpenTK.Input.Key.F6) {
				loadWindow ("#CrowIDE.ui.LQIsExplorer.crow");
			} else if (e.Key == OpenTK.Input.Key.F7) {
				loadWindow ("#CrowIDE.ui.CSProjExplorer.crow");
			}
		}
		void loadWindow(string path, object dataSource = null){
			try {
				GraphicObject g = CurrentInterface.FindByName (path);
				if (g != null)
					return;
				g = CurrentInterface.AddWidget (path);
				g.Name = path;
				if (dataSource == null)
					g.DataSource = imlVE;
				else
					g.DataSource = dataSource;
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex.ToString ());
			}
		}
		void closeWindow (string path){
			GraphicObject g = CurrentInterface.FindByName (path);
			if (g != null)
				CurrentInterface.DeleteWidget (g);
		}

		protected void onCommandSave(object sender, MouseButtonEventArgs e){
			System.Diagnostics.Debug.WriteLine("save");
		}

		void actionOpenFile(){
			System.Diagnostics.Debug.WriteLine ("OpenFile action");
		}
	}
}