// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

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

		#if DEBUG_LOG
		public enum Result : byte {
			Unknown,
			Register,
			Success,
			Requeued,
			Discarded,
			Deleted,
		}
		public Result result;
		public Widget graphicObject {
			get { return Layoutable as Widget; }
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
		public Rectangle Slot, NewSlot;
		#endif

		#region CTOR
		public LayoutingQueueItem (LayoutingType _layoutType, ILayoutable _graphicObject)
		{			
			LayoutType = _layoutType;
			Layoutable = _graphicObject;
			Layoutable.RegisteredLayoutings |= LayoutType;
			LayoutingTries = 0;
			DiscardCount = 0;
			#if DEBUG_LOG
			Slot = Rectangle.Zero;
			NewSlot = Rectangle.Zero;
			result = Result.Register;
			DebugLog.AddEvent (DbgEvtType.GORegisterLayouting, this);
			#endif
		}
		#endregion


		public void ProcessLayouting()
		{
			Widget go = Layoutable as Widget;
//			if (go == null) {
//				Debug.WriteLine ("ERROR: processLayouting on something else than a graphic object: " + this.ToString ());
//				return;
//			}

			go.parentRWLock.EnterReadLock ();

			if (go.Parent == null) {//TODO:improve this
				//cancel layouting for object without parent, maybe some were in queue when
				//removed from a listbox
				#if DEBUG_LOG
				DebugLog.AddEvent (DbgEvtType.GOProcessLayoutingWithNoParent, this);
				#endif
				go.parentRWLock.ExitReadLock ();
				return;
			}
			#if DEBUG_LOG
			DbgEvent dbgEvt = DebugLog.AddEvent (DbgEvtType.GOProcessLayouting, this);
			#endif
			LayoutingTries++;
			if (!Layoutable.UpdateLayout (LayoutType)) {
				#if DEBUG_LOG
				dbgEvt.end = DebugLog.chrono.ElapsedTicks;
				#endif
				if (LayoutingTries < Interface.MaxLayoutingTries) {
					Layoutable.RegisteredLayoutings |= LayoutType;
					(Layoutable as Widget).IFace.LayoutingQueue.Enqueue (this);
					#if DEBUG_LOG
					result = Result.Requeued;
					#endif
				} else if (DiscardCount < Interface.MaxDiscardCount) {
					#if DEBUG_LOG
					result = Result.Discarded;
					#endif
					LayoutingTries = 0;
					DiscardCount++;
					Layoutable.RegisteredLayoutings |= LayoutType;
					(Layoutable as Widget).IFace.DiscardQueue.Enqueue (this);
				}
				#if DEBUG_LOG
				else {
					result = Result.Deleted;
				}
				#endif
			}
			#if DEBUG_LOG
			else{
				result = Result.Success;
			}
			dbgEvt.data = this;
			#endif
			go.parentRWLock.ExitReadLock ();
		}

		public static implicit operator Widget(LayoutingQueueItem queueItem)
		{
			return queueItem.Layoutable as Widget;
		}
		public static implicit operator LayoutingType(LayoutingQueueItem lqi)
		{
			return lqi.LayoutType;
		}
		public override string ToString ()
		{
			#if DEBUG_LAYOUTING
			return string.Format ("{2};{3} {1}->{0}", LayoutType,Layoutable.ToString(),
				LayoutingTries,DiscardCount);
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

