//
//  LayoutingQueue.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
//
//  Copyright (c) 2015 jp
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
using System.Linq;
using System.Diagnostics;

namespace Crow
{
	public class LayoutingQueue : LinkedList<LayoutingQueueItem>
	{
		public LayoutingQueue ()
		{
		}
		public void Enqueue(LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			lqi.Node = this.AddLast (lqi);
		}
		LayoutingQueueItem searchLqi(ILayoutable go, LayoutingType lt){
			return go.RegisteredLQIs.Where(lq => lq.LayoutType == lt).LastOrDefault();
		}
		public void EnqueueAfterParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingQueueItem parentLqi = searchLqi (_object.Parent, _lt);

			if (parentLqi == null)
				lqi.Node = this.AddFirst (lqi);
			else
				lqi.Node = this.AddAfter (parentLqi.Node, lqi);
		}
		public void EnqueueBeforeParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingQueueItem parentLqi = searchLqi (_object.Parent, _lt);

			if (parentLqi == null)
				lqi.Node = this.AddLast (lqi);
			else
				lqi.Node = this.AddBefore (parentLqi.Node, lqi);
		}
		public void EnqueueAfterThisAndParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingType sizing = LayoutingType.Width;

			if (_lt == LayoutingType.Y)
				sizing = LayoutingType.Height;

			LayoutingQueueItem parentLqi = searchLqi (_object.Parent, sizing);
			LayoutingQueueItem thisLqi = searchLqi (_object, sizing);

			if (parentLqi == null) {
				if (thisLqi != null)
					lqi.Node = this.AddAfter (thisLqi.Node, lqi);
				else
					lqi.Node = this.AddLast (lqi);
			} else {
				if (thisLqi == null)
					lqi.Node = this.AddAfter (parentLqi.Node, lqi);
				else {
					switch (sizing) {
					case LayoutingType.Width:
						if (_object.Parent.getBounds().Width<0)
							lqi.Node = this.AddAfter (parentLqi.Node, lqi);
						else
							lqi.Node = this.AddAfter (thisLqi.Node, lqi);							
						break;
					case LayoutingType.Height:
						if (_object.Parent.getBounds().Height<0)
							lqi.Node = this.AddAfter (parentLqi.Node, lqi);
						else
							lqi.Node = this.AddAfter (thisLqi.Node, lqi);							
						break;
					}
				}
			}
		}
		public LayoutingQueueItem Dequeue()
		{
			LayoutingQueueItem tmp = this.First.Value;
			tmp.DeleteLayoutableRef ();
			this.RemoveFirst ();
			return tmp;
		}
	}
}

