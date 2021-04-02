using System.Threading;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.Loader;

namespace CrowDebugger
{
	class Program
	{
		const int BUFF_MAX = 1024;
		static void Main(string[] args)
		{
			AssemblyLoadContext crowLoadCtx = new AssemblyLoadContext("CrowDebuggerLoadContext");
			Console.WriteLine("Crow Debugger started.");
			char[] buffer = new char[1024];
			int buffPtr = 0;
			while (true)
			{				
				if (Console.KeyAvailable)
				{
					ConsoleKeyInfo key = Console.ReadKey(true);
					if (key.Key == ConsoleKey.Enter) {
						Span<char> cmd = buffer.AsSpan(0, buffPtr);
						Console.WriteLine($"=> {cmd.ToString()}");
						if (cmd[0] == 'q' || cmd.SequenceEqual ("quit"))
							break;
						
						if (cmd.StartsWith("load")) {
							using(crowLoadCtx.EnterContextualReflection()) {
								string str = cmd.Slice(5).ToString();
								Console.WriteLine($"[msg]:Trying to load crow dll from: '{str}'");
								if (!File.Exists (str)) {
									Console.WriteLine ($"[error]:File not found: {str}");
								} else {
									Assembly crowAssembly = crowLoadCtx.LoadFromAssemblyPath (str);
									if (crowAssembly == null) {
										Console.WriteLine($"[error]:Failed to load crow dll from: {str}");
									} else {
										Console.WriteLine($"[ok]:Crow Assembly loaded:{crowAssembly.FullName}");
										
									}
								}
							}
						} else
							Console.WriteLine($"=> {cmd.ToString()}");
						buffPtr = 0;
						continue;
					}
						
					buffer[buffPtr++] = key.KeyChar;
				}
			}

			Console.WriteLine("exited");
		}
	}
}
