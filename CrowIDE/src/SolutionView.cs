//
// Solution.cs
//
//code taken in project https://sourceforge.net/projects/syncproj/
// no licence info was included, I took the liberty to modify it.
// Author:
//		tarmopikaro
//      2018 Jean-Philippe Bruyère
//MIT-licenced

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace Crow.Coding
{
	public class StyleItemContainer {
		public object Value;
		public string Name;
		public StyleItemContainer(string name, object _value){
			Name = name;
			Value = _value;
		}
	}
	public class StyleContainer : IValueChange {
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		Style style;
		bool isExpanded;

		public string Name;
		public List<StyleItemContainer> Items;
		public bool IsExpanded
		{
			get { return isExpanded; }
			set
			{
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged ("IsExpanded", isExpanded);
			}
		}
		public StyleContainer(string name, Style _style){
			Name = name;
			style = _style;

			Items = new List<StyleItemContainer> ();
			foreach (string k in style.Keys) {
				Items.Add(new StyleItemContainer(k, style[k]));
			}
		}
	}

	/// <summary>
	/// .sln loaded into class.
	/// </summary>
	public class SolutionView: IValueChange
	{
		/*static SolutionView ()
		{
			var nativeSharedMethod = typeof (SolutionFile).Assembly.GetType ("Microsoft.Build.Shared.NativeMethodsShared");
			var isMonoField = nativeSharedMethod.GetField ("_isMono", BindingFlags.Static | BindingFlags.NonPublic);
			isMonoField.SetValue (null, true);

			Environment.SetEnvironmentVariable ("MSBUILD_EXE_PATH", "/usr/share/dotnet/sdk/3.1.101/MSBuild.dll");
		}*/
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		string path;
		SolutionFile solutionFile;
		public ProjectCollection projectCollection;
		public BuildParameters buildParams;
		public Dictionary<String, String> globalProperties = new Dictionary<String, String> ();

		public List<ProjectView> Projects = new List<ProjectView> ();

		public Configuration UserConfig;

		ProjectItem selectedItem = null;
		object selectedItemElement = null;
		ObservableList<ProjectItem> openedItems = new ObservableList<ProjectItem>();
		ObservableList<GraphicObjectDesignContainer> toolboxItems;

		public Dictionary<string, Style> Styling;
		public Dictionary<string, string> DefaultTemplates;

		public List<Style> Styles { get { return Styling.Values.ToList(); }}
		public List<StyleContainer> StylingContainers;
		//TODO: check project dependencies if no startup proj
				
		public ObservableList<BuildEventArgs> BuildEvents = new ObservableList<BuildEventArgs> ();

		public LoggerVerbosity MainLoggerVerbosity {
			get => buildParams == null ? LoggerVerbosity.Normal : buildParams.Loggers.First().Verbosity;
			set {
				if (MainLoggerVerbosity == value)
					return;
				if (buildParams != null)
					buildParams.Loggers.First ().Verbosity = value;
				NotifyValueChanged ("MainLoggerVerbosity", MainLoggerVerbosity);
			}
		}

		public void ReloadStyling () {
			Styling = new Dictionary<string, Style> ();
			if (StartupProject != null)
				StartupProject.GetStyling ();
			StylingContainers = new List<StyleContainer> ();
			foreach (string k in Styling.Keys) 
				StylingContainers.Add (new StyleContainer (k, Styling [k]));
			foreach (ImlProjectItem pf in openedItems.OfType<ImlProjectItem>()) {
				pf.SignalEditorOfType<ImlVisualEditor> ();
			}
		}
		public string[] AvailaibleStyles {
			get { return Styling == null ? new string[] {} : Styling.Keys.ToArray();}
		}
		public void ReloadDefaultTemplates () {
			DefaultTemplates = new Dictionary<string, string>();
			if (StartupProject != null)
				StartupProject.GetDefaultTemplates ();
		}
		public void updateToolboxItems () {
			Type[] crowItems = AppDomain.CurrentDomain.GetAssemblies ()
				.SelectMany (t => t.GetTypes ())
				.Where (t => t.IsClass && !t.IsAbstract && t.IsPublic &&					
					t.Namespace == "Crow" && t.IsSubclassOf(typeof(Widget)) &&
					t.GetCustomAttribute<DesignIgnore>(false) == null).ToArray ();
			ToolboxItems = new ObservableList<GraphicObjectDesignContainer> ();
			foreach (Type ci in crowItems) {
				toolboxItems.Add(new GraphicObjectDesignContainer(ci));
			}
		}
		public bool GetProjectFileFromPath (string path, out ProjectFile pi){
			pi = null;
			return false;/* StartupProject == null ? false :
				StartupProject.TryGetProjectFileFromPath (path, out pi);*/
		}

		public ObservableList<ProjectItem> OpenedItems {
			get { return openedItems; }
			set {
				if (openedItems == value)
					return;
				openedItems = value;
				NotifyValueChanged ("OpenedItems", openedItems);
			}
		}

		public ObservableList<GraphicObjectDesignContainer> ToolboxItems {
			get { return toolboxItems; }
			set {
				if (toolboxItems == value)
					return;
				toolboxItems = value;
				NotifyValueChanged ("ToolboxItems", toolboxItems);
			}			
		}
		public ProjectItem SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem == value)
					return;
				if (SelectedItem != null)
					SelectedItem.IsSelected = false;
				selectedItem = value;
				if (SelectedItem != null) 
					SelectedItem.IsSelected = true;

				//UserConfig.Set ("SelectedProjItems", SelectedItem?.AbsolutePath);

				NotifyValueChanged ("SelectedItem", selectedItem);
			}
		}
		public object SelectedItemElement {
			get { return selectedItemElement; }
			set {
				if (selectedItemElement == value)
					return;
				selectedItemElement = value;
				NotifyValueChanged ("SelectedItemElement", selectedItemElement);
			}
		}
		public string DisplayName => Path.GetFileNameWithoutExtension (path);

//		public System.CodeDom.Compiler.CompilerErrorCollection CompilationErrors {
//			get {
//				System.CodeDom.Compiler.CompilerErrorCollection tmp = Projects.SelectMany<Project>
//					(p => p.CompilationResults.Errors);
//				return tmp;
//			}
//		}
		public List<System.CodeDom.Compiler.CompilerError> CompilerErrors {
			get {
				int errCount = 0;
				for (int i = 0; i < Projects.Count; i++) {
					if (Projects [i].CompilationResults != null)
						errCount += Projects [i].CompilationResults.Errors.Count;
				}
				System.CodeDom.Compiler.CompilerError[] tmp = new System.CodeDom.Compiler.CompilerError[errCount];

				int ptr = 0;
				for (int i = 0; i < Projects.Count; i++) {
					if (Projects [i].CompilationResults == null)
						continue;
					Projects [i].CompilationResults.Errors.CopyTo (tmp,ptr);
					ptr += Projects [i].CompilationResults.Errors.Count;
				}
				return new List<System.CodeDom.Compiler.CompilerError>(tmp);
			}
		}

		public void UpdateErrorList () {
			NotifyValueChanged ("CompilerErrors", CompilerErrors);
		}

		void saveOpenedItemsInUserConfig (){
			if (openedItems.Count == 0)
				UserConfig.Set ("OpenedItems", "");
			else
				UserConfig.Set ("OpenedItems", openedItems.Select(o => o.AbsolutePath).Aggregate((a,b)=>a + ";" + b));
		}
		public void ReopenItemsSavedInUserConfig () {
			string tmp = UserConfig.Get<string> ("OpenedItems");
			string sel = UserConfig.Get<string> ("SelectedProjItems");
			ProjectFile selItem = null;
			if (string.IsNullOrEmpty (tmp))
				return;
			foreach (string f in tmp.Split(';')) {
				foreach (ProjectView p in Projects) {
					ProjectFile pi;
					/*if (p.TryGetProjectFileFromAbsolutePath (f, out pi)) {
						pi.Open ();
						if (pi.AbsolutePath == sel)
							selItem = pi;
						break;
					}*/
				}
			}
			if (selItem == null)
				return;
			selItem.IsSelected = true;
		}

		/*void onSelectedItemChanged (object sender, SelectionChangeEventArgs e){							
			SelectedItem = e.NewValue as ProjectItem;
			UserConfig.Set ("SelectedProjItems", SelectedItem?.AbsolutePath);
		}*/

		public void OpenItem (ProjectItem pi) {
			if (!openedItems.Contains (pi)) {
				openedItems.Add (pi);
				saveOpenedItemsInUserConfig ();
			}
		}
		public void CloseItem (ProjectItem pi) {			
			openedItems.Remove (pi);
			saveOpenedItemsInUserConfig ();
		}

		public void CloseSolution () {
			while (openedItems.Count > 0) {
				openedItems.Remove (openedItems [0]);
			}
			while (toolboxItems?.Count > 0) {
				toolboxItems.Remove (toolboxItems [0]);
			}
			NotifyValueChanged ("Projects", null);
		}

		public ProjectView StartupProject {
			get { return null; }// Projects?.FirstOrDefault (p => p.ProjectGuid == UserConfig.Get<string> ("StartupProject")); }
			set {
				if (value == StartupProject)
					return;
				if (value == null)
					UserConfig.Set ("StartupProject", "");
				else {
					UserConfig.Set ("StartupProject", value.ProjectGuid);
					value.NotifyValueChanged("IsStartupProject", true);
				}
				NotifyValueChanged ("StartupProject", StartupProject);
				ReloadStyling ();
				ReloadDefaultTemplates ();
			}
		}

		public string ActiveConfiguration {
			get => globalProperties.TryGetValue ("Configuration", out string conf) ? conf : "";
			set {
				if (globalProperties.TryGetValue ("Configuration", out string conf) &&  conf == value)
					return;
				globalProperties ["Configuration"] = value;
				NotifyValueChanged ("ActiveConfiguration", value);
			}
		}
		public string ActivePlatform {
			get => globalProperties.TryGetValue ("Platform", out string conf) ? conf : "";
			set {
				if (globalProperties.TryGetValue ("Platform", out string conf) && conf == value)
					return;
				globalProperties ["Platform"] = value;
				NotifyValueChanged ("ActivePlatform", value);
			}
		}

		public void Build (params string[] targets)
		{
			BuildRequestData buildRequest = new BuildRequestData (path, globalProperties, CrowIDE.toolsVersion, targets, null);
			BuildResult buildResult = BuildManager.DefaultBuildManager.Build (buildParams, buildRequest);
		}
		#region CTOR
		/// <summary>
		/// Creates new solution.
		/// </summary>
		public SolutionView (string path)
		{
			projectCollection = new ProjectCollection ();
			projectCollection.DefaultToolsVersion = CrowIDE.toolsVersion;
			buildParams = new BuildParameters (projectCollection);
			buildParams.Loggers = new List<ILogger> () { new IdeLogger (this)};
			buildParams.ResetCaches = true;
			buildParams.LogInitialPropertiesAndItems = true;
			//projectCollection.IsBuildEnabled = false;

			BuildManager.DefaultBuildManager.ResetCaches ();

			this.path = path;
			solutionFile = SolutionFile.Parse (path);
			UserConfig = new Configuration (path + ".user");

			globalProperties ["SolutionDir"] = Path.GetDirectoryName (path) + "/";
			//globalProperties ["OutputPath"] = "build/";
			//globalProperties ["IntermediateOutputPath"] = "build/obj/";
			globalProperties ["RestoreConfigFile"] = "/home/jp/.nuget/NuGet/NuGet.Config";
			ActiveConfiguration = solutionFile.GetDefaultConfigurationName ();
			ActivePlatform = solutionFile.GetDefaultPlatformName ();

			//solutionFile.SolutionConfigurations;
			//added to be able to compile with net472
#if NET472
			globalProperties ["RoslynTargetsPath"] = Path.Combine (CrowIDE.msbuildRoot, "Roslyn/");
			globalProperties ["MSBuildSDKsPath"] = Path.Combine (CrowIDE.msbuildRoot, "Sdks/");
#endif
			//------------

			foreach (ProjectInSolution pis in solutionFile.ProjectsInOrder) {
				switch (pis.ProjectType) {
				case SolutionProjectType.Unknown:
					break;
				case SolutionProjectType.KnownToBeMSBuildFormat:
					Projects.Add (new ProjectView (this, pis));
					break;
				case SolutionProjectType.SolutionFolder:
					break;
				case SolutionProjectType.WebProject:
					break;
				case SolutionProjectType.WebDeploymentProject:
					break;
				case SolutionProjectType.EtpSubProject:
					break;
				case SolutionProjectType.SharedProject:
					break;
				}
			}
		}
		#endregion
	}
}
