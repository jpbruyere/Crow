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

namespace Crow
{
	[Flags]
	public enum LayoutingType : byte
	{
		None = 0x00,
		X = 0x01,
		Y = 0x02,
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
		public ILayoutable GraphicObject;
		/// <summary> Bitfield containing the element of the layout to performs (x|y|width|height)</summary>
		public LayoutingType LayoutType;
		/// <summary> Unsuccessfull UpdateLayout and requeueing count </summary>
		public int LayoutingTries;

		#region CTOR
		public LayoutingQueueItem (LayoutingType _layoutType, ILayoutable _graphicObject)
		{
			LayoutType = _layoutType;
			GraphicObject = _graphicObject;
			GraphicObject.RegisteredLayoutings |= LayoutType;
		}
		#endregion


		public void ProcessLayouting()
		{
			if (GraphicObject.Parent == null) {//TODO:improve this
				//cancel layouting for object without parent, maybe some were in queue when
				//removed from a listbox
				Debug.WriteLine ("ERROR: processLayouting, no parent for: " + this.ToString ());
				return;
			}
			#if DEBUG_LAYOUTING
			Debug.WriteLine ("Layouting => " + this.ToString ());
			#endif
			if (!GraphicObject.UpdateLayout (LayoutType)) {
				#if DEBUG_LAYOUTING
				Debug.WriteLine ("\tRequeuing => " + this.ToString ());
				#endif
				LayoutingTries ++;
				if (LayoutingTries < Interface.MaxLayoutingTries) {
					GraphicObject.RegisteredLayoutings |= LayoutType;
					Interface.CurrentInterface.LayoutingQueue.Enqueue (this);
				}
			} else
				LayoutingTries = 0;
		}

		public static implicit operator GraphicObject(LayoutingQueueItem queueItem)
		{
			return queueItem.GraphicObject as GraphicObject;
		}
		public static implicit operator LayoutingType(LayoutingQueueItem lqi)
		{
			return lqi.LayoutType;
		}
		public override string ToString ()
		{
			return string.Format ("{1}->{0}", LayoutType,GraphicObject.ToString());
		}
	}
}

