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

namespace Crow
{
	public class LayoutingQueue : List<LayoutingQueueItem>
	{
		public LayoutingQueue ()
		{
		}
		public void Enqueue(LayoutingType _lt, ILayoutable _object)
		{
			this.Add (new LayoutingQueueItem (_lt, _object));
		}
		LayoutingQueueItem searchLqi(ILayoutable go, LayoutingType lt){
			return go.RegisteredLQIs.Where(lq => lq.LayoutType == lt).LastOrDefault();
		}
		public void EnqueueAfterParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingQueueItem parentLqi = searchLqi (_object.Parent, _lt);

			if (parentLqi == null)
				this.Insert (0, lqi);
			else
				this.Insert (this.IndexOf (parentLqi) + 1, lqi);
		}
		public void EnqueueBeforeParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingQueueItem parentLqi = searchLqi (_object.Parent, _lt);

			if (parentLqi == null)
				this.Add (lqi);
			else
				this.Insert (this.IndexOf (parentLqi), lqi);
		}
		public void EnqueueAfterThisAndParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingType sizing = LayoutingType.Width;

			if (_lt == LayoutingType.Y)
				sizing = LayoutingType.Height;

			LayoutingQueueItem parentLqi = searchLqi (_object.Parent, sizing);
			LayoutingQueueItem thisLqi = searchLqi (_object, sizing);
			int idx = -1;

			if (parentLqi == null) {
				if (thisLqi != null)
					idx = this.IndexOf (thisLqi);
			} else {
				if (thisLqi == null)
					idx = this.IndexOf (parentLqi);
				else
					idx = Math.Max(this.IndexOf (parentLqi), this.IndexOf (thisLqi));				
			}

			this.Insert (idx + 1, lqi);			
		}
		public LayoutingQueueItem Dequeue()
		{
			LayoutingQueueItem tmp = this [0];
			tmp.DeleteLayoutableRef ();
			this.RemoveAt (0);
			return tmp;
		}
	}
}

