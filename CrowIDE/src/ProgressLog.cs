// Copyright (c) 2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using Microsoft.CodeAnalysis.MSBuild;

namespace Crow.Coding
{
	public class ProgressLog : IProgress<ProjectLoadProgress>
	{
		public void Report (ProjectLoadProgress value)
		{
			Console.WriteLine ($"{value.ElapsedTime} {value.Operation} {value.TargetFramework}");
		}
	}
}
