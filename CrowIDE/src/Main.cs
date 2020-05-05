// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
		public static string msbuildRoot = Path.Combine (sdkFolder, "3.1.201/");
#endif
		static string msbuildFolder;

		[STAThread]
		static void Main ()
		{
			configureDefaultSDKPathes ();

			msbuildRoot = Path.Combine (sdkFolder, msbuildFolder);

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

		static void configureDefaultSDKPathes ()
		{
			sdkFolder = Configuration.Global.Get<string> ("SDKFolder");
			if (string.IsNullOrEmpty (sdkFolder)) {
				switch (Environment.OSVersion.Platform) {
				case PlatformID.Win32S:
				case PlatformID.Win32Windows:
				case PlatformID.Win32NT:
				case PlatformID.WinCE:
					throw new NotSupportedException ();
				case PlatformID.Unix:
					sdkFolder = "/usr/share/dotnet/sdk";
					break;
				default:
					throw new NotSupportedException ();
				}
				Configuration.Global.Set ("SDKFolder", sdkFolder);
			}
			msbuildFolder = Configuration.Global.Get<string> ("msbuildFolder");
			if (!string.IsNullOrEmpty (msbuildFolder) && Directory.Exists(msbuildFolder))
				return;
			List<SDKVersion> versions = new List<SDKVersion> ();
			foreach (string dir in Directory.EnumerateDirectories (sdkFolder)) {
				string dirName = Path.GetFileName (dir);
				if (SDKVersion.TryParse (dirName, out SDKVersion vers))
					versions.Add (vers);
			}
			versions.Sort ((a, b) => a.ToInt.CompareTo (b));
			msbuildFolder = versions.Last ().ToString ();
			Configuration.Global.Set ("msbuildFolder", msbuildFolder);
		}
	}
	public class SDKVersion
	{
		public int major, minor, revision;
		public static bool TryParse (string versionString, out SDKVersion version) {
			version = null;
			if (string.IsNullOrEmpty (versionString))
				return false;
			string [] verNums = versionString.Split ('.');
			if (verNums.Length != 3)
				return false;
			if (!int.TryParse (verNums [0], out int maj))
				return false;
			if (!int.TryParse (verNums [1], out int min))
				return false;
			if (!int.TryParse (verNums [2], out int rev))
				return false;
			version = new SDKVersion { major = maj, minor = min, revision = rev };
			return true;
		}
		public long ToInt => major << 62 + minor << 60 + revision;
		public override string ToString () => $"{major}.{minor}.{revision}";
	}
}
