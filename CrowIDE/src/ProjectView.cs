// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.Linq;
using System.CodeDom.Compiler;
using Microsoft.Build.Construction;
using Microsoft.Build.Framework;
using Microsoft.Build.Execution;
using System.Reflection;
using Microsoft.Build.Evaluation;

namespace Crow.Coding
{
	public class ProjectView : IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
		{
			ValueChanged.Raise (this, new ValueChangeEventArgs (MemberName, _value));
		}
		#endregion

		bool isLoaded = false;
		bool isExpanded;
		ProjectInSolution solutionProject;
		Microsoft.Build.Evaluation.Project project;

		Crow.Command cmdSave, cmdOpen, cmdCompile, cmdSetAsStartProj, cmdNewFile;

		#region CTOR
		public ProjectView (SolutionView sol, ProjectInSolution sp)
		{
			solutionProject = sp;
			solution = sol;

			//ProjectRootElement projectRootElt = ProjectRootElement.Open (solutionProject.AbsolutePath);
			ProjectCollection pc = new ProjectCollection ();

			project = new Project (solutionProject.AbsolutePath, sol.globalProperties, CrowIDE.toolsVersion, pc);

			cmdSave = new Crow.Command (new Action (() => Save ())) { Caption = "Save", Icon = new SvgPicture ("#Icons.save.svg"), CanExecute = true };
			cmdOpen = new Crow.Command (new Action (() => populateTreeNodes ())) { Caption = "Open", Icon = new SvgPicture ("#Icons.open.svg"), CanExecute = false };
			cmdCompile = new Crow.Command (new Action (() => Compile ("Restore"))) {
				Caption = "Restore",
			};
			cmdSetAsStartProj = new Crow.Command (new Action (() => setAsStartupProject ())) {
				Caption = "Set as Startup Project"
			};
			cmdNewFile = new Crow.Command (new Action (() => AddNewFile ())) {
				Caption = "Add New File",
				Icon = new SvgPicture ("#Icons.blank-file.svg"),
				CanExecute = true
			};

			Commands = new ObservableList<Crow.Command> (new Crow.Command [] { cmdOpen, cmdSave, cmdSetAsStartProj, cmdCompile, cmdNewFile });

			populateTreeNodes ();
		}
		#endregion

		public SolutionView solution;
		public ObservableList<Crow.Command> Commands;
		public CompilerResults CompilationResults;
		public List<ProjectView> dependantProjects = new List<ProjectView> ();
		public ProjectView ParentProject = null;
		List<ProjectNode> rootItems;

		public string Name {
			get { return solutionProject.ProjectName; }
		}
		public bool IsLoaded {
			get { return isLoaded; }
			set {
				if (isLoaded == value)
					return;
				isLoaded = value;
				NotifyValueChanged ("IsLoaded", isLoaded);
			}
		}
		public bool IsExpanded {
			get { return isExpanded; }
			set {
				if (value == isExpanded)
					return;
				isExpanded = value;
				NotifyValueChanged ("IsExpanded", isExpanded);
			}
		}
		public bool IsStartupProject {
			get { return solution.StartupProject == this; }
		}
		public string Path {
			get { return System.IO.Path.Combine (project.DirectoryPath.Replace ('\\', '/')); }
		}
		public string RootDir {
			get { return System.IO.Path.GetDirectoryName (Path); }
		}

		public List<ProjectNode> RootItems {
			get { return rootItems; }
		}

		#region Project properties
		public string ToolsVersion {
			get { return project.ToolsVersion; }
		}
		public string DefaultTargets {
			get { return project.Xml.DefaultTargets; }
		}
		public string ProjectGuid {
			get { return solutionProject.ProjectGuid; }
		}
		public string AssemblyName => project.AllEvaluatedProperties.Where (p => p.Name == "AssemblyName").FirstOrDefault ().EvaluatedValue;
		public string OutputType => project.AllEvaluatedProperties.Where (p => p.Name == "OutputType").FirstOrDefault ().EvaluatedValue;
		public string RootNamespace => project.AllEvaluatedProperties.Where (p => p.Name == "RootNamespace").FirstOrDefault ().EvaluatedValue;
		public bool AllowUnsafeBlocks {
			get {
				return false;
				/*return nodeProps["AllowUnsafeBlocks"] == null ? false :
               bool.Parse (nodeProps["AllowUnsafeBlocks"]?.InnerText);*/
			}
		}
		public bool NoStdLib {
			get {
				return false;
				/*return nodeProps["NoStdLib"] == null ? false :
              bool.Parse (nodeProps["NoStdLib"]?.InnerText);*/
			}
		}
		public bool TreatWarningsAsErrors {
			get {
				return false;
				/*return nodeProps["TreatWarningsAsErrors"] == null ? false :
              bool.Parse (nodeProps["TreatWarningsAsErrors"]?.InnerText);*/
			}
		}
		public bool SignAssembly {
			get { return false; }// projectRootElt.Properties.Where (p => p.Name == "SignAssembly").FirstOrDefault ().Value; }
		}
		public string TargetFrameworkVersion => project.AllEvaluatedProperties.Where (p => p.Name == "TargetFrameworkVersion").FirstOrDefault ().EvaluatedValue;
		public string Description => project.AllEvaluatedProperties.Where (p => p.Name == "Description").FirstOrDefault ().EvaluatedValue;
		public string OutputPath => project.AllEvaluatedProperties.Where (p => p.Name == "OutputPath").FirstOrDefault ().EvaluatedValue;
		public string IntermediateOutputPath => project.AllEvaluatedProperties.Where (p => p.Name == "IntermediateOutputPath").FirstOrDefault ().EvaluatedValue;
		public string StartupObject => project.AllEvaluatedProperties.Where (p => p.Name == "StartupObject").FirstOrDefault ().EvaluatedValue;
		public bool DebugSymbols => false;// nodeProps["DebugSymbols"] == null ? false : bool.Parse (nodeProps["DebugSymbols"]?.InnerText); }        
		public int WarningLevel => 0;
		#endregion


		public void AddNewFile ()
		{
			Window.Show (CrowIDE.MainIFace, "#CrowIDE.ui.NewFile.crow", true).DataSource = this;
		}

		void populateTreeNodes ()
		{

			ProjectNode root = new ProjectNode (this, ItemType.VirtualGroup, RootNamespace);
			ProjectNode refs = new ProjectNode (this, ItemType.ReferenceGroup, "References");
			root.AddChild (refs);



			foreach (Microsoft.Build.Evaluation.ProjectItem pn in project.AllEvaluatedItems) {
				//Console.WriteLine ($"{pn.ItemType}:{pn.EvaluatedInclude}");

				switch (pn.ItemType) {
				case "ProjectReferenceTargets":
					Commands.Add (new Crow.Command (new Action (() => Compile (pn.EvaluatedInclude))) {
						Caption = pn.EvaluatedInclude,
					});
					break;
				case "Reference":
				case "PackageReference":
					refs.AddChild (new ProjectItem (this, pn));
					break;
				case "ProjectReference":
					ProjectReference pr = new ProjectReference (this, pn);
					refs.AddChild (pr);
					break;
				case "Compile":
				case "None":
				case "EmbeddedResource":

					ProjectNode curNode = root;
					try {
						string file = pn.EvaluatedInclude.Replace ('\\', '/');
						string [] folds = file.Split ('/');
						for (int i = 0; i < folds.Length - 1; i++) {
							ProjectNode nextNode = curNode.ChildNodes.FirstOrDefault (n => n.DisplayName == folds [i] && n.Type == ItemType.VirtualGroup);
							if (nextNode == null) {
								nextNode = new ProjectNode (this, ItemType.VirtualGroup, folds [i]);
								curNode.AddChild (nextNode);
							}
							curNode = nextNode;
						}
						ProjectItem pi = new ProjectItem (this, pn);

						switch (System.IO.Path.GetExtension (file)) {
						/*case ".cs":
							f = new CSProjectFile (pn);
							break;*/
						case ".crow":
						case ".template":
						case ".goml":
						case ".itemp":
						case ".imtl":
							pi = new ImlProjectItem (pi);
							break;
						case ".style":
							pi = new StyleProjectItem (pi);
							break;
						default:
							pi = new ProjectFile (pi);
							break;
						}
						curNode.AddChild (pi);

					} catch (Exception ex) {

					}

					break;
				}
			}
			root.SortChilds ();
			rootItems = root.ChildNodes;
			IsLoaded = true;
		}

		public void Save ()
		{

		}

		void setAsStartupProject ()
		{
			solution.StartupProject = this;
		}
		//static Regex regexDirTokens = new Regex (@"\$\(([^\)]*)\)|([^\$]*)");

		//string getDirectoryWithTokens (string dir) {
		//    Match m = regexDirTokens.Match (dir);
		//    string tmp = "";
		//    while (m.Success) {
		//        if (m.Value == @"$(SolutionDir)")
		//            tmp = System.IO.Path.Combine (tmp, solution.SolutionFolder);
		//        else if (m.Value == @"$(Configuration)")
		//            tmp = System.IO.Path.Combine (tmp, "Debug");
		//        else
		//            tmp = System.IO.Path.Combine (tmp, m.Value);

		//        if (tmp.EndsWith (@"\") || tmp.EndsWith (@"/"))
		//            tmp = tmp.Remove (tmp.Length - 1);

		//        m = m.NextMatch ();
		//    }
		//    return tmp;
		//}
		public void Compile (string target = "Build")
		{
			/*var nativeSharedMethod = typeof (SolutionFile).Assembly.GetType ("Microsoft.Build.Shared.NativeMethodsShared");
			var isMonoField = nativeSharedMethod.GetField ("_isMono", BindingFlags.Static | BindingFlags.NonPublic);
			isMonoField.SetValue (null, true);

			Environment.SetEnvironmentVariable ("MSBUILD_EXE_PATH", "/usr/share/dotnet/sdk/3.1.101/MSBuild.dll");*/
			ProjectInstance pi = BuildManager.DefaultBuildManager.GetProjectInstanceForBuild (project);

			//ProjectInstance pi = new ProjectInstance (project.FullPath, solution.globalProperties, solution.toolsVersion);

			/*ILogger logger = new Microsoft.Build.Logging.ConsoleLogger {
				Verbosity = LoggerVerbosity.Diagnostic
			};*/

			if (pi.Build (new string [] { target }, solution.buildParams.Loggers))
				Console.WriteLine ("success");
			else
				Console.WriteLine ("error");

		}
		//    if (ParentProject != null)
		//        ParentProject.Compile ();

		//    CSharpCodeProvider cp = new CSharpCodeProvider ();
		//    CompilerParameters parameters = new CompilerParameters ();

		//    foreach (ProjectReference pr in flattenNodes.OfType<ProjectReference> ()) {
		//        Project p = solution.Projects.FirstOrDefault (pp => pp.ProjectGuid == pr.ProjectGUID);
		//        if (p == null)
		//            throw new Exception ("referenced project not found");
		//        parameters.ReferencedAssemblies.Add (p.Compile ());
		//    }

		//    string outputDir = getDirectoryWithTokens (this.OutputPath);
		//    string objDir = getDirectoryWithTokens (this.IntermediateOutputPath);

		//    Directory.CreateDirectory (outputDir);
		//    Directory.CreateDirectory (objDir);

		//    parameters.OutputAssembly = System.IO.Path.Combine (outputDir, this.AssemblyName);

		//    // True - exe file generation, false - dll file generation
		//    if (this.OutputType == "Library") {
		//        parameters.GenerateExecutable = false;
		//        parameters.CompilerOptions += " /target:library";
		//        parameters.OutputAssembly += ".dll";
		//    } else {
		//        parameters.GenerateExecutable = true;
		//        parameters.CompilerOptions += " /target:exe";
		//        parameters.OutputAssembly += ".exe";
		//        parameters.MainClass = this.StartupObject;
		//    }

		//    parameters.GenerateInMemory = false;
		//    parameters.IncludeDebugInformation = this.DebugSymbols;
		//    parameters.TreatWarningsAsErrors = this.TreatWarningsAsErrors;
		//    parameters.WarningLevel = this.WarningLevel;
		//    parameters.CompilerOptions += " /noconfig";
		//    if (this.AllowUnsafeBlocks)
		//        parameters.CompilerOptions += " /unsafe";
		//    parameters.CompilerOptions += " /delaysign+";
		//    parameters.CompilerOptions += " /debug:full /debug+";
		//    parameters.CompilerOptions += " /optimize-";
		//    parameters.CompilerOptions += " /define:\"DEBUG;TRACE\"";
		//    parameters.CompilerOptions += " /nostdlib";



		//    foreach (ProjectItem pi in flattenNodes.Where (p => p.Type == ItemType.Reference)) {

		//        if (string.IsNullOrEmpty (pi.HintPath)) {
		//            parameters.CompilerOptions += " /reference:/usr/lib/mono/4.5/" + pi.Path + ".dll";
		//            continue;
		//        }
		//        parameters.ReferencedAssemblies.Add (pi.Path);
		//        string fullHintPath = System.IO.Path.GetFullPath (System.IO.Path.Combine (RootDir, pi.HintPath.Replace ('\\', '/')));
		//        if (File.Exists (fullHintPath)) {
		//            string outPath = System.IO.Path.Combine (outputDir, System.IO.Path.GetFileName (fullHintPath));
		//            if (!File.Exists (outPath))
		//                File.Copy (fullHintPath, outPath);
		//        }
		//    }
		//    parameters.CompilerOptions += " /reference:/usr/lib/mono/4.5/System.Core.dll";
		//    parameters.CompilerOptions += " /reference:/usr/lib/mono/4.5/mscorlib.dll";
		//    //parameters.ReferencedAssemblies.Add ("System.Core");
		//    //parameters.ReferencedAssemblies.Add ("mscorlib.dll");


		//    IEnumerable<ProjectFile> pfs = flattenNodes.OfType<ProjectFile> ();

		//    foreach (ProjectFile pi in pfs.Where (p => p.Type == ItemType.EmbeddedResource)) {

		//        string absPath = pi.AbsolutePath;
		//        string logicName = pi.LogicalName;
		//        if (string.IsNullOrEmpty (logicName))
		//            parameters.CompilerOptions += string.Format (" /resource:{0},{1}", absPath, this.Name + "." + pi.Path.Replace ('/', '.'));
		//        else
		//            parameters.CompilerOptions += string.Format (" /resource:{0},{1}", absPath, logicName);
		//    }
		//    foreach (ProjectFile pi in pfs.Where (p => p.Type == ItemType.None)) {
		//        if (pi.CopyToOutputDirectory == CopyToOutputState.Never)
		//            continue;
		//        string source = pi.AbsolutePath;
		//        string target = System.IO.Path.Combine (outputDir, pi.Path);
		//        Directory.CreateDirectory (System.IO.Path.GetDirectoryName (target));

		//        if (File.Exists (target)) {
		//            if (pi.CopyToOutputDirectory == CopyToOutputState.PreserveNewest) {
		//                if (DateTime.Compare (
		//                        System.IO.File.GetLastWriteTime (source),
		//                        System.IO.File.GetLastWriteTime (target)) < 0)
		//                    continue;
		//            }
		//            File.Delete (target);
		//        }
		//        System.Diagnostics.Debug.WriteLine ("copy " + source + " to " + target);
		//        File.Copy (source, target);
		//    }
		//    string[] files = pfs.Where (p => p.Type == ItemType.Compile).Select (p => p.AbsolutePath).ToArray ();

		//    System.Diagnostics.Debug.WriteLine ("---- start compilation of :" + parameters.OutputAssembly);
		//    System.Diagnostics.Debug.WriteLine (parameters.CompilerOptions);

		//    CompilationResults = cp.CompileAssemblyFromFile (parameters, files);

		//    solution.UpdateErrorList ();

		//    return parameters.OutputAssembly;
		//}

		//public bool TryGetProjectFileFromAbsolutePath (string absolutePath, out ProjectFile pi) {
		//    pi = flattenNodes.OfType<ProjectFile> ().FirstOrDefault
		//        (pp => pp.AbsolutePath == absolutePath);
		//    return pi != null;
		//}
		//public bool TryGetProjectFileFromPath (string path, out ProjectFile pi) {
		//if (path.StartsWith ("#", StringComparison.Ordinal))
		//    pi = flattenNodes.OfType<ProjectFile> ().FirstOrDefault
		//        (pp => pp.Type == ItemType.EmbeddedResource && pp.ResourceID == path.Substring (1));
		//else
		//    pi = flattenNodes.OfType<ProjectFile> ().FirstOrDefault (pp => pp.Path == path);

		//if (pi != null)
		//    return true;

		//foreach (ProjectReference pr in flattenNodes.OfType<ProjectReference> ()) {
		//    Project p = solution.Projects.FirstOrDefault (pp => pp.ProjectGuid == pr.ProjectGUID);
		//    if (p == null)
		//        throw new Exception ("referenced project not found");
		//    if (p.TryGetProjectFileFromPath (path, out pi))
		//        return true;
		//}
		////TODO: search referenced assemblies
		//return "";
		//}

		public void GetDefaultTemplates ()
		{
			//IEnumerable<ProjectFile> tmpFiles =
			//    flattenNodes.OfType<ProjectFile> ().Where (pp => pp.Extension == ".template");

			//foreach (ProjectFile pi in tmpFiles.Where (
			//    pp => pp.Type == ItemType.None && pp.CopyToOutputDirectory != CopyToOutputState.Never)) {

			//    string clsName = System.IO.Path.GetFileNameWithoutExtension (pi.Path);
			//    if (solution.DefaultTemplates.ContainsKey (clsName))
			//        continue;
			//    solution.DefaultTemplates[clsName] = pi.AbsolutePath;
			//}
			//foreach (ProjectFile pi in tmpFiles.Where (pp => pp.Type == ItemType.EmbeddedResource)) {
			//    string resId = pi.ResourceID;
			//    string clsName = resId.Substring (0, resId.Length - 9);
			//    if (solution.DefaultTemplates.ContainsKey (clsName))
			//        continue;
			//    solution.DefaultTemplates[clsName] = pi.Path;
			//}

			//foreach (Project p in ReferencedProjects)
			//p.GetDefaultTemplates ();
		}
		//		void searchTemplatesIn(Assembly assembly){
		//			if (assembly == null)
		//				return;
		//			foreach (string resId in assembly
		//				.GetManifestResourceNames ()
		//				.Where (r => r.EndsWith (".template", StringComparison.OrdinalIgnoreCase))) {
		//				string clsName = resId.Substring (0, resId.Length - 9);
		//				if (DefaultTemplates.ContainsKey (clsName))
		//					continue;
		//				DefaultTemplates[clsName] = "#" + resId;
		//			}
		//		}

		public List<ProjectView> ReferencedProjects {
			get {
				List<ProjectView> tmp = new List<ProjectView> ();
				/*foreach (ProjectReference pr in flattenNodes.OfType<ProjectReference> ()) {
                    Project p = solution.Projects.FirstOrDefault (pp => pp.ProjectGuid == pr.ProjectGUID);
                    if (p != null)
                        tmp.Add (p);
                }*/
				return tmp;
			}
		}

		public void GetStyling ()
		{
			/*try {
                foreach (ProjectFile pi in flattenNodes.OfType<ProjectFile> ().Where (pp => pp.Type == ItemType.EmbeddedResource && pp.Extension == ".style")) {
                    using (Stream s = new MemoryStream (System.Text.Encoding.UTF8.GetBytes (pi.Source))) {
                        new StyleReader (solution.Styling, s, pi.ResourceID);
                    }
                }
            } catch (Exception ex) {
                Console.WriteLine (ex.ToString ());
            }
            foreach (ProjectReference pr in flattenNodes.OfType<ProjectReference> ()) {
                Project p = solution.Projects.FirstOrDefault (pp => pp.ProjectGuid == pr.ProjectGUID);
                if (p != null)
                    //throw new Exception ("referenced project not found");
                	p.GetStyling ();
            }*/

			//TODO:get styling from referenced assemblies
		}
	}
}

