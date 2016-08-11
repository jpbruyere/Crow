//
//  IMLReader.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Xml;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

namespace Crow
{
	public class IMLInstantiatorBuilder : XmlTextReader
	{
		public Stream ImlStream;
		public Type RootType = null;

		DynamicMethod dm = null;
		public ILGenerator il = null;

		public IMLInstantiatorBuilder (Stream stream) 
			: base(stream)
		{
			ImlStream = stream;
		}
		public IMLInstantiatorBuilder (ILGenerator ilGen, string xmlFragment)
			: base(xmlFragment, XmlNodeType.Element,null){
			il = ilGen;
		}
		/// <summary>
		/// Inits il generator, RootType must have been read first
		/// </summary>
		public void InitEmitter(){

			dm = new DynamicMethod("dyn_instantiator",
				MethodAttributes.Family | MethodAttributes.FamANDAssem | MethodAttributes.NewSlot,
				CallingConventions.Standard,
				typeof(void),new Type[] {typeof(object)}, RootType, true);

			il = dm.GetILGenerator(256);
			il.DeclareLocal(typeof(GraphicObject));
			il.Emit(OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_0);
			il.Emit (OpCodes.Stloc_0);
		}

		/// <summary>
		/// read first node to set GraphicObject class for loading
		/// and let reader position on that node
		/// </summary>
		public Type ReadRootType ()
		{
			string root = "Object";
			while (Read()) {
				if (NodeType == XmlNodeType.Element) {
					root = this.Name;
					break;
				}
			}

			Type t = Type.GetType ("Crow." + root);
			if (t == null) {
				Assembly a = Assembly.GetEntryAssembly ();
				foreach (Type expT in a.GetExportedTypes ()) {
					if (expT.Name == root)
						t = expT;
				}
			}
			RootType = t;
			return t;
		}
		/// <summary>
		/// Finalize instatiator MSIL and return LoaderInvoker delegate
		/// </summary>
		public Instantiator GetInstanciator(){
			il.Emit(OpCodes.Ret);

			return new Instantiator (RootType,
				(Interface.LoaderInvoker)dm.CreateDelegate (typeof(Interface.LoaderInvoker)));
		}
	}
}

