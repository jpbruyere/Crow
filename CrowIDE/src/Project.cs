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
		public Solution solution;
		public List<Crow.Command> Commands;
		public CompilerResults CompilationResults;
		public List<Project> dependantProjects = new List<Project>();
		public Project ParentProject = null;

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


		#region Project properties
		public string RootDir {
			get { return System.IO.Path.GetDirectoryName (Path); }
		}
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
		#endregion

		public List<ProjectItem> Items {
			get {
				List<ProjectItem> tmp = new List<ProjectItem> ();
				foreach (XmlNode i in nodesItems) {
					foreach (XmlNode f in i.ChildNodes) {
						tmp.Add (new ProjectItem (this, f));
					}
				}
				return tmp;
			}
		}

		public List<ProjectNode> RootItems {
			get {
				ProjectNode root = new ProjectNode (this, ItemType.VirtualGroup, RootNamespace);
				List<ProjectItem> items = Items;

				ProjectNode refs = new ProjectNode (this, ItemType.ReferenceGroup, "References");
				root.ChildNodes.Add (refs);

				foreach (ProjectItem pn in items) {
					switch (pn.Type) {
					case ItemType.Reference:
						refs.ChildNodes.Add (pn);
						break;					
					case ItemType.ProjectReference:
						refs.ChildNodes.Add (new ProjectReference(pn));
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
						switch (pn.Extension) {
						case ".crow":
						case ".template":
							curNode.ChildNodes.Add (new ImlProjectItem(pn));
							break;
						default:
							curNode.ChildNodes.Add (pn);
							break;
						}


						break;
					}
				}
				root.SortChilds ();

				return root.ChildNodes;
			}

		}
		SolutionProject solutionProject;

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

			IsLoaded = true;
		}

		void setAsStartupProject () {
			solution.StartupProject = this;
		}
	
		public void Compile () {
			if (ParentProject != null)
				ParentProject.Compile ();

			CSharpCodeProvider cp = new CSharpCodeProvider();
			CompilerParameters parameters = new CompilerParameters();

			foreach (ProjectItem pi in Items.Where (pp=>pp.Type == ItemType.ProjectReference)) {
				ProjectReference pr = new ProjectReference (pi);
				Project p = solution.Projects.FirstOrDefault (pp => pp.ProjectGuid == pr.ProjectGUID);
				if (p == null)
					throw new Exception ("referenced project not found");
				p.Compile ();
				parameters.ReferencedAssemblies.Add (pr.DisplayName);
			}

			foreach (ProjectItem pi in Items.Where (p=>p.Type == ItemType.Reference)) {
				parameters.ReferencedAssemblies.Add (pi.Path);
			}
			parameters.ReferencedAssemblies.Add ("System.Core");
				
			parameters.GenerateInMemory = true;

			if (!Directory.Exists("testbuild"))
				Directory.CreateDirectory ("testbuild");
			parameters.OutputAssembly = "testbuild/" + this.AssemblyName;

			// True - exe file generation, false - dll file generation
			if (this.OutputType == "Library") {
				parameters.GenerateExecutable = false;
				parameters.CompilerOptions += " /target:library";
				parameters.OutputAssembly += ".dll";
			} else {
				parameters.GenerateExecutable = true;
				parameters.CompilerOptions += " /target:exe";
				parameters.OutputAssembly += ".exe";
			}

			parameters.IncludeDebugInformation = true;
			parameters.TreatWarningsAsErrors = this.TreatWarningsAsErrors;
			parameters.WarningLevel = 4;
			parameters.CompilerOptions += " /noconfig";
			if (this.AllowUnsafeBlocks)
				parameters.CompilerOptions += " /unsafe";
			parameters.CompilerOptions += " /delaysign+";

			foreach (ProjectFile pi in Items.OfType<ProjectFile>().Where (p => p.Type == ItemType.EmbeddedResource)) {
				
				string absPath = pi.AbsolutePath;
				string logicName = pi.LogicalName;
				if (string.IsNullOrEmpty (logicName))
					parameters.CompilerOptions += string.Format (" /resource:{0}", absPath);
				else
					parameters.CompilerOptions += string.Format (" /resource:{0},{1}", absPath, logicName);
			}


			string[] files = Items.Where (p => p.Type == ItemType.Compile).Select (p => p.AbsolutePath).ToArray();

			CompilationResults = cp.CompileAssemblyFromFile(parameters, files);

			solution.UpdateErrorList ();
		}	
	}
}

