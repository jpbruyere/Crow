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

namespace CrowIDE
{
	class CrowIDE : OpenTKGameWindow
	{
		public Command CMDLoad = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Open"))) { Caption = "Open", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
		public Command CMDSave = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Save"))) { Caption = "Save", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
		public Command CMDQuit;
//		public Command CMDSave = new Command(actionOpenFile) { Caption = "Open...", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
//		public Command CMDQuit = new Command(actionOpenFile) { Caption = "Open...", Icon = new SvgPicture("#Crow.Icons.open-file.svg")};
		public Command CMDCut = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Cut"))) { Caption = "Cut", Icon = new SvgPicture("#Crow.Icons.scissors.svg")};
		public Command CMDCopy = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Copy"))) { Caption = "Copy", Icon = new SvgPicture("#Crow.Icons.copy-file.svg")};
		public Command CMDPaste = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Paste"))) { Caption = "Paste", Icon = new SvgPicture("#Crow.Icons.paste-on-document.svg")};
		public Command CMDHelp = new Command(new Action(() => System.Diagnostics.Debug.WriteLine("Help"))) { Caption = "Help", Icon = new SvgPicture("#Crow.Icons.question.svg")};
		public Command CMDViewGTExp;
		public Command CMDViewProps;
		public Command CMDViewProj;

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

		protected override void OnLoad (EventArgs e)
		{
			base.OnLoad (e);

			CMDQuit = new Command(new Action(() => Quit (null, null))) { Caption = "Quit", Icon = new SvgPicture("#Crow.Icons.exit-symbol.svg")};
			CMDViewGTExp = new Command(new Action(() => loadWindow ("#CrowIDE.ui.GTreeExplorer.crow"))) { Caption = "Graphic Tree Explorer"};
			CMDViewProps = new Command(new Action(() => loadWindow ("#CrowIDE.ui.MemberView.crow"))) { Caption = "Properties View"};
			CMDViewProj = new Command(new Action(() => loadWindow ("#CrowIDE.ui.CSProjExplorer.crow"))) { Caption = "Project Explorer"};

			this.KeyDown += CrowIDE_KeyDown;

			//this.CrowInterface.LoadInterface ("#CrowIDE.ui.imlEditor.crow").DataSource = this;
			//GraphicObject go = this.CrowInterface.LoadInterface (@"ui/test.crow");
			GraphicObject go = this.CrowInterface.LoadInterface (@"#CrowIDE.ui.imlEditor.crow");
			imlVE = go.FindByName ("crowContainer") as ImlVisualEditor;
			go.DataSource = this;
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
		void loadWindow(string path){
			try {
				GraphicObject g = CrowInterface.FindByName (path);
				if (g != null)
					return;
				g = CrowInterface.LoadInterface (path);
				g.Name = path;
				g.DataSource = imlVE;
			} catch (Exception ex) {
				System.Diagnostics.Debug.WriteLine (ex.ToString ());
			}
		}
		void closeWindow (string path){
			GraphicObject g = CrowInterface.FindByName (path);
			if (g != null)
				CrowInterface.DeleteWidget (g);
		}

		protected void onCommandSave(object sender, MouseButtonEventArgs e){
			System.Diagnostics.Debug.WriteLine("save");
		}

		void actionOpenFile(){
			System.Diagnostics.Debug.WriteLine ("OpenFile action");
		}
	}
}