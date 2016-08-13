//
//  LayoutingQueueItem.cs
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
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;

namespace Crow
{
	[Flags]
	public enum LayoutingType : byte
	{
		None = 0x00,
		X = 0x01,
		Y = 0x02,
		Positioning = 0x03,
		Width = 0x04,
		Height = 0x08,
		Sizing = 0x0C,
		ArrangeChildren = 0x10,
		All = 0xFF
	}
	/// <summary>
	/// Element class of the LayoutingQueue
	/// </summary>
	public class LayoutingQueueItem
	{
		/// <summary> Instance of widget to be layouted</summary>
		public ILayoutable Layoutable;
		/// <summary> Bitfield containing the element of the layout to performs (x|y|width|height)</summary>
		public LayoutingType LayoutType;
		/// <summary> Unsuccessfull UpdateLayout and requeueing count </summary>
		public int LayoutingTries, DiscardCount;

		#if DEBUG_LAYOUTING
		public static List<LayoutingQueueItem> processedLQIs = new List<LayoutingQueueItem>();
		public static LayoutingQueueItem[] MultipleRunsLQIs {
			get { return processedLQIs.Where(l=>l.LayoutingTries>2 || l.DiscardCount > 0).ToArray(); }
		}
		public static LayoutingQueueItem currentLQI = null;
		public Stopwatch LQITime = new Stopwatch();
		public List<LayoutingQueueItem> triggeredLQIs = new List<LayoutingQueueItem>();
		public LayoutingQueueItem wasTriggeredBy = null;
		public GraphicObject graphicObject {
			get { return Layoutable as GraphicObject; }
		}
		public Rectangle Slot, NewSlot;
		#endif

		#region CTOR
		public LayoutingQueueItem (LayoutingType _layoutType, ILayoutable _graphicObject)
		{
			LayoutType = _layoutType;
			Layoutable = _graphicObject;
			Layoutable.RegisteredLayoutings |= LayoutType;
			#if DEBUG_LAYOUTING
			if (graphicObject.CurrentDrawLQIs == null)
				graphicObject.CurrentDrawLQIs = new List<LayoutingQueueItem>();
			graphicObject.CurrentDrawLQIs.Add(this);
			if (currentLQI != null){
				wasTriggeredBy = currentLQI;
				currentLQI.triggeredLQIs.Add(this);
			}
			#endif
		}
		#endregion


		public void ProcessLayouting()
		{
			if (Layoutable.Parent == null) {//TODO:improve this
				//cancel layouting for object without parent, maybe some were in queue when
				//removed from a listbox
				Debug.WriteLine ("ERROR: processLayouting, no parent for: " + this.ToString ());
				return;
			}
			#if DEBUG_LAYOUTING
			currentLQI = this;
			processedLQIs.Add(this);
			LQITime.Start();
			#endif
			if (!Layoutable.UpdateLayout (LayoutType)) {
				if (LayoutingTries < Interface.MaxLayoutingTries) {
					LayoutingTries++;
					Layoutable.RegisteredLayoutings |= LayoutType;
					Interface.CurrentInterface.LayoutingQueue.Enqueue (this);
				} else if (DiscardCount < Interface.MaxDiscardCount) {
					LayoutingTries = 0;
					DiscardCount++;
					Layoutable.RegisteredLayoutings |= LayoutType;
					Interface.CurrentInterface.DiscardQueue.Enqueue (this);
				}
				#if DEBUG_LAYOUTING
				else
					Debug.WriteLine ("\tDELETED    => " + this.ToString ());
				#endif
			}
			#if DEBUG_LAYOUTING
			currentLQI = null;
			LQITime.Stop();
			#endif
		}

		public static implicit operator GraphicObject(LayoutingQueueItem queueItem)
		{
			return queueItem.Layoutable as GraphicObject;
		}
		public static implicit operator LayoutingType(LayoutingQueueItem lqi)
		{
			return lqi.LayoutType;
		}
		public override string ToString ()
		{
			#if DEBUG_LAYOUTING
			return string.Format ("{2};{3};{4} {1}->{0}", LayoutType,Layoutable.ToString(),
				LayoutingTries,DiscardCount,LQITime.ElapsedTicks);
			#else
			return string.Format ("{2};{3} {1}->{0}", LayoutType,Layoutable.ToString(),
				LayoutingTries, DiscardCount);
			#endif
		}
	}
}

