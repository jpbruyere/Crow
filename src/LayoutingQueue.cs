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

namespace go
{
	public class LayoutingQueue : List<LayoutingQueueItem>
	{
		public LayoutingQueue ()
		{
		}
		public void Enqueue(LayoutingType _lt, ILayoutable _object)
		{
			Interface.LayoutingQueue.RemoveAll(lq => lq.GraphicObject == _object && lq.LayoutType == _lt);
			Interface.LayoutingQueue.Add (new LayoutingQueueItem (_lt, _object));
		}

		public void EnqueueAfterParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			int idxParentSz = Interface.LayoutingQueue.IndexOf 
				(Interface.LayoutingQueue.Where(lq => lq.GraphicObject == _object.Parent && lq.LayoutType == _lt).LastOrDefault());

			Interface.LayoutingQueue.Insert (idxParentSz + 1, lqi);			
		}
		public void EnqueueBeforeParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			int idxParentSz = Interface.LayoutingQueue.IndexOf 
				(Interface.LayoutingQueue.Where(lq => lq.GraphicObject == _object.Parent && lq.LayoutType == _lt).FirstOrDefault());

			if (idxParentSz < 0)
				Interface.LayoutingQueue.Enqueue (_lt, _object);
			else
				Interface.LayoutingQueue.Insert (idxParentSz, lqi);			
		}
		public void EnqueueAfterThisAndParentSizing (LayoutingType _lt, ILayoutable _object)
		{
			LayoutingQueueItem lqi = new LayoutingQueueItem (_lt, _object);
			LayoutingType sizing = LayoutingType.Width;

			if (_lt == LayoutingType.Y)
				sizing = LayoutingType.Height;
				
			int idxW = Interface.LayoutingQueue.IndexOf (Interface.LayoutingQueue.Where
				(lq => (lq.GraphicObject == _object.Parent || lq.GraphicObject == _object) && lq.LayoutType == sizing).LastOrDefault());

			Interface.LayoutingQueue.Insert (idxW + 1, lqi);			
		}

		public LayoutingQueueItem Dequeue()
		{
			LayoutingQueueItem tmp = this [0];
			this.RemoveAt (0);
			return tmp;
		}
	}
}

