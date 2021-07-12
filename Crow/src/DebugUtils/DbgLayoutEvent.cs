// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Threading;

namespace Crow.DebugLogger
{
	public class DbgLayoutEvent : DbgWidgetEvent
	{
		public LayoutingType layouting;
		public LayoutingQueueItem.Result result;
		public Rectangle OldSlot, NewSlot;
		public override bool IsLayoutEvent => true;
		public DbgLayoutEvent () { }
		public DbgLayoutEvent (long timeStamp, DbgEvtType evt, int widgetInstanceIndex,
			LayoutingType layouting, LayoutingQueueItem.Result result, Rectangle oldSlot, Rectangle newSlot) :
			base (timeStamp, evt, widgetInstanceIndex)
		{
			SetLQI (layouting, result, oldSlot, newSlot);
		}
		public void SetLQI (LayoutingType layouting, LayoutingQueueItem.Result result, Rectangle oldSlot, Rectangle newSlot)
		{
			this.layouting = layouting;
			this.result = result;
			OldSlot = oldSlot;
			NewSlot = newSlot;
		}
		public override string Print()
			=> $"{base.Print()} {layouting} {result} {OldSlot}->{NewSlot}";

		public override string ToString ()
			=> $"{base.ToString ()};{layouting};{result};{OldSlot};{NewSlot}";
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
	}
}