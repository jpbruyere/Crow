//
// Project.cs
//
// Author:
//       jp <>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
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
using System.Xml;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CSharp;
using System.CodeDom.Compiler;
using Crow;
using System.Text.RegularExpressions;

namespace CrowIDE
{	
	public class Project: IValueChange
	{
		#region IValueChange implementation
		public event EventHandler<ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			ValueChanged.Raise(this, new ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		bool isLoaded = false;
		XmlDocument xmlDoc;
		XmlNode nodeProject;
		XmlNode nodeProps;
		XmlNodeList nodesItems;
		SolutionProject solutionProject;

		public Solution solution;
		public List<Crow.Command> Commands;
		public CompilerResults CompilationResults;
		public List<Project> dependantProjects = new List<Project>();
		public Project ParentProject = null;
		List<ProjectNode> rootItems;
		List<ProjectNode> flattenNodes;

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
		public bool IsStartupProject {
			get { 
				bool result = solution.StartupProject == this; 
				System.Diagnostics.Debug.WriteLine ("is startup project tested for {0} => {1}", this.ProjectGuid, result);
				return result;
			}
		}
		public string Path {
			get { return System.IO.Path.Combine (solution.SolutionFolder, solutionProject.RelativePath.Replace('\\','/')); }
		}
		public string RootDir {
			get { return System.IO.Path.GetDirectoryName (Path); }
		}



		public List<ProjectNode> RootItems {
			get { return rootItems; }
		}

		void buildTreeNodes(){
			ProjectNode root = new ProjectNode (this, ItemType.VirtualGroup, RootNamespace);
			List<ProjectItem> items = new List<ProjectItem> ();
			foreach (XmlNode i in nodesItems) {
				foreach (XmlNode f in i.ChildNodes) {
					items.Add (new ProjectItem (this, f));
				}
			}

			flattenNodes = new List<ProjectNode> ();

			ProjectNode refs = new ProjectNode (this, ItemType.ReferenceGroup, "References");
			root.ChildNodes.Add (refs);

			foreach (ProjectItem pn in items) {
				switch (pn.Type) {
				case ItemType.Reference:
					refs.ChildNodes.Add (pn);
					flattenNodes.Add (pn);
					break;					
				case ItemType.ProjectReference:
					ProjectReference pr = new ProjectReference (pn); 
					refs.ChildNodes.Add (pr);
					flattenNodes.Add (pr);
					break;					
				case ItemType.Compile:
				case ItemType.None:
				case ItemType.EmbeddedResource:						
					ProjectNode curNode = root;
					string[] folds = pn.Path.Split ('/');
					for (int i = 0; i < folds.Length - 1; i++) {
						ProjectNode nextNode = curNode.ChildNodes.FirstOrDefault (n => n.DisplayName == folds [i] && n.Type == ItemType.VirtualGroup);
						if (nextNode == null) {
							nextNode = new ProjectNode (this, ItemType.VirtualGroup, folds [i]);
							curNode.ChildNodes.Add (nextNode);
						}
						curNode = nextNode;
					}
					ProjectNode f = null;
					switch (pn.Extension) {
					case ".crow":
					case ".template":
						f = new ImlProjectItem (pn);
						break;
					default:
						f = new ProjectFile (pn);
						break;
					}
					curNode.ChildNodes.Add (f);
					flattenNodes.Add (f);
					break;
				}
			}
			root.SortChilds ();

			rootItems = root.ChildNodes;
		}
			
		#region Project properties
		public string ToolsVersion {
			get { return nodeProject?.Attributes ["ToolsVersion"]?.Value; }
		}
		public string DefaultTargets {
			get { return nodeProject?.Attributes ["DefaultTargets"]?.Value; }
		}
		public string ProjectGuid {
			get { return solutionProject.ProjectGuid; }
		}
		public string AssemblyName {
			get { return nodeProps["AssemblyName"]?.InnerText; }
		}
		public string OutputType {
			get { return nodeProps["OutputType"]?.InnerText; }
		}
		public string RootNamespace {
			get { return nodeProps["RootNamespace"]?.InnerText; }
		}
		public bool AllowUnsafeBlocks {
			get {return nodeProps["AllowUnsafeBlocks"] == null ? false :
				bool.Parse (nodeProps["AllowUnsafeBlocks"]?.InnerText); }
		}
		public bool NoStdLib {
			get { return nodeProps["NoStdLib"] == null ? false :
				bool.Parse (nodeProps["NoStdLib"]?.InnerText); }
		}
		public bool TreatWarningsAsErrors {
			get { return nodeProps["TreatWarningsAsErrors"] == null ? false:
				bool.Parse (nodeProps["TreatWarningsAsErrors"]?.InnerText); }
		}
		public bool SignAssembly {
			get { return bool.Parse (nodeProps["SignAssembly"]?.InnerText); }
		}
		public string TargetFrameworkVersion {
			get { return nodeProps["TargetFrameworkVersion"]?.InnerText; }
		}
		public string Description {
			get { return nodeProps["Description"]?.InnerText; }
		}
		public string OutputPath {
			get { return nodeProps["OutputPath"]?.InnerText; }
		}
		public string IntermediateOutputPath {
			get { return nodeProps["IntermediateOutputPath"]?.InnerText; }
		}
		public string StartupObject {
			get { return nodeProps["StartupObject"]?.InnerText; }
		}
		public bool DebugSymbols {
			get { return nodeProps["DebugSymbols"] == null ? false : bool.Parse( nodeProps["DebugSymbols"]?.InnerText); }
		}
		public int WarningLevel {
			get { return nodeProps["WarningLevel"] == null ? 0 : int.Parse(nodeProps["WarningLevel"]?.InnerText); }
		}

		#endregion


		public Project (Solution sol, SolutionProject sp) {
			solutionProject = sp;

			solution = sol;
			
			Commands = new List<Crow.Command> (new Crow.Command[] {
				new Crow.Command(new Action(() => Compile())) { Caption = "Compile"},
				new Crow.Command(new Action(() => setAsStartupProject())) { Caption = "Set as Startup Project"},
			});

			Load ();
		}

		public void Load () {
			
			xmlDoc = new XmlDocument();
			using (Stream ins = new FileStream (this.Path, FileMode.Open)) {
				xmlDoc.Load (new XmlTextReader(ins) { Namespaces = false });
			}

			nodeProject = xmlDoc.SelectSingleNode("Project");
			XmlNodeList nodesProps = xmlDoc.SelectNodes ("/Project/PropertyGroup");

			foreach (XmlNode n in nodesProps) {
				if (n.Attributes ["Condition"] == null)
					nodeProps = n;
			}
			nodesItems = xmlDoc.SelectNodes ("/Project/ItemGroup");

			if (ProjectGuid != solutionProject.ProjectGuid)
				throw new Exception ("Project GUID not matching with solution");

			buildTreeNodes ();

			IsLoaded = true;
		}

		void setAsStartupProject () {
			solution.StartupProject = this;
		}
		static Regex regexDirTokens = new Regex(@"\$\(([^\)]*)\)|([^\$]*)");

		string getDirectoryWithTokens (string dir){			
			Match m = regexDirTokens.Match (dir);
			string tmp = "";
			while (m.Success)
			{
				if (m.Value == @"$(SolutionDir)")
					tmp = System.IO.Path.Combine (tmp, solution.SolutionFolder);
				else if (m.Value == @"$(Configuration)")
					tmp = System.IO.Path.Combine (tmp, "Debug");
				else
					tmp = System.IO.Path.Combine (tmp, m.Value);

				if (tmp.EndsWith (@"\")||tmp.EndsWith (@"/"))
					tmp = tmp.Remove (tmp.Length - 1);

				m = m.NextMatch();
			}
			return tmp;
		}
		public string Compile () {
			GetStyling ();

			if (ParentProject != null)
				ParentProject.Compile ();

			CSharpCodeProvider cp = new CSharpCodeProvider();
			CompilerParameters parameters = new CompilerParameters();

			foreach (ProjectReference pr in flattenNodes.OfType<ProjectReference>()) {				
				Project p = solution.Projects.FirstOrDefault (pp => pp.ProjectGuid == pr.ProjectGUID);
				if (p == null)
					throw new Exception ("referenced project not found");
				parameters.ReferencedAssemblies.Add (p.Compile ());
			}

			string outputDir =  getDirectoryWithTokens(this.OutputPath);
			string objDir = getDirectoryWithTokens (this.IntermediateOutputPath);

			Directory.CreateDirectory (outputDir);
			Directory.CreateDirectory (objDir);

			parameters.OutputAssembly = System.IO.Path.Combine (outputDir, this.AssemblyName);

			// True - exe file generation, false - dll file generation
			if (this.OutputType == "Library") {
				parameters.GenerateExecutable = false;
				parameters.CompilerOptions += " /target:library";
				parameters.OutputAssembly += ".dll";
			} else {
				parameters.GenerateExecutable = true;
				parameters.CompilerOptions += " /target:exe";
				parameters.OutputAssembly += ".exe";
				parameters.MainClass = this.StartupObject;
			}

			parameters.GenerateInMemory = false;
			parameters.IncludeDebugInformation = this.DebugSymbols;
			parameters.TreatWarningsAsErrors = this.TreatWarningsAsErrors;
			parameters.WarningLevel = this.WarningLevel;
			parameters.CompilerOptions += " /noconfig";
			if (this.AllowUnsafeBlocks)
				parameters.CompilerOptions += " /unsafe";
			parameters.CompilerOptions += " /delaysign+";
			parameters.CompilerOptions += " /debug:full /debug+";
			parameters.CompilerOptions += " /optimize-";
			parameters.CompilerOptions += " /define:\"DEBUG;TRACE\"";
			parameters.CompilerOptions += " /nostdlib";



			foreach (ProjectItem pi in flattenNodes.Where (p=>p.Type == ItemType.Reference)) {
				
				if (string.IsNullOrEmpty (pi.HintPath)) {
					parameters.CompilerOptions += " /reference:/usr/lib/mono/4.5/" + pi.Path + ".dll";
					continue;
				}
				parameters.ReferencedAssemblies.Add (pi.Path);
				string fullHintPath = System.IO.Path.GetFullPath(System.IO.Path.Combine (RootDir, pi.HintPath.Replace('\\','/')));
				if (File.Exists(fullHintPath))
					File.Copy (fullHintPath, System.IO.Path.Combine(outputDir, System.IO.Path.GetFileName(fullHintPath)));
			}
			parameters.CompilerOptions += " /reference:/usr/lib/mono/4.5/System.Core.dll";
			parameters.CompilerOptions += " /reference:/usr/lib/mono/4.5/mscorlib.dll";
			//parameters.ReferencedAssemblies.Add ("System.Core");
			//parameters.ReferencedAssemblies.Add ("mscorlib.dll");


			IEnumerable<ProjectFile> pfs = flattenNodes.OfType<ProjectFile> ();

			foreach (ProjectFile pi in pfs.Where (p => p.Type == ItemType.EmbeddedResource)) {
				
				string absPath = pi.AbsolutePath;
				string logicName = pi.LogicalName;
				if (string.IsNullOrEmpty (logicName))
					parameters.CompilerOptions += string.Format (" /resource:{0},{1}", absPath , this.Name + "." + pi.Path.Replace('/','.'));
				else
					parameters.CompilerOptions += string.Format (" /resource:{0},{1}", absPath, logicName);
			}
			foreach (ProjectFile pi in pfs.Where (p => p.Type == ItemType.None)) {
				if (pi.CopyToOutputDirectory == CopyToOutputState.Never)
					continue;
				string source = pi.AbsolutePath;
				string target = System.IO.Path.Combine (outputDir, pi.Path);					
				Directory.CreateDirectory (System.IO.Path.GetDirectoryName (target));

				if (File.Exists (target)) {
					if (pi.CopyToOutputDirectory == CopyToOutputState.PreserveNewest) {
						if (DateTime.Compare (
							    System.IO.File.GetLastWriteTime (source),
							    System.IO.File.GetLastWriteTime (target)) < 0)
							continue;
					}
					File.Delete (target);
				}
				System.Diagnostics.Debug.WriteLine ("copy " + source + " to " + target);
				File.Copy (source, target);
			}
			string[] files = pfs.Where (p => p.Type == ItemType.Compile).Select (p => p.AbsolutePath).ToArray();

			System.Diagnostics.Debug.WriteLine("---- start compilation of :" + parameters.OutputAssembly);
			System.Diagnostics.Debug.WriteLine (parameters.CompilerOptions);

			CompilationResults = cp.CompileAssemblyFromFile(parameters, files);

			solution.UpdateErrorList ();

			return parameters.OutputAssembly;
		}	

	
		public void GetStyling () {
			foreach (ProjectFile pi in flattenNodes.OfType<ProjectFile> ().Where (pp=>pp.Type == ItemType.EmbeddedResource)) {
				Console.WriteLine (pi.Extension);
			}			
		}
	}
}

