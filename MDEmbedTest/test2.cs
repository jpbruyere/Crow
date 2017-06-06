using System;
using System.Runtime.CompilerServices;

class MonoEmbed {
	[MethodImplAttribute(MethodImplOptions.InternalCall)]
	unsafe extern static string gimme();

	static void Main() {		
		Console.WriteLine ("test2 => " + gimme ());

	}
}	
