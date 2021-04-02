using System.Threading;
using System;
using System.Runtime.Loader;
using System.Reflection;

namespace CrowDebugger
{
	public class CrowDebugAssemblyLoadContext : AssemblyLoadContext
	{
		protected override Assembly Load(AssemblyName assemblyName)
		{
			return base.Load(assemblyName);
		}
	}
}