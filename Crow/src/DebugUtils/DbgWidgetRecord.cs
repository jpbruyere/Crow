// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Linq;

namespace Crow.DebugLogger
{
	/// <summary>
	/// Recorded Widget instance data.
	/// </summary>
	public class DbgWidgetRecord : DbgEventSource
	{
		public DbgWidgetRecord ()
		{
			 Events = new List<DbgEvent> ();
		}
		public int listIndex;//prevent doing an IndexOf on list for each event to know y pos on screen
							 //public int instanceNum;//class instantiation order, used to bind events to objs
		public string name;
		//0 is the main graphic tree, for other obj tree not added to main tree, it range from 1->n
		//useful to track events for obj shown later, not on start
		public int treeIndex;
		public int yIndex;//index in parenting, the whole main graphic tree is one continuous suite
		public int xLevel;//depth
		public String Width;
		public String Height;

		public static DbgWidgetRecord Parse (string str)
		{
			DbgWidgetRecord g = new DbgWidgetRecord ();
			if (str == null)
				return null;
			string [] tmp = str.Trim ().Split (';');
			g.name = tmp [0];
			g.yIndex = int.Parse (tmp [1]);
			g.xLevel = int.Parse (tmp [2]);
			g.Width = tmp [3];
			g.Height = tmp [4];
			return g;
		}

		public List<DbgWidgetEvent> RootEvents => Events.OfType<DbgWidgetEvent> ().Where (
			e => (e.parentEvent != null && !e.parentEvent.type.HasFlag (DbgEvtType.Widget)) ||
			(e.parentEvent is DbgWidgetEvent w && w.InstanceIndex != e.InstanceIndex)).ToList();

		public override string ToString () => $"{name}";
	}
}