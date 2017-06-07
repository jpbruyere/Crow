using System;
using System.Runtime.CompilerServices;
using System.ComponentModel;

class MonoEmbed {
	[MethodImplAttribute(MethodImplOptions.InternalCall)]
	unsafe extern static string gimme();

	static void Main() {		
		Console.WriteLine (gimme ());
	}
}
