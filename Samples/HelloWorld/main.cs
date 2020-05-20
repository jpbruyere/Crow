using System;
using System.Reflection;
using System.Reflection.Emit;
using Crow;
using Crow.IML;

namespace HelloWorld
{
	class Program {
		/*static void Main (string[] args) {
			using (Interface app = new Interface ()) {
				app.Initialized += (sender, e) => (sender as Interface).Load ("#HelloWorld.helloworld.crow");
				app.Run ();
			}
		}*/
		public static void testMethod (Group w, Widget z) {
			Console.WriteLine (w);
			Console.WriteLine (z);
		}
		static void Main (string[] args) {
			using (Interface app = new Interface ()) {
				Instantiator inst = new Instantiator (app);
				IMLContext ctx = new IMLContext (typeof (Widget));

				ctx.EmitCreateWidget (typeof (Group));
				MethodInfo mi = typeof (Program).GetMethod ("testMethod", BindingFlags.Static | BindingFlags.Public);
				ctx.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
				ctx.il.Emit (OpCodes.Castclass, typeof (Group));

				ctx.Emit (OpCodes.Dup);
				ctx.EmitCreateWidget (typeof (Widget));
				ctx.Emit (OpCodes.Ldloc_0);//load child on stack for parenting
								
				ctx.il.Emit (OpCodes.Call, mi);
				ctx.Emit (OpCodes.Stloc_0);

										   //ctx.Emit (OpCodes.Pop);
										   //ctx.Emit (OpCodes.Pop);

				InstanciatorInvoker del = ctx.CreateDelegate (inst);

				Widget w = (Widget)del (app);
				Console.WriteLine ("widget ok");
			}
		}

	}
}
