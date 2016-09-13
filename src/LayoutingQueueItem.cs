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
		public Stopwatch LQITime = new Stopwatch();
		public LQIList triggeredLQIs = new LQIList();
		public LayoutingQueueItem wasTriggeredBy = null;
		public GraphicObject graphicObject {
			get { return Layoutable as GraphicObject; }
		}
		public string Name {
			get { return graphicObject.Name; }
		}
		public string FullName {
			get { return graphicObject.ToString(); }
		}
		public Measure Width {
			get { return graphicObject.Width; }
		}
		public Measure Height {
			get { return graphicObject.Height; }
		}
		public bool HasTriggeredLQIs { get { return triggeredLQIs.Count > 0; }}
		public Rectangle Slot, NewSlot;
		#endif

		#region CTOR
		public LayoutingQueueItem (LayoutingType _layoutType, ILayoutable _graphicObject)
		{
			LayoutType = _layoutType;
			Layoutable = _graphicObject;
			Layoutable.RegisteredLayoutings |= LayoutType;
			#if DEBUG_LAYOUTING
			GraphicObject g = graphicObject;
			g.CurrentInterface.curLQIs.Add(this);
			if (g.CurrentInterface.currentLQI != null){
				wasTriggeredBy = g.CurrentInterface.currentLQI;
				g.CurrentInterface.currentLQI.triggeredLQIs.Add(this);
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
			LQITime.Start();
			#endif
			LayoutingTries++;
			if (!Layoutable.UpdateLayout (LayoutType)) {
				if (LayoutingTries < Interface.MaxLayoutingTries) {
					Layoutable.RegisteredLayoutings |= LayoutType;
					(Layoutable as GraphicObject).CurrentInterface.LayoutingQueue.Enqueue (this);
				} else if (DiscardCount < Interface.MaxDiscardCount) {
					LayoutingTries = 0;
					DiscardCount++;
					Layoutable.RegisteredLayoutings |= LayoutType;
					(Layoutable as GraphicObject).CurrentInterface.DiscardQueue.Enqueue (this);
				}
				#if DEBUG_LAYOUTING
				else
					Debug.WriteLine ("\tDELETED    => " + this.ToString ());
				#endif
			}
			#if DEBUG_LAYOUTING
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
	public class LQIList : List<LayoutingQueueItem>{
		#if DEBUG_LAYOUTING
		public List<LayoutingQueueItem> GetRootLQIs(){
			return this.Where (lqi => lqi.wasTriggeredBy == null).ToList ();
		}
		#endif
	}
}

