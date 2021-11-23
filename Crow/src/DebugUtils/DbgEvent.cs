using System.Diagnostics;
// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Threading;
using Drawing2D;

namespace Crow.DebugLogger
{
	public class DbgEvent : DbgEventSource
	{
		public long begin, end;
		public int threadId;
		public DbgEvtType type;
		public DbgEvtType Category => type & DbgEvtType.All;
		public string Message;
		public DbgEvent parentEvent;
		public bool HasChildEvents => Events != null && Events.Count > 0;
		public override long Duration => end - begin;
		public double DurationMS => Math.Round ((double)Duration / Stopwatch.Frequency * 1000.0, 4);
		public double BeginMS => Math.Round ((double)begin / Stopwatch.Frequency, 6);
		public double EndMS => Math.Round ((double)end / Stopwatch.Frequency, 6);
		public virtual bool IsWidgetEvent => false;
		public virtual bool IsLayoutEvent => false;
		public bool HasMessage => !string.IsNullOrEmpty(Message);

		public void AddEvent (DbgEvent evt)
		{
			if (Events == null)
				Events = new List<DbgEvent> () { evt };
			else
				Events.Add (evt);
			evt.parentEvent = this;
		}

		public DbgEvent () { }
		public DbgEvent (long timeStamp, DbgEvtType evt, string message = null)
		{
			type = evt;
			begin = timeStamp;
			end = timeStamp;
			threadId = Thread.CurrentThread.ManagedThreadId;
			Message = message;
		}

		public static DbgEvent Parse (string str)
		{
			if (str == null)
				return null;
			string [] tmp = str.Trim ().Split (';', StringSplitOptions.None);

			DbgEvtType evtType = (DbgEvtType)Enum.Parse (typeof (DbgEvtType), tmp [3]);

			if (evtType.HasFlag (DbgEvtType.Widget)) {
				if (evtType.HasFlag (DbgEvtType.Layouting))
					return new DbgLayoutEvent () {
						begin = long.Parse (tmp [0]),
						end = long.Parse (tmp [1]),
						threadId = int.Parse (tmp [2]),
						type = evtType,
						Message = tmp[4],
						InstanceIndex = int.Parse (tmp [5]),
						layouting = (LayoutingType)Enum.Parse (typeof (LayoutingType), tmp [6]),
						result = evtType == DbgEvtType.GOProcessLayouting ?
							(LayoutingQueueItem.Result)Enum.Parse (typeof (LayoutingQueueItem.Result), tmp [7])
							: LayoutingQueueItem.Result.Unknown,
						OldSlot = Rectangle.Parse (tmp [8]),
						NewSlot = Rectangle.Parse (tmp [9]),
					};
				return (tmp.Length < 6) ?
 							new DbgWidgetEvent () {
								begin = long.Parse (tmp [0]),
								end = long.Parse (tmp [1]),
								threadId = int.Parse (tmp [2]),
								type = evtType,
								Message = tmp[4],
								InstanceIndex = -1,
							} : new DbgWidgetEvent () {
								begin = long.Parse (tmp [0]),
								end = long.Parse (tmp [1]),
								threadId = int.Parse (tmp [2]),
								type = evtType,
								Message = tmp[4],
								InstanceIndex = int.Parse (tmp [5]),
							};
			}
			return new DbgEvent () {
				begin = long.Parse (tmp [0]),
				end = long.Parse (tmp [1]),
				threadId = int.Parse (tmp [2]),
				type = evtType,
				Message = tmp[4]
			};
		}
		public virtual string Print ()
			=> $"{begin,10}:{threadId,-2}:{type,-20}:{Message}";
		public override string ToString ()
			=> $"{begin};{end};{threadId};{type};{Message}";
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
					if (type.HasFlag(DbgEvtType.Mouse))
						return Colors.DeepPink;
					return Colors.White;
				}
			}
		}
	}
}