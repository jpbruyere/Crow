// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Crow
{
	public static class Logger
	{
		[Flags]
		public enum LogType
		{
			Info		= 1,
			Warning		= 2,
			Error		= 4,
		}

		public static LogType CurrentLogLevel = LogType.Error;

		static Stopwatch timer = Stopwatch.StartNew ();


		public static void LOG(string message = null, [CallerMemberName] string caller = null) {
			Console.WriteLine ($"{timer.ElapsedMilliseconds, 10} {caller}: {message}");
		}
	}
}
