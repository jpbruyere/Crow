//
//  Context.cs
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
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using System.Xml;

namespace Crow.IML
{
	public class DataSourceBinding {
		public bool TwoWay;
		public MemberAddress Source;
		public string DataSourceMember;
	}
	/// <summary>
	/// Context while parsing IML
	/// </summary>
	public class Context
	{
		public XmlTextReader reader = null;
		public Type RootType = null;

		public DynamicMethod dm = null;
		public ILGenerator il = null;
		//public SubNodeType curSubNodeType;
		public NodeStack nodesStack = new NodeStack ();

		public Dictionary<string, List<NodeAddress>> Names  = new Dictionary<string, List<NodeAddress>>();

		public Dictionary<NodeAddress, Dictionary<string, List<MemberAddress>>> Bindings =
			new Dictionary<NodeAddress, Dictionary<string, List<MemberAddress>>>();
		//public List<DataSourceBinding> DataSourceBindings = new List<DataSourceBinding>();
		public Dictionary<NamedNodeAddress, Dictionary<string, MemberAddress>> NamedBindings =
			new Dictionary<NamedNodeAddress, Dictionary<string, MemberAddress>>();
		
		public Context (Type rootType)
		{
			RootType = rootType;
			dm = new DynamicMethod ("dyn_instantiator",
				typeof (void), new Type [] { typeof (Instantiator), typeof (object), typeof (Interface) }, true);
			il = dm.GetILGenerator (256);

			initILGen ();
		}

		public NodeAddress CurrentNodeAddress {
			get {
				Node[] n = nodesStack.ToArray ();
				Array.Reverse (n);
				return new NodeAddress(n);
			}
		}
		public Type CurrentNodeType {
			get { return nodesStack.Peek().CrowType; }
		}
		public void StorePropertyBinding(NodeAddress origNA, string origMember, NodeAddress destNA, string destMember){
			Dictionary<string, List<MemberAddress>> nodeBindings = null;
			if (Bindings.ContainsKey (origNA))
				nodeBindings = Bindings [origNA];
			else {
				nodeBindings = new Dictionary<string, List<MemberAddress>> ();
				Bindings [origNA] = nodeBindings;
			}

			if (!nodeBindings.ContainsKey (origMember))
				nodeBindings [origMember] = new List<MemberAddress> ();
			nodeBindings [origMember].Add (new MemberAddress (destNA, destMember));
		}
		public void StorePropertyBinding(BindingDefinition bindDef){
			StorePropertyBinding (bindDef.TargetNA, bindDef.TargetMember, bindDef.SourceNA, bindDef.SourceMember);
			if (bindDef.TwoWay)
				StorePropertyBinding (bindDef.SourceNA, bindDef.SourceMember, bindDef.TargetNA, bindDef.TargetMember);
		}
		void initILGen ()
		{
			il.DeclareLocal (typeof (GraphicObject));
			il.Emit (OpCodes.Nop);
			//set local GraphicObject to root object passed as 1st argument
			il.Emit (OpCodes.Ldarg_1);
			il.Emit (OpCodes.Stloc_0);
			CompilerServices.emitSetCurInterface (il);
		}

		public void emitCachedDelegateHandlerAddition(int index, EventInfo evt){
			il.Emit(OpCodes.Ldloc_0);//load ref to current graphic object
			il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			il.Emit(OpCodes.Ldfld, typeof(Instantiator).GetField("cachedDelegates", BindingFlags.Instance | BindingFlags.NonPublic));
			il.Emit(OpCodes.Ldc_I4, index);//load delegate index
			il.Emit(OpCodes.Callvirt, typeof(List<Delegate>).GetMethod("get_Item", new Type[] { typeof(Int32) }));
			il.Emit(OpCodes.Callvirt, evt.AddMethod);//call add event
		}
	}
}