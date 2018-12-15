//
// Context.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
// Copyright (c) 2013-2017 Jean-Philippe Bruyère
//
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

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
	/// Context while parsing IML, this will store what's needed only while parsing and not during instancing
	/// </summary>
	public class IMLContext
	{
		public XmlReader reader = null;
		public int curLine = 0;
		public Type RootType = null;

		public DynamicMethod dm = null;
		public ILGenerator il = null;
		//public SubNodeType curSubNodeType;
		public NodeStack nodesStack = new NodeStack ();

		/// <summary> store addresses of named node for name resolution at end of parsing </summary>
		public Dictionary<string, List<NodeAddress>> Names  = new Dictionary<string, List<NodeAddress>>();
		/// <summary> Store non datasource binding (in tree and template) by origine and orig member </summary>
		public Dictionary<NodeAddress, Dictionary<string, List<MemberAddress>>> Bindings =
			new Dictionary<NodeAddress, Dictionary<string, List<MemberAddress>>>();
		/// <summary> Store binding with name in target, will be resolved at end of parsing </summary>
		public List<BindingDefinition> UnresolvedTargets = new List<BindingDefinition>();


		public IMLContext (Type rootType)
		{
			RootType = rootType;
			dm = new DynamicMethod ("dyn_instantiator",
				typeof (object), new Type [] { typeof (Instantiator), typeof (Interface) }, true);
			il = dm.GetILGenerator (256);

			il.DeclareLocal (typeof (GraphicObject));
			il.Emit (OpCodes.Nop);
			//set local GraphicObject to root object
			ConstructorInfo ci = rootType.GetConstructor (
					BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public, 
					null, Type.EmptyTypes, null);
			if (ci == null)
				throw new Exception ("No default parameterless constructor found in " + rootType.Name);			
			il.Emit (OpCodes.Newobj, ci);
			il.Emit (OpCodes.Stloc_0);
			CompilerServices.emitSetCurInterface (il);
		}

		Type curDataSourceType = null;
		/// <summary>
		/// Pushs  new node and set datasourcetype to current ds type
		/// </summary>
		/// <param name="crowType">Crow type.</param>
		/// <param name="_index">Index.</param>
		public void PushNode (Type crowType, int _index = 0) {
			nodesStack.Push (new Node (crowType, _index, curDataSourceType));
		}
		/// <summary>
		/// Pops node and set curDS type to previous one in node on top of the stack
		/// </summary>
		/// <returns>The node.</returns>
		public Node PopNode () {
			Node n = nodesStack.Pop ();
			if (nodesStack.Count > 0)
				curDataSourceType = nodesStack.Peek().DataSourceType;
			return n;
		}
		public bool CurrentNodeHasDataSourceType {
			get { return curDataSourceType != null; }
		}
		public Type CurrentDataSourceType {
			get { return curDataSourceType; }
		}
		public void SetDataSourceTypeForCurrentNode (Type dsType)
		{
			Node n = nodesStack.Pop ();
			n.DataSourceType = dsType;
			nodesStack.Push (n);
			curDataSourceType = dsType;
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
			if (bindDef.HasUnresolvedTargetName) {
				UnresolvedTargets.Add (bindDef);
				return;
			}
			StorePropertyBinding (bindDef.TargetNA, bindDef.TargetMember, bindDef.SourceNA, bindDef.SourceMember);
			if (bindDef.TwoWay)
				StorePropertyBinding (bindDef.SourceNA, bindDef.SourceMember, bindDef.TargetNA, bindDef.TargetMember);
		}
		/// <summary>
		/// Stores all the names found in current iml for binding resolution if any of them
		/// are targeting named widget 
		/// </summary>
		public void StoreCurrentName(string name){
			if (!Names.ContainsKey(name))
				Names[name] = new List<NodeAddress>();
			Names[name].Add(CurrentNodeAddress);
		}
		public void ResolveNamedTargets(){//TODO:methodinfo fetching is redundant with early parsing
			foreach (BindingDefinition bd in UnresolvedTargets) {
				if (bd.HasUnresolvedTargetName) {
					try {
						ResolveName (bd);	
					} catch (Exception ex) {
						System.Diagnostics.Debug.WriteLine (ex.ToString ());
						continue;
					}
				}

				if (bd is EventBinding) {
					emitHandlerMethodAddition (bd as EventBinding);
					continue;
				}

				MemberInfo miSource = bd.SourceMemberAddress.member;
				if (miSource == null)
					throw new Exception ("Source member '" + bd.SourceMember + "' not found");
				StorePropertyBinding (bd);
			}
		}
		public void ResolveName (BindingDefinition bd){

			if (!Names.ContainsKey (bd.TargetName))
				throw new Exception ("Target Name '" + bd.TargetName + "' not found");

			NodeAddress resolvedNA = null;
			foreach (NodeAddress na in Names[bd.TargetName]) {
				bool naMatch = true;
				for (int i = 0; i < bd.TargetNA.Count; i++) {
					if (bd.TargetNA [i] != na [i]) {
						naMatch = false;
						break;
					}
				}
				if (naMatch) {
					resolvedNA = na;
					break;
				}
			}

			if (resolvedNA == null)
				throw new Exception ("Target Name '" + bd.TargetName + "' not found");

			bd.ResolveTargetName (resolvedNA);
		}

		/// <summary>
		/// Emits cached delegate handler addition in the context of instantiator (ctx)
		/// </summary>
		public void emitCachedDelegateHandlerAddition(int index, EventInfo evt, NodeAddress address = null){
			il.Emit(OpCodes.Ldloc_0);//load ref to current graphic object
			CompilerServices.emitGetInstance (il, address);
			il.Emit(OpCodes.Ldarg_0);//load ref to this instanciator onto the stack
			il.Emit(OpCodes.Ldfld, CompilerServices.fiCachedDel);
			il.Emit(OpCodes.Ldc_I4, index);//load delegate index
			il.Emit(OpCodes.Callvirt, CompilerServices.miGetDelegateListItem);
			il.Emit(OpCodes.Call, evt.AddMethod);//call add event
		}
		/// <summary>
		/// Emits the handler method addition, done at end of parsing, Loc_0 is root node instance
		/// </summary>
		/// <param name="bd">Bd.</param>
		public void emitHandlerMethodAddition(EventBinding bd){

			//fetch source instance with address for handler addition (as 1st arg of handler.add)
			il.Emit (OpCodes.Ldloc_0);//push root
			CompilerServices.emitGetInstance (il, bd.SourceNA);

			il.Emit (OpCodes.Ldloc_0);
			CompilerServices.emitGetInstance (il, bd.TargetNA);

			string[] membs = bd.TargetMember.Split ('.');
			for (int i = 0; i < membs.Length - 1; i++) {
				il.Emit (OpCodes.Dup);
				il.Emit (OpCodes.Ldstr, membs[i]);
				il.Emit (OpCodes.Call, CompilerServices.miGetMembIinfoWithRefx);
				il.Emit (OpCodes.Call, CompilerServices.miGetValWithRefx);
			}

			//load handlerType of sourceEvent to create handler delegate (1st arg)
			il.Emit (OpCodes.Ldtoken, bd.SourceEvent.EventHandlerType);
			il.Emit (OpCodes.Call, CompilerServices.miGetTypeFromHandle);
			//load methodInfo (3rd arg)
			il.Emit (OpCodes.Ldstr, membs[membs.Length-1]);
			il.Emit (OpCodes.Call, CompilerServices.miCreateDel);
			il.Emit (OpCodes.Call, bd.SourceEvent.AddMethod);//call add event
		}
//		public void emitHandlerMethodAddition(EventBinding bd){
//			//fetch source instance with address for handler addition (as 1st arg of handler.add)
//			il.Emit (OpCodes.Ldloc_0);//push root
//			CompilerServices.emitGetInstance (il, bd.SourceNA);
//
//			//load handlerType of sourceEvent to create handler delegate (1st arg)
//			il.Emit (OpCodes.Ldtoken, bd.SourceEvent.EventHandlerType);
//			il.Emit (OpCodes.Call, CompilerServices.miGetTypeFromHandle);
//			//load target the where the method is defined (2nd arg)
//			il.Emit (OpCodes.Ldloc_0);
//			CompilerServices.emitGetInstance (il, bd.TargetNA);
//			//load methodInfo (3rd arg)
//			il.Emit (OpCodes.Ldstr, bd.TargetMember);
//
//			il.Emit (OpCodes.Callvirt, CompilerServices.miCreateDel);
//
//			il.Emit (OpCodes.Callvirt, bd.SourceEvent.AddMethod);//call add event
//		}
//
	}
}