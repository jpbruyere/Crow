// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Reflection;

namespace Crow.Coding
{
	public static class Startup
	{
#if NET472
		public static string sdkFolder = "/usr/lib/mono/msbuild/Current/bin/";
		public static string msbuildRoot = sdkFolder;

#else
		public static string sdkFolder = "/usr/share/dotnet/sdk";
		public static string msbuildRoot = Path.Combine (sdkFolder, "3.1.101/");
#endif

		[STAThread]
		static void Main ()
		{		
			AppDomain currentDomain = AppDomain.CurrentDomain;
			currentDomain.AssemblyResolve += msbuildAssembliesResolve;

			start ();

		}
		static void start()
		{
#if NET472
			var nativeSharedMethod = typeof (Microsoft.Build.Construction.SolutionFile).Assembly.GetType ("Microsoft.Build.Shared.NativeMethodsShared");
			var isMonoField = nativeSharedMethod.GetField ("_isMono", BindingFlags.Static | BindingFlags.NonPublic);
			isMonoField.SetValue (null, true);

			Environment.SetEnvironmentVariable ("MSBUILD_EXE_PATH", Path.Combine (msbuildRoot, "MSBuild.dll"));
#endif
			Environment.SetEnvironmentVariable ("MSBUILD_NUGET_PATH", "/home/jp/.nuget/packages");
			Environment.SetEnvironmentVariable ("FrameworkPathOverride", "/usr/lib/mono/4.5/");

			using (CrowIDE app = new CrowIDE ()) {
				app.Run ();
				app.saveWinConfigs ();
			}

		}
		static Assembly msbuildAssembliesResolve (object sender, ResolveEventArgs args)
		{
			string assemblyPath = Path.Combine (msbuildRoot, new AssemblyName (args.Name).Name + ".dll");
			if (!File.Exists (assemblyPath)) return null;
			Assembly assembly = Assembly.LoadFrom (assemblyPath);
			return assembly;
		}
	}
}
