// Copyright (c) 2013-2020  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Threading;

namespace Crow
{
	public class DbgEvent : DbgEventSource
	{
		public long begin, end;
		public int threadId;
		public DbgEvtType type;
		public DbgEvent parentEvent;
		public bool HasChildEvents => Events != null && Events.Count > 0;
		public override long Duration => end - begin;
		public virtual bool IsWidgetEvent => false;
		public virtual bool IsLayoutEvent => false;

		bool isSelected;
		bool isExpanded;
		Widget listContainer;

		public bool IsSelected {
			get => isSelected;
			set {
				if (isSelected == value)
					return;
				isSelected = value;
				NotifyValueChangedAuto (isSelected);
			}
		}
		public bool IsExpanded {
			get => isExpanded;
			set {
				if (isExpanded == value)
					return;
				isExpanded = value;
				if (isExpanded && parentEvent != null)
					parentEvent.IsExpanded = true;
				NotifyValueChangedAuto (isExpanded);
			}
		}
		public Widget ListContainer {
			get => listContainer;
			set {
				if (listContainer == value)
					return;
				listContainer = value;
				NotifyValueChangedAuto (listContainer);
			}
		}

		public virtual Color Color {
			get {
				switch (type) {
				case DbgEvtType.Layouting:
					return Colors.Yellow;
				case DbgEvtType.Clipping:
					return Colors.DarkTurquoise;
				case DbgEvtType.Drawing:
					return Colors.MidnightBlue;
				case DbgEvtType.Update:
					return Colors.Grey;
				case DbgEvtType.IFaceLoad:
					return Colors.Teal;
				default:
					return Colors.White;
				}

			}
		}

		public void AddEvent (DbgEvent evt)
		{
			if (Events == null)
				Events = new List<DbgEvent> () { evt };
			else
				Events.Add (evt);
			evt.parentEvent = this;
		}

		public DbgEvent () { }
		public DbgEvent (long timeStamp, DbgEvtType evt)
		{
			type = evt;
			begin = timeStamp;
			end = timeStamp;
			threadId = Thread.CurrentThread.ManagedThreadId;
		}

		public static DbgEvent Parse (string str)
		{
			if (str == null)
				return null;
			string [] tmp = str.Trim ().Split (';');

			DbgEvtType evtType = (DbgEvtType)Enum.Parse (typeof (DbgEvtType), tmp [3]);

			if (evtType.HasFlag (DbgEvtType.Widget)) {
				if (evtType.HasFlag (DbgEvtType.Layouting))
					return new DbgLayoutEvent () {
						begin = long.Parse (tmp [0]),
						end = long.Parse (tmp [1]),
						threadId = int.Parse (tmp [2]),
						type = evtType,
						InstanceIndex = int.Parse (tmp [4]),
						layouting = (LayoutingType)Enum.Parse (typeof (LayoutingType), tmp [5]),
						result = evtType == DbgEvtType.GOProcessLayouting ?
							(LayoutingQueueItem.Result)Enum.Parse (typeof (LayoutingQueueItem.Result), tmp [6])
							: LayoutingQueueItem.Result.Unknown,
						OldSlot = Rectangle.Parse (tmp [7]),
						NewSlot = Rectangle.Parse (tmp [8]),
					};
				return new DbgWidgetEvent () {
					begin = long.Parse (tmp [0]),
					end = long.Parse (tmp [1]),
					threadId = int.Parse (tmp [2]),
					type = evtType,
					InstanceIndex = int.Parse (tmp [4]),
				};
			}
			return new DbgEvent () {
				begin = long.Parse (tmp [0]),
				end = long.Parse (tmp [1]),
				threadId = int.Parse (tmp [2]),
				type = evtType,
			};
		}
		public override string ToString ()
			=> $"{begin};{end};{threadId};{type}";
			
	}
	public class DbgWidgetEvent : DbgEvent
	{
		public int InstanceIndex;
		public override Color Color {
			get {
				switch (type) {
				case DbgEvtType.GOClassCreation:
					return Colors.DarkSlateGrey;
				case DbgEvtType.GOInitialization:
					return Colors.DarkOliveGreen;
				case DbgEvtType.GOClippingRegistration:
					return Colors.MediumTurquoise;
				case DbgEvtType.GORegisterClip:
					return Colors.Turquoise;
				case DbgEvtType.GORegisterForGraphicUpdate:
					return Colors.LightPink;
				case DbgEvtType.GOEnqueueForRepaint:
					return Colors.LightSalmon;
				case DbgEvtType.GONewDataSource:
					return Colors.MediumVioletRed;
				case DbgEvtType.GODraw:
					return Colors.SteelBlue;
				case DbgEvtType.GORecreateCache:
					return Colors.CornflowerBlue;
				case DbgEvtType.GOUpdateCache:
					return Colors.SteelBlue;
				case DbgEvtType.GOPaint:
					return Colors.RoyalBlue;
				case DbgEvtType.GOLockUpdate:
					return Colors.SaddleBrown;
				case DbgEvtType.GOLockClipping:
					return Colors.Sienna;
				case DbgEvtType.GOLockRender:
					return Colors.BurlyWood;
				case DbgEvtType.GOLockLayouting:
					return Colors.GoldenRod;
				case DbgEvtType.TGCancelLoadingThread:
					return Colors.Maroon;
				default:
					return Colors.Crimson;
				}
				if (type.HasFlag (DbgEvtType.Lock))
					return Colors.DarkMagenta;
				return Colors.White;
			}
		}
		public override bool IsWidgetEvent => true;
		public DbgWidgetEvent () { }
		public DbgWidgetEvent (long timeStamp, DbgEvtType evt, Widget w) : base (timeStamp, evt)
		{
#if DEBUG_LOG
			InstanceIndex = w.instanceIndex;
#endif
		}
		public override string ToString ()
			=> $"{base.ToString ()};{InstanceIndex}";
	}
	public class DbgLayoutEvent : DbgWidgetEvent
	{
		public LayoutingType layouting;
		public LayoutingQueueItem.Result result;
		public Rectangle OldSlot, NewSlot;
		public override Color Color {
			get {
				if (type == DbgEvtType.GORegisterLayouting)
					return Colors.GreenYellow;
				if (type == DbgEvtType.GOProcessLayoutingWithNoParent)
					return Colors.DarkRed;
				switch (result) {
				case LayoutingQueueItem.Result.Success:
					return Colors.Green;
				case LayoutingQueueItem.Result.Deleted:
					return Colors.Red;
				case LayoutingQueueItem.Result.Discarded:
					return Colors.OrangeRed;
				default:
					return Colors.Orange;
				}
			}
		}
		public override bool IsLayoutEvent => true;
		public DbgLayoutEvent () { }
#if DEBUG_LOG
		public DbgLayoutEvent (long timeStamp, DbgEvtType evt, LayoutingQueueItem lqi) :
			base (timeStamp, evt, lqi.graphicObject)
		{
			layouting = lqi.LayoutType;
			result = lqi.result;
			OldSlot = lqi.Slot;
			NewSlot = lqi.NewSlot;
		}
		public void SetLQI (LayoutingQueueItem lqi)
		{
			layouting = lqi.LayoutType;
			result = lqi.result;
			OldSlot = lqi.Slot;
			NewSlot = lqi.NewSlot;
		}
#else
		public DbgLayoutEvent (long timeStamp, DbgEvtType evt, LayoutingQueueItem lqi) {}
		public void SetLQI (LayoutingQueueItem lqi) { }

#endif
		public override string ToString ()
			=> $"{base.ToString ()};{layouting};{result};{OldSlot};{NewSlot}";
	}
}