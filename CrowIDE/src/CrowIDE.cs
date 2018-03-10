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
using System.Threading;

namespace Crow.Coding
{
	class CrowIDE : CrowWindow
	{
		public Command CMDNew, CMDOpen, CMDSave, CMDSaveAs, cmdCloseSolution, CMDQuit,
		CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste, CMDHelp,
		CMDAbout, CMDOptions,
		CMDViewGTExp, CMDViewProps, CMDViewProj, CMDViewProjProps, CMDViewErrors, CMDViewSolution, CMDViewEditor, CMDViewProperties,
		CMDViewToolbox,
		CMDCompile;

		void initCommands () {
			CMDNew = new Command(new Action(() => newFile())) { Caption = "New", Icon = new SvgPicture("#Crow.Coding.ui.icons.blank-file.svg"), CanExecute = false};
			CMDOpen = new Command(new Action(() => openFileDialog())) { Caption = "Open...", Icon = new SvgPicture("#Crow.Coding.ui.icons.outbox.svg")};
			CMDSave = new Command(new Action(() => saveFileDialog())) { Caption = "Save", Icon = new SvgPicture("#Crow.Coding.ui.icons.inbox.svg"), CanExecute = false};
			CMDSaveAs = new Command(new Action(() => saveFileDialog())) { Caption = "Save As...", Icon = new SvgPicture("#Crow.Coding.ui.icons.inbox.svg"), CanExecute = false};
			CMDQuit = new Command(new Action(() => Quit (null, null))) { Caption = "Quit", Icon = new SvgPicture("#Crow.Coding.ui.icons.sign-out.svg")};
			CMDUndo = new Command(new Action(() => undo())) { Caption = "Undo", Icon = new SvgPicture("#Crow.Coding.ui.icons.reply.svg"), CanExecute = false};
			CMDRedo = new Command(new Action(() => redo())) { Caption = "Redo", Icon = new SvgPicture("#Crow.Coding.ui.icons.share-arrow.svg"), CanExecute = false};
			CMDCut = new Command(new Action(() => Quit (null, null))) { Caption = "Cut", Icon = new SvgPicture("#Crow.Coding.ui.icons.scissors.svg"), CanExecute = false};
			CMDCopy = new Command(new Action(() => Quit (null, null))) { Caption = "Copy", Icon = new SvgPicture("#Crow.Coding.ui.icons.copy-file.svg"), CanExecute = false};
			CMDPaste = new Command(new Action(() => Quit (null, null))) { Caption = "Paste", Icon = new SvgPicture("#Crow.Coding.ui.icons.paste-on-document.svg"), CanExecute = false};
			CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("help"))) { Caption = "Help", Icon = new SvgPicture("#Crow.Coding.ui.icons.question.svg")};
			CMDOptions = new Command(new Action(() => openOptionsDialog())) { Caption = "Editor Options", Icon = new SvgPicture("#Crow.Coding.ui.icons.tools.svg")};

			cmdCloseSolution = new Command(new Action(() => closeSolution())) { Caption = "Close Solution", Icon = new SvgPicture("#Crow.Coding.ui.icons.paste-on-document.svg"), CanExecute = true};

			CMDViewErrors = new Command(new Action(() => loadDockWindow ("#Crow.Coding.ui.DockWindows.winErrors.crow")))
			{ Caption = "Errors pane"};
			CMDViewSolution = new Command(new Action(() => loadDockWindow ("#Crow.Coding.ui.DockWindows.winSolution.crow")))
			{ Caption = "Solution Tree"};
			CMDViewEditor = new Command(new Action(() => loadDockWindow ("#Crow.Coding.ui.DockWindows.winEditor.crow")))
			{ Caption = "Editor Pane"};
			CMDViewProperties = new Command(new Action(() => loadDockWindow ("#Crow.Coding.ui.DockWindows.winProperties.crow")))
			{ Caption = "Properties"};
			CMDViewToolbox = new Command(new Action(() => loadDockWindow ("#Crow.Coding.ui.DockWindows.winToolbox.crow")))
			{ Caption = "Toolbox"};

			CMDViewGTExp = new Command(new Action(() => loadDockWindow ("#Crow.Coding.ui.GTreeExplorer.crow"))) { Caption = "Graphic Tree Explorer"};
			CMDViewProps = new Command(new Action(() => loadWindow ("#Crow.Coding.ui.MemberView.crow"))) { Caption = "Properties View"};
			CMDCompile = new Command(new Action(() => compileSolution())) { Caption = "Compile"};
			CMDViewProj = new Command(new Action(() => loadWindow ("#Crow.Coding.ui.CSProjExplorer.crow"))) { Caption = "Project Explorer"};
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
		void closeSolution (){
			if (currentSolution != null)
				currentSolution.CloseSolution ();
			CurrentSolution = null;
		}

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

		Instantiator instFileDlg;
		Solution currentSolution;
		Docker mainDock;

		public static Interface MainIFace;

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			initCommands ();

			this.KeyDown += CrowIDE_KeyDown;

			//this.CrowInterface.LoadInterface ("#Crow.Coding.ui.imlEditor.crow").DataSource = this;
			//GraphicObject go = this.CrowInterface.LoadInterface (@"ui/test.crow");
			GraphicObject go = AddWidget (@"#Crow.Coding.ui.CrowIDE.crow");
			go.DataSource = this;

			MainIFace = ifaceControl[0].CrowInterface;

			mainDock = go.FindByName ("mainDock") as Docker;

			if (ReopenLastSolution && !string.IsNullOrEmpty (LastOpenSolution)) {
				CurrentSolution = Solution.LoadSolution (LastOpenSolution);
				//lock(MainIFace.UpdateMutex)
				CurrentSolution.ReopenItemsSavedInUserConfig ();
			}

			instFileDlg = Instantiator.CreateFromImlFragment
				(MainIFace, "<FileDialog Caption='Open File' CurrentDirectory='{²CurrentDirectory}' SearchPattern='*.sln' OkClicked='onFileOpen'/>");

			DockWindow dw = loadDockWindow ("#Crow.Coding.ui.DockWindows.winEditor.crow");
			dw.DockingPosition = Alignment.Center;
			dw.Dock (mainDock.Children [0] as DockStack);
			dw = loadDockWindow ("#Crow.Coding.ui.DockWindows.winSolution.crow");
			dw.DockingPosition = Alignment.Right;
			dw.Dock (mainDock.Children [0] as DockStack);
			dw = loadDockWindow ("#Crow.Coding.ui.DockWindows.winToolbox.crow");
			dw.DockingPosition = Alignment.Left;
			dw.Dock (mainDock.Children [0] as DockStack);

		}

		void loadProjProps () {
			//loadWindow ("#Crow.Coding.ui.ProjectProperties.crow", currentProject);
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
				lock (MainIFace) {
					NotifyValueChanged ("CurrentSolution", currentSolution);
				}
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
//				loadWindow ("#Crow.Coding.ui.LQIsExplorer.crow");
//			} else if (e.Key == OpenTK.Input.Key.F7) {
//				loadWindow ("#Crow.Coding.ui.CSProjExplorer.crow");
//			}
		}
		DockWindow loadDockWindow(string path){
			lock (MainIFace.UpdateMutex) {
				GraphicObject g = MainIFace.FindByName (path);
				if (g == null){
					try {					
							g = MainIFace.Load (path);
							mainDock.AddChild (g);
							g.Name = path;
							g.DataSource = CurrentSolution;
					} catch (Exception ex) {
						System.Diagnostics.Debug.WriteLine (ex.ToString ());
					}
				}
				return g as DockWindow;
			}
		}
		void loadWindow(string path, object dataSource = null){
			try {
				GraphicObject g = MainIFace.FindByName (path);
				if (g != null)
					return;
				g = MainIFace.AddWidget (path);
				g.Name = path;
				g.DataSource = dataSource;
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex.ToString ());
			}
		}
		void closeWindow (string path){
			GraphicObject g = MainIFace.FindByName (path);
			if (g != null)
				MainIFace.DeleteWidget (g);
		}

		protected void onCommandSave(object sender, MouseButtonEventArgs e){
			System.Diagnostics.Debug.WriteLine("save");
		}

		void actionOpenFile(){
			System.Diagnostics.Debug.WriteLine ("OpenFile action");
		}
	}
}