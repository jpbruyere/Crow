using System.Diagnostics;
// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Threading;

namespace Crow.DebugLogger
{
	public class DbgEvent : DbgEventSource
	{
		public long begin, end;
		public int threadId;
		public DbgEvtType type;
		public DbgEvtType Category => type & DbgEvtType.All;
		public DbgEvent parentEvent;
		public bool HasChildEvents => Events != null && Events.Count > 0;
		public override long Duration => end - begin;
		public double DurationMS => Math.Round ((double)Duration / Stopwatch.Frequency * 1000.0, 4);
		public double BeginMS => Math.Round ((double)begin / Stopwatch.Frequency * 1000.0, 4);
		public double EndMS => Math.Round ((double)end / Stopwatch.Frequency * 1000.0, 4);
		public virtual bool IsWidgetEvent => false;
		public virtual bool IsLayoutEvent => false;

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
				return (tmp.Length < 5) ?
 							new DbgWidgetEvent () {
								begin = long.Parse (tmp [0]),
								end = long.Parse (tmp [1]),
								threadId = int.Parse (tmp [2]),
								type = evtType,
								InstanceIndex = -1,
							} : new DbgWidgetEvent () {
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
		public virtual string Print ()
			=> $"{begin,10}:{threadId,-2}:{type,-20}:";
		public override string ToString ()
			=> $"{begin};{end};{threadId};{type}";
		public virtual Color Color {
			get {
				switch (type) {
				case DbgEvtType.ProcessLayouting:
					return Colors.Yellow;
				case DbgEvtType.ClippingRegistration:
					return Colors.DarkTurquoise;
				case DbgEvtType.ProcessDrawing:
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
	}
}