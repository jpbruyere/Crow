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
using Crow.Coding;

namespace CrowIDE
{
	class CrowIDE : CrowWindow
	{
		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, CMDQuit,
		CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp,
		CMDAbout, CMDOptions,
		CMDViewGTExp, CMDViewProps, CMDViewProj, CMDViewProjProps,
		CMDCompile;

		void initCommands () {
			CMDNew = new Command(new Action(() => newFile())) { Caption = "New", Icon = new SvgPicture("#CrowIDE.ui.icons.blank-file.svg"), CanExecute = false};
			CMDOpen = new Command(new Action(() => openFileDialog())) { Caption = "Open...", Icon = new SvgPicture("#CrowIDE.ui.icons.outbox.svg")};
			CMDSave = new Command(new Action(() => saveFileDialog())) { Caption = "Save", Icon = new SvgPicture("#CrowIDE.ui.icons.inbox.svg"), CanExecute = false};
			CMDSaveAs = new Command(new Action(() => saveFileDialog())) { Caption = "Save As...", Icon = new SvgPicture("#CrowIDE.ui.icons.inbox.svg"), CanExecute = false};
			CMDQuit = new Command(new Action(() => Quit (null, null))) { Caption = "Quit", Icon = new SvgPicture("#CrowIDE.ui.icons.sign-out.svg")};
			CMDUndo = new Command(new Action(() => undo())) { Caption = "Undo", Icon = new SvgPicture("#CrowIDE.ui.icons.reply.svg"), CanExecute = false};
			CMDRedo = new Command(new Action(() => redo())) { Caption = "Redo", Icon = new SvgPicture("#CrowIDE.ui.icons.share-arrow.svg"), CanExecute = false};
			CMDCut = new Command(new Action(() => Quit (null, null))) { Caption = "Cut", Icon = new SvgPicture("#CrowIDE.ui.icons.scissors.svg"), CanExecute = false};
			CMDCopy = new Command(new Action(() => Quit (null, null))) { Caption = "Copy", Icon = new SvgPicture("#CrowIDE.ui.icons.copy-file.svg"), CanExecute = false};
			CMDPaste = new Command(new Action(() => Quit (null, null))) { Caption = "Paste", Icon = new SvgPicture("#CrowIDE.ui.icons.paste-on-document.svg"), CanExecute = false};
			CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("help"))) { Caption = "Help", Icon = new SvgPicture("#CrowIDE.ui.icons.question.svg")};
			CMDOptions = new Command(new Action(() => openOptionsDialog())) { Caption = "Editor Options", Icon = new SvgPicture("#CrowIDE.ui.icons.tools.svg")};

			CMDViewGTExp = new Command(new Action(() => loadWindow ("#CrowIDE.ui.GTreeExplorer.crow"))) { Caption = "Graphic Tree Explorer"};
			CMDViewProps = new Command(new Action(() => loadWindow ("#CrowIDE.ui.MemberView.crow"))) { Caption = "Properties View"};
			CMDCompile = new Command(new Action(() => compileSolution())) { Caption = "Compile"};
			CMDViewProj = new Command(new Action(() => loadWindow ("#CrowIDE.ui.CSProjExplorer.crow"))) { Caption = "Project Explorer"};
			CMDViewProjProps = new Command(new Action(loadProjProps) ){ Caption = "Project Properties"};
		}

		void openFileDialog () {			
			AddWidget (instFileDlg.CreateInstance()).DataSource = this;
		}
		void openOptionsDialog(){}
		void newFile() {}
		void saveFileDialog() {}
		void undo() {}
		void redo() {}


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

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			instFileDlg = Instantiator.CreateFromImlFragment
				(CurrentInterface, "<FileDialog Caption='Open File' CurrentDirectory='{²CurrentDirectory}' SearchPattern='*.sln' OkClicked='onFileOpen'/>");

			initCommands ();

			this.KeyDown += CrowIDE_KeyDown;

			//this.CrowInterface.LoadInterface ("#CrowIDE.ui.imlEditor.crow").DataSource = this;
			//GraphicObject go = this.CrowInterface.LoadInterface (@"ui/test.crow");
			GraphicObject go = AddWidget (@"#CrowIDE.ui.CrowIDE.crow");
			imlVE = go.FindByName ("crowContainer") as ImlVisualEditor;
			if (ReopenLastSolution && !string.IsNullOrEmpty(LastOpenSolution))
				CurrentSolution = Solution.LoadSolution (LastOpenSolution);

			go.DataSource = this;

		}

		void loadProjProps () {
			//loadWindow ("#CrowIDE.ui.ProjectProperties.crow", currentProject);
		}
		void compileSolution () {
			//ProjectItem pi = CurrentSolution.SelectedItem;
			Project p = CurrentSolution?.Projects[1];
			if (p == null)
				return;
			p.Compile ();
		}

		public string CurrentDirectory {
			get { return Crow.Configuration.Global.Get<string>("CurrentDirectory");}
			set {
				Crow.Configuration.Global.Set ("CurrentDirectory", value);
			}
		}
		public Solution CurrentSolution {
			get { return currentSolution; }
			set {
				if (currentSolution == value)
					return;
				currentSolution = value;
				NotifyValueChanged ("CurrentSolution", currentSolution);
			}
		}

		public string LastOpenSolution {
			get { return Crow.Configuration.Global.Get<string>("LastOpenSolution");}
			set {
				if (LastOpenSolution == value)
					return;
				Crow.Configuration.Global.Set ("LastOpenSolution", value);
				NotifyValueChanged ("LastOpenSolution", value);
			}
		}
		public bool ReopenLastSolution {
			get { return Crow.Configuration.Global.Get<bool>("ReopenLastSolution");}
			set {
				if (ReopenLastSolution == value)
					return;
				Crow.Configuration.Global.Set ("ReopenLastSolution", value);
				NotifyValueChanged ("ReopenLastSolution", value);
			}
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
			} else if (e.Key == OpenTK.Input.Key.F5) {
				try {
					CurrentSolution.StartupProject.Compile ();	
				} catch (Exception ex) {
					Console.WriteLine (ex.ToString ());
				}

			}// else if (e.Key == OpenTK.Input.Key.F6) {
//				loadWindow ("#CrowIDE.ui.LQIsExplorer.crow");
//			} else if (e.Key == OpenTK.Input.Key.F7) {
//				loadWindow ("#CrowIDE.ui.CSProjExplorer.crow");
//			}
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