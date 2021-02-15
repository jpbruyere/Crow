// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow;
using System.IO;
using System.Text;
using Crow.IML;
using System.Runtime.CompilerServices;
using Glfw;
using System.Diagnostics;

namespace ShowCase
{
	class Showcase : SampleBase
	{
		static void Main ()
		{
			DbgLogger.IncludeEvents = DbgEvtType.Widget;
			DbgLogger.DiscardEvents = DbgEvtType.Focus;

			Environment.SetEnvironmentVariable ("FONTCONFIG_PATH", @"C:\Users\Jean-Philippe\source\vcpkg\installed\x64-windows\tools\fontconfig\fonts");

			using (Showcase app = new Showcase ()) 
				app.Run ();
		}

		public Container crowContainer;

		public string CurrentDir {
			get { return Configuration.Global.Get<string> (nameof (CurrentDir)); }
			set {
				if (CurrentDir == value)
					return;
				Configuration.Global.Set (nameof (CurrentDir), value);
				NotifyValueChanged (CurrentDir);
			}
		}

		string source, origSource;
		public string CurrentFile {
			get { return Configuration.Global.Get<string> (nameof (CurrentFile)); }
			set {
				if (CurrentFile == value)
					return;
				Configuration.Global.Set (nameof (CurrentFile), value);
				NotifyValueChanged (CurrentFile);
			}
		}
		
		public string Source {
			get => source;
			set {
				if (source == value)
					return;

				source = value;
				if (!reloadChrono.IsRunning)					
					reloadChrono.Restart ();
				
				CMDSave.CanExecute = source != origSource;
				NotifyValueChanged (source);
			}
		}


		public static Picture IcoNew = new SvgPicture ("#Icons.blank-file.svg");
		public static Picture IcoOpen = new SvgPicture ("#Icons.open.svg");
		public static Picture IcoSave = new SvgPicture ("#Icons.save.svg");
		public static Picture IcoSaveAs = new SvgPicture ("#Icons.save.svg");
		public static Picture IcoQuit = new SvgPicture ("#Icons.sign-out.svg");
		public static Picture IcoUndo = new SvgPicture ("#Icons.undo.svg");
		public static Picture IcoRedo = new SvgPicture ("#Icons.redo.svg");

		public static Picture IcoCut = new SvgPicture ("#Icons.scissors.svg");
		public static Picture IcoCopy = new SvgPicture ("#Icons.copy-file.svg");
		public static Picture IcoPaste = new SvgPicture ("#Icons.paste-on-document.svg");

		public Command CMDNew, CMDSave, CMDSaveAs, CMDUndo, CMDRedo, CMDCut, CMDCopy, CMDPaste;
		public CommandGroup ContextCommands => new CommandGroup (CMDNew, CMDSave, CMDSaveAs);
		void initCommands ()
		{
			CMDNew = new Command (new Action (onNewFile)) { Caption = "New", Icon = "#Icons.blank-file.svg", CanExecute = true };
			CMDSave = new Command (new Action (onSave)) { Caption = "Save", Icon = "#Icons.save.svg", CanExecute = false };
			CMDSaveAs = new Command (new Action (onSaveAs)) { Caption = "Save As...", Icon = "#Icons.save.svg", CanExecute = true };
			/*CMDUndo = new Command (new Action (undo)) { Caption = "Undo", Icon = IcoUndo, CanExecute = false };
			CMDRedo = new Command (new Action (redo)) { Caption = "Redo", Icon = IcoRedo, CanExecute = false };
			CMDCut = new Command (new Action (cut)) { Caption = "Cut", Icon = IcoCut, CanExecute = false };
			CMDCopy = new Command (new Action (copy)) { Caption = "Copy", Icon = IcoCopy, CanExecute = false };
			CMDPaste = new Command (new Action (paste)) { Caption = "Paste", Icon = IcoPaste, CanExecute = false };*/
		}

		public new bool IsDirty {
			get => !string.Equals(origSource, source);
		}
		void onSave ()
		{
			if (!File.Exists (CurrentFile)) {
				onSaveAs ();
				return;
			}
			save ();
		}
		void onSaveAs ()
		{
			string dir = Path.GetDirectoryName (CurrentFile);
			if (string.IsNullOrEmpty (dir))
				dir = Directory.GetCurrentDirectory ();
			LoadIMLFragment (@"<FileDialog Width='60%' Height='50%' Caption='Save as ...' CurrentDirectory='" +
				dir + "' SelectedFile='" +
				Path.GetFileName(CurrentFile) + "' OkClicked='saveFileDialog_OkClicked'/>").DataSource = this;
		}
		void saveFileDialog_OkClicked (object sender, EventArgs e)
		{
			FileDialog fd = sender as FileDialog;

			if (string.IsNullOrEmpty (fd.SelectedFileFullPath))
				return;

			if (File.Exists(fd.SelectedFileFullPath)) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "File exists, overwrite?");
				mb.Yes += (sender2, e2) => {
					CurrentFile = fd.SelectedFileFullPath;
					save ();
				};
				return;
			}

			CurrentFile = fd.SelectedFileFullPath;
			save ();
		}

		void onNewFile () {
			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?");
				mb.Yes += (sender, e) => newFile ();
			} else
				newFile ();
		}
		void newFile()
		{
			origSource = "";
			Source = "<Widget Background='DarkGrey'/>";
			if (!string.IsNullOrEmpty (CurrentFile))
				CurrentFile = Path.Combine (Path.GetDirectoryName (CurrentFile), "newfile.crow");
			else
				CurrentFile = Path.Combine (CurrentDir, "newfile.crow");
		}


		void save () {
			using (Stream s = new FileStream(CurrentFile, FileMode.Create)) {
				s.WriteByte (0xEF);
				s.WriteByte (0xBB);
				s.WriteByte (0xBF);
				byte [] buff = Encoding.UTF8.GetBytes (source);
				s.Write (buff, 0, buff.Length);
			}
			origSource = source;
		}

		protected override void OnInitialized ()
		{
			initCommands ();

			base.OnInitialized ();

			if (string.IsNullOrEmpty (CurrentDir))
				CurrentDir = Path.Combine (Directory.GetCurrentDirectory (), "Interfaces");

			Widget g = Load ("#ShowCase.showcase.crow");
			crowContainer = g.FindByName ("CrowContainer") as Container;
			g.DataSource = this;

			if (!File.Exists(CurrentFile))
				origSource = Source = @"<Label Text='Hello World' Background='MediumSeaGreen' Margin='10'/>";

			//I set an empty object as datasource at this level to force update when new
			//widgets are added to the interface
			crowContainer.DataSource = new object ();
			hideError ();

			reloadFromFile ();
		}


		public void goUpDirClick (object sender, MouseButtonEventArgs e)
		{
			string root = Directory.GetDirectoryRoot (CurrentDir);
			if (CurrentDir == root)
				return;
			CurrentDir = Directory.GetParent (CurrentDir).FullName;
		}

		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;

			if (IsDirty) {
				MessageBox mb = MessageBox.ShowModal (this, MessageBox.Type.YesNo, "Current file has unsaved changes, are you sure?");
				mb.Yes += (mbsender, mbe) => { CurrentFile = fi.FullName; reloadFromFile (); };
				return;
			}

			CurrentFile = fi.FullName;
			reloadFromFile ();
		}

		void showError (Exception ex)
		{
			NotifyValueChanged ("ErrorMessage", (object)ex.Message);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError ()
		{
			NotifyValueChanged ("ShowError", false);
		}
		void reloadFromFile ()
		{
			if (File.Exists (CurrentFile)) {
				using (Stream s = new FileStream (CurrentFile, FileMode.Open)) {
					using (StreamReader sr = new StreamReader (s))
						origSource = sr.ReadToEnd ();
				}
				Source = origSource;
			}
		}
		void reloadFromSource ()
		{
			hideError ();
			Widget g = null;
			try {
				lock (UpdateMutex) {
					Instantiator inst = null;
					using (MemoryStream ms = new MemoryStream (Encoding.UTF8.GetBytes (source))) 
						inst = new Instantiator (this, ms);
					g = inst.CreateInstance ();
					crowContainer.SetChild (g);
					g.DataSource = this;
				}
			} catch (InstantiatorException itorex) {
				//Console.WriteLine (itorex);
				showError (itorex.InnerException);
			} catch (Exception ex) {
				//Console.WriteLine (ex);
				showError (ex);
			}
		}
		Stopwatch reloadChrono = Stopwatch.StartNew ();
        public override void UpdateFrame () {
            base.UpdateFrame ();
			if (reloadChrono.ElapsedMilliseconds < 200)
				return;
			reloadFromSource ();
			reloadChrono.Reset ();
		}

    }
}