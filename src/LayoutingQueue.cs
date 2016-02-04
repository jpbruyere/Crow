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
			_object.RegisteredLQINodes.Add(this.AddLast (new LayoutingQueueItem (_lt, _object)));
		}
		LinkedListNode<LayoutingQueueItem> searchLqi(ILayoutable go, LayoutingType lt){
			return go.RegisteredLQINodes.Where(n => n.Value.LayoutType == lt).LastOrDefault();
		}
		public void EnqueueAfterParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LinkedListNode<LayoutingQueueItem> parentLqi = searchLqi (_object.Parent, _lt);

			if (parentLqi == null)
				_object.RegisteredLQINodes.Add(this.AddFirst (lqi));
			else
				_object.RegisteredLQINodes.Add(this.AddAfter (parentLqi, lqi));
		}
		public void EnqueueBeforeParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LinkedListNode<LayoutingQueueItem> parentLqi = searchLqi (_object.Parent, _lt);

			if (parentLqi == null)
				_object.RegisteredLQINodes.Add(this.AddLast (lqi));
			else
				_object.RegisteredLQINodes.Add(this.AddBefore (parentLqi, lqi));
		}
		public void EnqueueAfterThisAndParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingType sizing = LayoutingType.Width;

			if (_lt == LayoutingType.Y)
				sizing = LayoutingType.Height;

			LinkedListNode<LayoutingQueueItem> parentLqi = searchLqi (_object.Parent, sizing);
			LinkedListNode<LayoutingQueueItem> thisLqi = searchLqi (_object, sizing);

			if (parentLqi == null) {
				if (thisLqi != null)
					_object.RegisteredLQINodes.Add(this.AddAfter (thisLqi, lqi));
				else
					_object.RegisteredLQINodes.Add(this.AddLast (lqi));
			} else {
				if (thisLqi == null)
					_object.RegisteredLQINodes.Add(this.AddAfter (parentLqi, lqi));
				else {
					switch (sizing) {
					case LayoutingType.Width:
						if (_object.Parent.getBounds().Width<0)
							_object.RegisteredLQINodes.Add(this.AddAfter (parentLqi, lqi));
						else
							_object.RegisteredLQINodes.Add(this.AddAfter (thisLqi, lqi));	
						break;
					case LayoutingType.Height:
						if (_object.Parent.getBounds().Height<0)
							_object.RegisteredLQINodes.Add(this.AddAfter (parentLqi, lqi));
						else
							_object.RegisteredLQINodes.Add(this.AddAfter (thisLqi, lqi));							
						break;
					}
				}
			}
		}
		public LayoutingQueueItem Dequeue()
		{
			LayoutingQueueItem tmp = this.First.Value;
			tmp.GraphicObject.RegisteredLQINodes.Remove(this.First);
			this.RemoveFirst ();
			return tmp;
		}
	}
}

