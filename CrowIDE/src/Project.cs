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

namespace CrowIDE
{	
	public class Project {
		string path;
		XmlDocument xmlDoc;
		XmlNode nodeProject;
		XmlNode nodeProps;
		XmlNodeList nodesItems;
		public Solution solution;

		public string Name {
			get { return solution.projects.FirstOrDefault(p=>p.ProjectGuid == ProjectGuid).ProjectName; }
		}

		#region Project properties
		public string RootDir {
			get { return Path.GetDirectoryName (path); }
		}
		public string ToolsVersion {
			get { return nodeProject?.Attributes ["ToolsVersion"]?.Value; }
		}
		public string DefaultTargets {
			get { return nodeProject?.Attributes ["DefaultTargets"]?.Value; }
		}
		public string ProjectGuid {
			get { return nodeProps["ProjectGuid"]?.InnerText; }
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
			get { return bool.Parse (nodeProps["AllowUnsafeBlocks"]?.InnerText); }
		}
		public bool NoStdLib {
			get { return bool.Parse (nodeProps["NoStdLib"]?.InnerText); }
		}
		public bool TreatWarningsAsErrors {
			get { return bool.Parse (nodeProps["TreatWarningsAsErrors"]?.InnerText); }
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
						curNode.ChildNodes.Add (pn);
						break;
					}
				}
				root.SortChilds ();

				return root.ChildNodes;
			}

		}
		public Project () {}

		public Project (string _path, Solution _solution){
			solution = _solution;
			path = _path;
			xmlDoc = new XmlDocument();
			using (Stream ins = new FileStream (_path, FileMode.Open)) {
				xmlDoc.Load (new XmlTextReader(ins) { Namespaces = false });
			}

			nodeProject = xmlDoc.SelectSingleNode("Project");
			XmlNodeList nodesProps = xmlDoc.SelectNodes ("/Project/PropertyGroup");

			foreach (XmlNode n in nodesProps) {
				if (n.Attributes ["Condition"] == null)
					nodeProps = n;
			}
			nodesItems = xmlDoc.SelectNodes ("/Project/ItemGroup");

		}
	
	
		public void Compile () {
			CSharpCodeProvider cp = new CSharpCodeProvider();

			CompilerParameters parameters = new CompilerParameters();

			foreach (ProjectItem pi in Items.Where (p=>p.Type == ItemType.Reference)) {
				parameters.ReferencedAssemblies.Add (pi.Path);
			}
				
			parameters.GenerateInMemory = true;
			// True - exe file generation, false - dll file generation
			parameters.GenerateExecutable = true;

			string[] files = Items.Where (p => p.Type == ItemType.Compile).Select (p => p.AbsolutePath).ToArray();

			CompilerResults results = cp.CompileAssemblyFromFile(parameters, files);
		}	
	}
}

