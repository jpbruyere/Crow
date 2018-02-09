//
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

namespace CrowIDE{
	public class SolutionProject {
		public string ProjectHostGuid;
		public string ProjectName;
		public string RelativePath;
		public string ProjectGuid;
	}
	/// <summary>
/// .sln loaded into class.
/// </summary>
	public class Solution
	{
	    /// <summary>
	    /// Solution name
	    /// </summary>
	    public String name;

		public string DisplayName {
			get { return name; }
		}

	    /// <summary>
	    /// File path from where solution was loaded.
	    /// </summary>
	    [XmlIgnore]
	    public String path;

	    /// <summary>
	    /// Solution name for debugger.
	    /// </summary>
	    [ExcludeFromCodeCoverage]
	    public override string ToString()
	    {
	        return "Solution, name = " + name;
	    }

	    /// <summary>
	    /// Gets solution path
	    /// </summary>
	    /// <returns></returns>
	    public String SolutionFolder
	    {
			get { return Path.GetDirectoryName (path); }
	    }

		public IList<Project> Projects {
			get {
				List<Project> tmp = new List<Project> ();
				foreach (SolutionProject p in projects) {
					string pp = Path.Combine (SolutionFolder, p.RelativePath.Replace('\\','/'));
					tmp.Add (new Project (pp, this));
				}
				return tmp;
			}
		}

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
	    public List<SolutionProject> projects = new List<SolutionProject>();

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


	    /// <summary>
	    /// Creates new solution.
	    /// </summary>
	    public Solution() { }

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
	            SolutionProject p = new SolutionProject();

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
	            s.projects.Add(p);
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

	        new Regex("GlobalSection\\(ProjectConfigurationPlatforms\\).*?[\r\n]+(.*?)EndGlobalSection[\r\n]+", RegexOptions.Singleline).Replace(slnTxt, new MatchEvaluator(m2 =>
	        {
	            foreach (Match m3 in new Regex("\\s*({[A-F0-9-]+})\\.(.*?)\\.(.*?)\\s+=\\s+(.*?)[\r\n]+").Matches(m2.Groups[1].ToString()))
	            {
	                String guid = m3.Groups[1].Value;
	                String solutionConfig = m3.Groups[2].Value;
	                String action = m3.Groups[3].Value;
	                String projectConfig = m3.Groups[4].Value;

	                SolutionProject p = s.projects.Where(x => x.ProjectGuid == guid).FirstOrDefault();
	                if (p == null)
	                    continue;

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
	                String[] args = m5.Groups.Cast<Group>().Skip(1).Select(x => x.Value).ToArray();
	                SolutionProject child = s.projects.Where(x => args[0] == x.ProjectGuid).FirstOrDefault();
	                SolutionProject parent = s.projects.Where(x => args[1] == x.ProjectGuid).FirstOrDefault();
//	                parent.nodes.Add(child);
//	                child.parent = parent;
	                return "";
	            }));
	            return "";
	        }
	        ));

	        return s;
	    } //LoadSolution
	} //class Solution
}
