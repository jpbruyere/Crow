﻿//
// Solution.cs
//
//code taken in project https://sourceforge.net/projects/syncproj/
// no licence info was included, I took the liberty to modify it.
// Author:
//		tarmopikaro
//      2018 Jean-Philippe Bruyère
//MIT-licenced

using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Serialization;
using Crow;

namespace Crow.Coding{	
	public class SolutionProject {
		public string ProjectHostGuid;
		public string ProjectName;
		public string RelativePath;
		public string ProjectGuid;
	}
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
	public class Solution: IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		ProjectItem selectedItem = null;
		object selectedItemElement = null;
		ObservableList<ProjectItem> openedItems = new ObservableList<ProjectItem>();
		ObservableList<GraphicObjectDesignContainer> toolboxItems;

		public Dictionary<string, Style> Styling;
		public Dictionary<string, string> DefaultTemplates;

		public List<Style> Styles { get { return Styling.Values.ToList(); }}
		public List<StyleContainer> StylingContainers;
		//TODO: check project dependencies if no startup proj

		public void ReloadStyling () {
			Styling = new Dictionary<string, Style> ();
			if (StartupProject != null)
				StartupProject.GetStyling ();
//			StylingContainers = new List<StyleContainer> ();
//			foreach (string k in Styling.Keys) {
//				StylingContainers.Add (new StyleContainer (k, Styling [k]));
//			}
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
				toolboxItems.AddElement(new GraphicObjectDesignContainer(ci));
			}
		}
		public bool GetProjectFileFromPath (string path, out ProjectFile pi){
			pi = null;
			return StartupProject == null ? false :
				StartupProject.TryGetProjectFileFromPath (path, out pi);
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

				UserConfig.Set ("SelectedProjItems", SelectedItem?.AbsolutePath);

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
		public string DisplayName {
			get { return name; }
		}
		/// <summary>
		/// Gets solution path
		/// </summary>
		public String SolutionFolder
		{
			get { return Path.GetDirectoryName (path); }
		}

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
				foreach (Project p in Projects) {
					ProjectFile pi;
					if (p.TryGetProjectFileFromAbsolutePath (f, out pi)) {
						pi.Open ();
						if (pi.AbsolutePath == sel)
							selItem = pi;
						break;
					}
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
				openedItems.AddElement (pi);
				saveOpenedItemsInUserConfig ();
			}
		}
		public void CloseItem (ProjectItem pi) {			
			openedItems.RemoveElement (pi);
			saveOpenedItemsInUserConfig ();
		}

		public void CloseSolution () {
			while (openedItems.Count > 0) {
				openedItems.RemoveElement (openedItems [0]);
			}
			while (toolboxItems.Count > 0) {
				toolboxItems.RemoveElement (toolboxItems [0]);
			}
			NotifyValueChanged ("Projects", null);
		}
	    /// <summary>
	    /// Solution name
	    /// </summary>
	    String name;
		/// <summary>
		/// File path from where solution was loaded.
		/// </summary>
		[XmlIgnore]
		String path;

	    /// <summary>
	    /// Solution name for debugger.
	    /// </summary>
	    [ExcludeFromCodeCoverage]
	    public override string ToString()
	    {
	        return "Solution: " + name;
	    }
			
		#region Solution properties
	    double slnVer;                                      // 11.00 - vs2010, 12.00 - vs2015

	    /// <summary>
	    /// Visual studio version information used for generation, for example 2010, 2012, 2015 and so on...
	    /// </summary>
	    public int fileFormatVersion;

	    /// <summary>
	    /// null for old visual studio's
	    /// </summary>
	    public String VisualStudioVersion;
	    
	    /// <summary>
	    /// null for old visual studio's
	    /// </summary>
	    public String MinimumVisualStudioVersion;

	    /// <summary>
	    /// List of project included into solution.
	    /// </summary>
	    public List<Project> Projects = new List<Project>();

	    /// <summary>
	    /// List of configuration list, in form "{Configuration}|{Platform}", for example "Release|Win32".
	    /// To extract individual platforms / configuration list, use following functions.
	    /// </summary>
	    public List<String> configurations = new List<string>();

	    /// <summary>
	    /// Extracts platfroms supported by solution
	    /// </summary>
	    public IEnumerable<String> getPlatforms()
	    {
	        return configurations.Select(x => x.Split('|')[1]).Distinct();
	    }

	    /// <summary>
	    /// Extracts configuration names supported by solution
	    /// </summary>
	    public IEnumerable<String> getConfigurations()
	    {
	        return configurations.Select(x => x.Split('|')[0]).Distinct();
	    }
		#endregion

		public Configuration UserConfig;

		public Project StartupProject {
			get { return Projects?.FirstOrDefault (p => p.ProjectGuid == UserConfig.Get<string> ("StartupProject")); }
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

		#region CTOR
	    /// <summary>
	    /// Creates new solution.
	    /// </summary>
	    public Solution() { 
		}

		#endregion


		#region Loading from SLN file	
	    /// <summary>
	    /// Loads visual studio .sln solution
	    /// </summary>
	    /// <exception cref="System.IO.FileNotFoundException">The file specified in path was not found.</exception>
	    static public Solution LoadSolution(string path)
	    {
	        Solution s = new Solution();
	        s.path = path;

	        String slnTxt = File.ReadAllText(path);
	        //
	        //  Extra line feed is used by Visual studio, cmake does not generate extra line feed.
	        //
	        s.slnVer = Double.Parse(Regex.Match(slnTxt, "[\r\n]?Microsoft Visual Studio Solution File, Format Version ([0-9.]+)", RegexOptions.Multiline).Groups[1].Value, CultureInfo.InvariantCulture);

	        int vsNumber = Int32.Parse(Regex.Match(slnTxt, "^\\# Visual Studio (Express )?([0-9]+)", RegexOptions.Multiline).Groups[2].Value);
	        if (vsNumber > 2000)
	            s.fileFormatVersion = vsNumber;
	        else
	            s.fileFormatVersion = vsNumber - 14 + 2015;     // Visual Studio 14 => vs2015, formula might not be applicable for future vs versions.

	        foreach (String line in new String[] { "VisualStudioVersion", "MinimumVisualStudioVersion" })
	        {
	            var m = Regex.Match(slnTxt, "^" + line + " = ([0-9.]+)", RegexOptions.Multiline);
	            String v = null;
	            if (m.Success)
	                v = m.Groups[1].Value;

	            s.GetType().GetField(line).SetValue(s, v);
	        }

	        Regex reProjects = new Regex(
	            "Project\\(\"(?<ProjectHostGuid>{[A-F0-9-]+})\"\\) = \"(?<ProjectName>.*?)\", \"(?<RelativePath>.*?)\", \"(?<ProjectGuid>{[A-F0-9-]+})\"[\r\n]*(?<dependencies>.*?)EndProject[\r\n]+",
	            RegexOptions.Singleline);


	        reProjects.Replace(slnTxt, new MatchEvaluator(m =>
	        {
	            SolutionProject p = new SolutionProject ();

	            foreach (String g in reProjects.GetGroupNames())
	            {
	                if (g == "0")   //"0" - RegEx special kind of group
	                    continue;

	                //
	                // ProjectHostGuid, ProjectName, RelativePath, ProjectGuid fields/properties are set here.
	                //
	                String v = m.Groups[g].ToString();
	                if (g != "dependencies")
	                {
	                    FieldInfo fi = p.GetType().GetField(g);
	                    if (fi != null)
	                    {
	                        fi.SetValue(p, v);
	                    }
	                    else
	                    {
	                        p.GetType().GetProperty(g).SetValue(p, v);
	                    }
	                    continue;
	                }

	                if (v == "")    // No dependencies set
	                    continue;

	                String depsv = new Regex("ProjectSection\\(ProjectDependencies\\)[^\r\n]*?[\r\n]+" + "(.*?)" + "EndProjectSection", RegexOptions.Singleline).Match(v).Groups[1].Value;

	                //
	                // key is always equal to it's value.
	                // http://stackoverflow.com/questions/5629981/question-about-visual-studio-sln-file-format
	                //
	                var ProjectDependencies = new Regex("\\s*?({[A-F0-9-]+}) = ({[A-F0-9-]+})[\r\n]+", RegexOptions.Multiline).Matches(depsv).Cast<Match>().Select(x => x.Groups[1].Value).ToList();
	            } //foreach
				s.Projects.Add(new Project (s, p));
	            return "";
	        }
	        )
	        );

	        new Regex("GlobalSection\\(SolutionConfigurationPlatforms\\).*?[\r\n]+(.*?)EndGlobalSection[\r\n]+", RegexOptions.Singleline).Replace(slnTxt, new MatchEvaluator(m2 =>
	        {
	            s.configurations = new Regex("\\s*(.*)\\s+=").Matches(m2.Groups[1].ToString()).Cast<Match>().Select(x => x.Groups[1].Value).ToList();
	            return "";
	        }
	        ));

			Console.WriteLine ("******** CONFIG ***************");

	        new Regex("GlobalSection\\(ProjectConfigurationPlatforms\\).*?[\r\n]+(.*?)EndGlobalSection[\r\n]+", RegexOptions.Singleline).Replace(slnTxt, new MatchEvaluator(m2 =>
	        {
	            foreach (Match m3 in new Regex("\\s*({[A-F0-9-]+})\\.(.*?)\\.(.*?)\\s+=\\s+(.*?)[\r\n]+").Matches(m2.Groups[1].ToString()))
	            {
	                String guid = m3.Groups[1].Value;
	                String solutionConfig = m3.Groups[2].Value;
	                String action = m3.Groups[3].Value;
	                String projectConfig = m3.Groups[4].Value;

	                Project p = s.Projects.Where(x => x.ProjectGuid == guid).FirstOrDefault();
	                if (p == null)
	                    continue;

						Console.WriteLine ("{0},{1},{2},{3}",guid,solutionConfig,action,projectConfig);

	                int iConfigIndex = s.configurations.IndexOf(solutionConfig);
	                if (iConfigIndex == -1)
	                    continue;

//	                while (p.slnConfigurations.Count < s.configurations.Count)
//	                {
//	                    p.slnConfigurations.Add(null);
//	                    p.slnBuildProject.Add(false);
//	                }
//
//	                if (action == "ActiveCfg")
//	                {
//	                    p.slnConfigurations[iConfigIndex] = projectConfig;
//	                }
//	                else
//	                {
//	                    if (action.StartsWith("Build"))
//	                    {
//	                        p.slnBuildProject[iConfigIndex] = true;
//	                    }
//	                    else
//	                    {
//	                        if (action.StartsWith("Deploy"))
//	                        {
//	                            if (p.slnDeployProject == null) p.slnDeployProject = new List<bool?>();
//
//	                            while (p.slnDeployProject.Count < s.configurations.Count)
//	                                p.slnDeployProject.Add(null);
//
//	                            p.slnDeployProject[iConfigIndex] = true;
//	                        }
//	                    }
//	                } //if-esle
	            }
	            return "";
	        }
	        ));

	        //
	        // Initializes parent-child relationship.
	        //
	        new Regex("GlobalSection\\(NestedProjects\\).*?[\r\n]+(.*?)EndGlobalSection[\r\n]+", RegexOptions.Singleline).Replace(slnTxt, new MatchEvaluator(m4 =>
	        {
	            String v = m4.Groups[1].Value;
	            new Regex("\\s*?({[A-F0-9-]+}) = ({[A-F0-9-]+})[\r\n]+", RegexOptions.Multiline).Replace(v, new MatchEvaluator(m5 =>
	            {
	                String[] args = m5.Groups.Cast<System.Text.RegularExpressions.Group>().Skip(1).Select(x => x.Value).ToArray();
	                Project child = s.Projects.Where(x => args[0] == x.ProjectGuid).FirstOrDefault();
	                Project parent = s.Projects.Where(x => args[1] == x.ProjectGuid).FirstOrDefault();
	                parent.dependantProjects.Add(child);
	                child.ParentProject = parent;
	                return "";
	            }));
	            return "";
	        }
	        ));

			s.UserConfig = new Configuration (s.path + ".user");
			s.ReloadStyling ();
			s.ReloadDefaultTemplates ();
			s.updateToolboxItems ();
	        return s;
	    } //LoadSolution
		#endregion
	} 
}
