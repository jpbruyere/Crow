using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.Build.Construction;
using Microsoft.Build.Evaluation;
using Microsoft.Build.Execution;
using Microsoft.Build.Framework;

namespace testMSBuild
{
	class Program
	{

		//const string msbuildRoot = "/usr/lib/mono/msbuild/Current/bin/";
		const string msbuildRoot = "/usr/share/dotnet/sdk/3.1.101/";
		//const string msbuildRoot = "/mnt/data/src/microsoft/msbuild/artifacts/bin/bootstrap/netcoreapp2.1/MSBuild/";
		const string toolsVersion = "Current";

		static void Main (string [] args)
		{
			/*var nativeSharedMethod = typeof (SolutionFile).Assembly.GetType ("Microsoft.Build.Shared.NativeMethodsShared");
			var isMonoField = nativeSharedMethod.GetField ("_isMono", BindingFlags.Static | BindingFlags.NonPublic);
			isMonoField.SetValue (null, true);*/

			//Environment.SetEnvironmentVariable (MSBUILD_EXE_PATH, "/usr/share/dotnet/sdk/3.1.101/MSBuild.dll");
			Environment.SetEnvironmentVariable ("MSBUILD_EXE_PATH", Path.Combine(msbuildRoot, "MSBuild.dll"));
			Environment.SetEnvironmentVariable ("MSBUILD_NUGET_PATH", "/home/jp/.nuget/packages");


			//Environment.SetEnvironmentVariable ("COREHOST_TRACE", "1");

			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += msbuildAssembliesResolve;

			compile ();
		}

		static Assembly msbuildAssembliesResolve (object sender, ResolveEventArgs args)
		{
			string assemblyPath = Path.Combine (msbuildRoot, new AssemblyName (args.Name).Name + ".dll");
			/*Console.BackgroundColor = ConsoleColor.DarkBlue;
			Console.ForegroundColor = ConsoleColor.White;
			Console.WriteLine ($"Probing: {assemblyPath}");
			Console.ResetColor ();*/
			if (!File.Exists (assemblyPath)) return null;
			Assembly assembly = Assembly.LoadFrom (assemblyPath);
			return assembly;
		}


		static void compile ()
		{
			//Console.WriteLine ($"{Microsoft.Build. BuildEnvironmentHelper.Instance.CurrentMSBuildExePath}");
			string slnPath = "/mnt/devel/glfw-sharp/glfw-sharp.sln";//<--- change here can be another VS type ex: .vcxproj
			//string slnPath = "/mnt/devel/vke.net/vke.net.sln";//<--- change here can be another VS type ex: .vcxproj
																	//string csprojPath = "/mnt/devel/glfw-sharp/glfw-sharp.csproj";
			//ILogger Logger = new BasicLogger ();
			ILogger Logger = new Microsoft.Build.Logging.ConsoleLogger ();
			Logger.Verbosity = LoggerVerbosity.Minimal;
			//Logger.Parameters = "V=DIAG;consoleloggerparameters:ShowEventId";
			ProjectCollection projectCollection = new ProjectCollection ();
			projectCollection.DefaultToolsVersion = toolsVersion;
			//projectCollection.IsBuildEnabled = false;
			//projectCollection.LoadProject (projectFileName);
			SolutionFile sln = SolutionFile.Parse (slnPath);

			/*foreach (ProjectInSolution pis in sln.ProjectsInOrder) {
				Console.WriteLine ($"{pis.ProjectName} {pis.RelativePath} {pis.ProjectType}");
			}*/


			//Project project = new Project (projectCollection);
			BuildParameters buildParams = new BuildParameters (projectCollection);
			buildParams.Loggers = new List<ILogger> () { Logger };
			//buildParams.DefaultToolsVersion = "Current";
			//buildParams.DetailedSummary = true;
			//buildParams.LogInitialPropertiesAndItems = true;
			//buildParams.LogTaskInputs = true;

			buildParams.ResetCaches = false;

			Dictionary<String, String> globalProperties = new Dictionary<String, String> ();

			globalProperties ["Configuration"] = sln.GetDefaultConfigurationName ();
			globalProperties ["Platform"] = sln.GetDefaultPlatformName ();


			//globalProperties ["RollForward"] = "none";
			//globalProperties ["_IsRollForwardSupported"] = "false";

			//globalProperties ["GenerateRuntimeConfigurationFiles"] = "true";

			//globalProperties ["NuGetPackageRoot"] = "/home/jp/.nuget/packages";
			//globalProperties ["RoslynTargetsPath"] = Path.Combine (msbuildRoot, "Roslyn/");
			//globalProperties ["MSBuildSDKsPath"] = Path.Combine (msbuildRoot, "Sdks/");

			//globalProperties ["AdditionalLibPaths"] = msbuildRoot;
			//globalProperties ["AssemblySearchPaths"] = msbuildRoot;


			//globalProperty.Add ("CSharpCoreTargetsPath", "/usr/share/dotnet/sdk/3.1.101/Roslyn/Microsoft.CSharp.Core.targets");

			/*ProjectInstance pinst = new ProjectInstance (csprojPath, globalProperties, "Current");
			if (pinst.Build (new String [] { "Build" }, buildParams.Loggers, out System.Collections.Generic.IDictionary<string, Microsoft.Build.Execution.TargetResult> targetOutputs))
				Console.WriteLine ("Success");
			ProjectRootElement pre = pinst.ToProjectRootElement ();*/

			//List <Project> projects = new List<Project> ();

			//projectCollection.ProjectAdded += (sender, e) => {
			//	//	pc = sender as ProjectCollection;
			//	//	Console.ForegroundColor = ConsoleColor.Yellow;
			//	//	Console.WriteLine ($"Proj added: {e.ProjectRootElement} {e.ProjectRootElement.FullPath}");
			//	//	Console.ResetColor ();

			//	Project p = new Project (e.ProjectRootElement);//, globalProperties, toolsVersion, projectCollection);
			//												   //	//projects.Add (p);
			//	Console.WriteLine ($"\nEvaluated properties:");
			//	foreach (var pi in p.Properties)
			//		Console.WriteLine ($"\t{pi.Name} = {pi.EvaluatedValue}");
			//	Console.WriteLine ("");

			//	//	//foreach (ProjectItem pi in p.AllEvaluatedItems)
			//	//	//Console.WriteLine ($"{pi.ItemType} {pi.EvaluatedInclude}");
			//	//	/*foreach (GlobResult gr in p.GetAllGlobs()) {
			//	//		foreach (string g in gr.IncludeGlobs) {
			//	//			Console.WriteLine ($"\t\t{g}");
			//	//		}
			//};

			//};

			BuildManager.DefaultBuildManager.ResetCaches ();

			//string[] targetsToBuild = { "ResolveAssemblyReferences" };
			string [] targetsToBuild = { "restore", "build"};
			//string [] targetsToBuild = { "Rebuild" };

			//globalProperties ["Configuration"] = "ReleaseSpirVTasks";

			BuildRequestData buildRequest = new BuildRequestData (slnPath, globalProperties, "Current", targetsToBuild, null);

			BuildResult buildResult = BuildManager.DefaultBuildManager.Build (buildParams, buildRequest);


			if (buildResult.OverallResult == BuildResultCode.Success) {
				Console.WriteLine ("Build succes.");
			} else {
				foreach (var item in buildResult.ResultsByTarget) {
					Console.WriteLine ($"{item.Key} -> {item.Value.ResultCode} {item.Value.ToString()}");
				}

				Console.WriteLine ("Error: " + buildResult.Exception);
			}
			//Console.WriteLine ($"proj count: {}");
			//MessageBox.Show (Logger.GetLogString ());
			/*foreach (Project p in buildParams. ..LoadedProjects) {
				foreach (ProjectItem pi in p.AllEvaluatedItems) {
					Console.WriteLine ($"{pi.ItemType} {pi}");	
				}
			}*/
 
		}
	}

	public class FileLogger : ILogger
	{
		public LoggerVerbosity Verbosity { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }
		public string Parameters { get => throw new NotImplementedException (); set => throw new NotImplementedException (); }

		Stream log;
		StreamWriter sw;

		public void Initialize (IEventSource eventSource)
		{
			log = new FileStream ("buildlog.txt", FileMode.Create);
			sw = new StreamWriter (log);

			eventSource.MessageRaised += (sender, e) => sw.WriteLine ($"{e.Message}"); 
			eventSource.BuildStarted += (sender, e) => sw.WriteLine("Build started");
			eventSource.BuildFinished += (sender, e) => sw.WriteLine ("Build finished");
		}

		public void Shutdown ()
		{
			log.Flush ();
			sw.Dispose ();
			log.Dispose ();
		}
	}

	public class BasicLogger : ILogger
	{
		public LoggerVerbosity Verbosity { get; set; }
		public string Parameters { get; set; }

		public void Initialize (IEventSource eventSource)
		{
			//eventSource.ProjectStarted += (sender, e) => Console.WriteLine ($"Building project: {e.Message}");
			//eventSource.ProjectFinished += (sender, e) => Console.WriteLine ($"Done project: {e.Message}");
			eventSource.ErrorRaised += (sender, e) => {
				Console.ForegroundColor = ConsoleColor.White;
				Console.BackgroundColor = ConsoleColor.Red;
				Console.WriteLine ($"{e.Message}");
				Console.ResetColor ();
			};
			eventSource.TargetStarted += (sender, e) => {
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine ($"Target start: {e.TargetName}<-{e.ParentTarget} {e.Message}");
				Console.ResetColor ();
			};
			eventSource.TargetFinished += (sender, e) => {
				Console.ForegroundColor = ConsoleColor.DarkYellow;
				Console.WriteLine ($"\tDone {e.TargetName} ({e.Succeeded}) : {e.Message}");
				Console.ResetColor ();
			};

			eventSource.MessageRaised += (sender, e) => {
				Console.ForegroundColor = ConsoleColor.Gray;
				Console.WriteLine ($"{e.HelpKeyword} {e.Message}");
				Console.ResetColor ();
			};
			//eventSource.AnyEventRaised += (sender, e) => Console.WriteLine ($"{e.Timestamp}:{e.SenderName}=>{e.Message}"); 
			//eventSource.BuildStarted += (sender, e) => Console.WriteLine("Build started");
			eventSource.BuildFinished += (sender, e) => Console.WriteLine ("Build finished");
		}

		public void Shutdown ()
		{
		}
	}

	//public class BasicLogger : ILogger
	//	{
	//	MemoryStream streamMem = new MemoryStream ();
	//	/// <summary>
	//	/// Initialize is guaranteed to be called by MSBuild at the start of the build
	//	/// before any events are raised.
	//	/// </summary>
	//	public override void Initialize (IEventSource eventSource)
	//	{

	//		try {
	//			// Open the file
	//			this.streamWriter = new StreamWriter (streamMem);
	//			//this.streamWriter = new StreamWriter(logFile);
	//		} catch (Exception ex) {
	//			if
	//			(
	//				ex is UnauthorizedAccessException
	//				|| ex is ArgumentNullException
	//				|| ex is PathTooLongException
	//				|| ex is DirectoryNotFoundException
	//				|| ex is NotSupportedException
	//				|| ex is ArgumentException
	//				|| ex is SecurityException
	//				|| ex is IOException
	//			) {
	//				throw new LoggerException ("Failed to create log file: " + ex.Message);
	//			} else {
	//				// Unexpected failure
	//				throw;
	//			}
	//		}

	//		// For brevity, we'll only register for certain event types. Loggers can also
	//		// register to handle TargetStarted/Finished and other events.
	//		eventSource.ProjectStarted += new ProjectStartedEventHandler (eventSource_ProjectStarted);
	//		eventSource.TaskStarted += new TaskStartedEventHandler (eventSource_TaskStarted);
	//		eventSource.MessageRaised += new BuildMessageEventHandler (eventSource_MessageRaised);
	//		eventSource.WarningRaised += new BuildWarningEventHandler (eventSource_WarningRaised);
	//		eventSource.ErrorRaised += new BuildErrorEventHandler (eventSource_ErrorRaised);
	//		eventSource.ProjectFinished += new ProjectFinishedEventHandler (eventSource_ProjectFinished);
	//	}

	//	void eventSource_ErrorRaised (object sender, BuildErrorEventArgs e)
	//	{
	//		// BuildErrorEventArgs adds LineNumber, ColumnNumber, File, amongst other parameters
	//		string line = String.Format (": ERROR {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
	//		WriteLineWithSenderAndMessage (line, e);
	//	}

	//	void eventSource_WarningRaised (object sender, BuildWarningEventArgs e)
	//	{
	//		// BuildWarningEventArgs adds LineNumber, ColumnNumber, File, amongst other parameters
	//		string line = String.Format (": Warning {0}({1},{2}): ", e.File, e.LineNumber, e.ColumnNumber);
	//		WriteLineWithSenderAndMessage (line, e);
	//	}

	//	void eventSource_MessageRaised (object sender, BuildMessageEventArgs e)
	//	{
	//		// BuildMessageEventArgs adds Importance to BuildEventArgs
	//		// Let's take account of the verbosity setting we've been passed in deciding whether to log the message
	//		if ((e.Importance == MessageImportance.High && IsVerbosityAtLeast (LoggerVerbosity.Minimal))
	//			|| (e.Importance == MessageImportance.Normal && IsVerbosityAtLeast (LoggerVerbosity.Normal))
	//			|| (e.Importance == MessageImportance.Low && IsVerbosityAtLeast (LoggerVerbosity.Detailed))
	//		) {
	//			WriteLineWithSenderAndMessage (String.Empty, e);
	//		}
	//	}

	//	void eventSource_TaskStarted (object sender, TaskStartedEventArgs e)
	//	{
	//		// TaskStartedEventArgs adds ProjectFile, TaskFile, TaskName
	//		// To keep this log clean, this logger will ignore these events.
	//	}

	//	void eventSource_ProjectStarted (object sender, ProjectStartedEventArgs e)
	//	{
	//		// ProjectStartedEventArgs adds ProjectFile, TargetNames
	//		// Just the regular message string is good enough here, so just display that.
	//		WriteLine (String.Empty, e);
	//		indent++;
	//	}

	//	void eventSource_ProjectFinished (object sender, ProjectFinishedEventArgs e)
	//	{
	//		// The regular message string is good enough here too.
	//		indent--;
	//		WriteLine (String.Empty, e);
	//	}

	//	/// <summary>
	//	/// Write a line to the log, adding the SenderName and Message
	//	/// (these parameters are on all MSBuild event argument objects)
	//	/// </summary>
	//	private void WriteLineWithSenderAndMessage (string line, BuildEventArgs e)
	//	{
	//		if (0 == String.Compare (e.SenderName, "MSBuild", true /*ignore case*/)) {
	//			// Well, if the sender name is MSBuild, let's leave it out for prettiness
	//			WriteLine (line, e);
	//		} else {
	//			WriteLine (e.SenderName + ": " + line, e);
	//		}
	//	}

	//	/// <summary>
	//	/// Just write a line to the log
	//	/// </summary>
	//	private void WriteLine (string line, BuildEventArgs e)
	//	{
	//		for (int i = indent; i > 0; i--) {
	//			streamWriter.Write ("\t");
	//		}
	//		streamWriter.WriteLine (line + e.Message);
	//	}


	//	public string GetLogString ()
	//	{
	//		var sr = new StreamReader (streamMem);
	//		var myStr = sr.ReadToEnd ();
	//		return myStr;
	//	}
	//	/// <summary>
	//	/// Shutdown() is guaranteed to be called by MSBuild at the end of the build, after all 
	//	/// events have been raised.
	//	/// </summary>
	//	/// 
	//	/// 
	//	public override void Shutdown ()
	//	{
	//		streamWriter.Flush ();
	//		streamMem.Position = 0;
	//	}
	//	private StreamWriter streamWriter;
	//	private int indent;
	//}
}
