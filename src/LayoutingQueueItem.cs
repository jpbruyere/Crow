//
// LayoutingQueueItem.cs
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
	public struct LayoutingQueueItem
	{
		/// <summary> Instance of widget to be layouted</summary>
		public ILayoutable Layoutable;
		/// <summary> Bitfield containing the element of the layout to performs (x|y|width|height)</summary>
		public LayoutingType LayoutType;
		/// <summary> Unsuccessfull UpdateLayout and requeueing count </summary>
		public int LayoutingTries, DiscardCount;

		#region CTOR
		public LayoutingQueueItem (LayoutingType _layoutType, ILayoutable _graphicObject)
		{			
			LayoutType = _layoutType;
			Layoutable = _graphicObject;
			Layoutable.RegisteredLayoutings |= LayoutType;
			LayoutingTries = 0;
			DiscardCount = 0;
		}
		#endregion

		public void ProcessLayouting()
		{
			GraphicObject go = Layoutable as GraphicObject;

			go.parentRWLock.EnterReadLock ();

			if (go.Parent == null) {//TODO:improve this
				go.registeredLayoutings &= (~LayoutType);
				go.requestedLayoutings |= LayoutType;
				go.parentRWLock.ExitReadLock ();
				#if DBG_EVENTS
				Interface.DbgLog(DbgEvtType.DeleteLQI, "Parent is null", go);
				#endif
				return;
			}

			LayoutingTries++;
			if (!Layoutable.UpdateLayout (LayoutType)) {
				if (LayoutingTries < Interface.MaxLayoutingTries) {
					Layoutable.RegisteredLayoutings |= LayoutType;

					#if DBG_EVENTS
					Interface.DbgLog(DbgEvtType.RequeueLQI, LayoutType.ToString(), go);
					#endif

					(Layoutable as GraphicObject).IFace.LayoutingQueue.Enqueue (this);
				} else if (DiscardCount < Interface.MaxDiscardCount) {
					#if DBG_EVENTS
					Interface.DbgLog(DbgEvtType.DiscardLQI, LayoutType.ToString(), go);
					#endif
					go.LayoutingDiscardCheck (LayoutType);
					LayoutingTries = 0;
					DiscardCount++;
					Layoutable.RegisteredLayoutings |= LayoutType;
					(Layoutable as GraphicObject).IFace.DiscardQueue.Enqueue (this);
				}
				#if DBG_EVENTS
				else
					Interface.DbgLog(DbgEvtType.DeleteLQI, LayoutType.ToString(), go);
				#endif
			}
			#if DBG_EVENTS
			else
				Interface.DbgLog(DbgEvtType.SucceedLQI, LayoutType.ToString(), go);
			#endif

			go.parentRWLock.ExitReadLock ();
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
//		#if DEBUG_LAYOUTING
//		public List<LayoutingQueueItem> GetRootLQIs(){
//			return this.Where (lqi => lqi.wasTriggeredBy == null).ToList ();
//		}
//		#endif
	}
}

