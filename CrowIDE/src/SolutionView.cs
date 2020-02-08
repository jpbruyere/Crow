// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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
	public class SolutionView: IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		public readonly CrowIDE IDE;

		string path;
		SolutionFile solutionFile;

		public BuildParameters buildParams;
		public Dictionary<String, String> projectProperties = new Dictionary<String, String> ();

		public List<ProjectView> Projects = new List<ProjectView> ();

		public Configuration UserConfig;

		ProjectItemNode selectedItem = null;
		object selectedItemElement = null;
		ObservableList<ProjectItemNode> openedItems = new ObservableList<ProjectItemNode>();
		ObservableList<GraphicObjectDesignContainer> toolboxItems;

		public string Directory => Path.GetDirectoryName (path);

		public Dictionary<string, Style> Styling;
		public Dictionary<string, string> DefaultTemplates;

		public List<Style> Styles { get { return Styling.Values.ToList(); }}
		public List<StyleContainer> StylingContainers;
		//TODO: check project dependencies if no startup proj

		public IEnumerable<string> Configurations => solutionFile.SolutionConfigurations.Select (sc => sc.ConfigurationName).Distinct ().ToList ();
		public IEnumerable<string> Platforms => solutionFile.SolutionConfigurations.Select (sc => sc.PlatformName).Distinct ().ToList ();

		public void Build (params string [] targets)
		{
			BuildRequestData buildRequest = new BuildRequestData (path, projectProperties, CrowIDE.DEFAULT_TOOLS_VERSION, targets, null);
			BuildResult buildResult = BuildManager.DefaultBuildManager.Build (buildParams, buildRequest);
		}
		#region CTOR
		/// <summary>
		/// Creates new solution.
		/// </summary>
		public SolutionView (CrowIDE ide, string path)
		{
			this.IDE = ide;
			this.path = path;
			solutionFile = SolutionFile.Parse (path);
			UserConfig = new Configuration (path + ".user");

			ActiveConfiguration = solutionFile.GetDefaultConfigurationName ();
			ActivePlatform = solutionFile.GetDefaultPlatformName ();

			ide.projectCollection.SetGlobalProperty ("SolutionDir", Path.GetDirectoryName (path) + "/");

			buildParams = new BuildParameters (ide.projectCollection) {
				Loggers = ide.projectCollection.Loggers,
				ResetCaches = true,
				LogInitialPropertiesAndItems = true
			};
			//projectCollection.IsBuildEnabled = false;

			BuildManager.DefaultBuildManager.ResetCaches ();

#if NET472
			ide.projectCollection.SetGlobalProperty ("RoslynTargetsPath", Path.Combine (Startup.msbuildRoot, "Roslyn/"));
			ide.projectCollection.SetGlobalProperty ("MSBuildSDKsPath", Path.Combine (Startup.msbuildRoot, "Sdks/"));
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

			ReloadStyling ();
			ReloadDefaultTemplates ();

		}
		#endregion

		public void ReloadStyling () {
			Console.WriteLine ("reload styling");

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
		public bool GetProjectFileFromPath (string path, out ProjectFileNode pi){
			pi = null;
			return false;/* StartupProject == null ? false :
				StartupProject.TryGetProjectFileFromPath (path, out pi);*/
		}

		public ObservableList<ProjectItemNode> OpenedItems {
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
		public ProjectItemNode SelectedItem {
			get { return selectedItem; }
			set {
				if (selectedItem == value)
					return;
				if (SelectedItem != null)
					SelectedItem.IsSelected = false;
				selectedItem = value;
				if (SelectedItem != null) {
					SelectedItem.IsSelected = true;
					UserConfig.Set ("SelectedProjItems", SelectedItem.SaveID);
				}

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
				UserConfig.Set ("OpenedItems", openedItems.Select(o => o.SaveID).Aggregate((a,b)=>$"{a};{b}"));
		}
		public void ReopenItemsSavedInUserConfig () {
			string tmp = UserConfig.Get<string> ("OpenedItems");
			if (string.IsNullOrEmpty (tmp))
				return;
			string sel = UserConfig.Get<string> ("SelectedProjItems");
			ProjectFileNode selItem = null;
			foreach (string f in tmp.Split(';')) {
				string [] s = f.Split ('|');
				ProjectFileNode pi = Projects.FirstOrDefault (p => p.DisplayName == s [0])?.Flatten.OfType<ProjectFileNode>().FirstOrDefault(pfn=>pfn.RelativePath == s[1]);
				if (pi == null)
					continue;
				pi.Open ();
				if (pi.SaveID == sel)
					selItem = pi;
			}
			if (selItem == null)
				return;
			selItem.IsSelected = true;
		}

		/*void onSelectedItemChanged (object sender, SelectionChangeEventArgs e){							
			SelectedItem = e.NewValue as ProjectItem;
			UserConfig.Set ("SelectedProjItems", SelectedItem?.AbsolutePath);
		}*/

		public void OpenItem (ProjectItemNode pi) {
			if (!openedItems.Contains (pi)) {
				openedItems.Add (pi);
				saveOpenedItemsInUserConfig ();
			}
		}
		public void CloseItem (ProjectItemNode pi) {			
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

			IDE.projectCollection.UnloadAllProjects ();
		}

		public ProjectView StartupProject {
			get => Projects.FirstOrDefault (p => p.FullPath == UserConfig.Get<string> ("StartupProject")); 
			set {
				if (value == StartupProject)
					return;

				StartupProject?.NotifyValueChanged ("IsStartupProject", false);

				if (value == null)
					UserConfig.Set ("StartupProject", "");
				else {
					UserConfig.Set ("StartupProject", value.FullPath);
					value.NotifyValueChanged("IsStartupProject", true);
				}
				NotifyValueChanged ("StartupProject", StartupProject);
				ReloadStyling ();
				ReloadDefaultTemplates ();
			}
		}

		public string ActiveConfiguration {
			get => projectProperties.TryGetValue ("Configuration", out string conf) ? conf : null;
			set {
				if (projectProperties.TryGetValue ("Configuration", out string conf) &&  conf == value)
					return;
				projectProperties ["Configuration"] = value;
				NotifyValueChanged ("ActiveConfiguration", value);
			}
		}
		public string ActivePlatform {
			get => projectProperties.TryGetValue ("Platform", out string conf) ? conf : null;
			set {
				if (projectProperties.TryGetValue ("Platform", out string conf) && conf == value)
					return;
				projectProperties ["Platform"] = value;
				NotifyValueChanged ("ActivePlatform", value);
			}

		}

		void onSelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			TreeNode n = e.NewValue as TreeNode;
		}

	}
}
