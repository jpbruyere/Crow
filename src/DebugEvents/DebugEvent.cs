//
// DebugEvent.cs
//
// Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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
using System.Threading;

namespace Crow
{
	#if DBG_EVENTS
	public enum DbgEvtType {
		Clipping 		= 8,
		Interface 		= 0x10,
		IfaceStart		= 0x11,
		IFaceUpdate		= 0x12,
		IFaceClipping	= 0x13,
		IFaceDrawing	= 0x14,
		Drawnig			= 0x20,
		OnDraw			= 0x21,
		Paint			= 0x22,
		UpdateCache		= 0x23,
		RecreateCache	= 0x24,
		IFaceLayouting	= 0x80,
		UpdateLayout	= 0x81,
		RegisterLayouting= 0x82,
	}

	public class DebugEvent
	{
		public DebugEvent (DbgEvtType type, string message = "")
		{
			EventType = type;
			ThreadId = Thread.CurrentThread.ManagedThreadId;
		}

		public int ThreadId;
		public DbgEvtType EventType;
		public Stopwatch Time;
		public string Message;

		public DebugEvent Parent;
		public List<DebugEvent> ChildEvents = new List<DebugEvent>();

		public long Ticks { get { return Time.ElapsedTicks; }}


		public virtual void Start () {
			Time = Stopwatch.StartNew();
		}
		public virtual void Finished () {
			Time.Stop();
		}
	}
	public class WidgetDebugEvent : DebugEvent {		
		public GraphicObject Target;
		public LayoutingType RegisteredLayoutings;
		public LayoutingType RequestedLayoutings;
		public Rectangle Slot;
		public Measure Width;
		public Measure Height;

		public WidgetDebugEvent (DbgEvtType type, GraphicObject go, string message = "") : base(type, message)
		{
			Target = go;
			saveTargetState ();
		}
		protected virtual void saveTargetState () {
			RegisteredLayoutings = Target.registeredLayoutings;
			RequestedLayoutings = Target.requestedLayoutings;
			Width = Target.Width;
			Height = Target.Height;
			Slot = Target.Slot;
		}
		public override void Finished ()
		{			
			base.Finished ();
			saveTargetState ();
		}
	}
	public class LayoutingDebugEvent : WidgetDebugEvent
	{
		public enum Result {
			Ok,
			Requeued,
			Discarded,
			Deleted,
		}
		public LayoutingDebugEvent (LayoutingType layoutingType, GraphicObject go) : base(DbgEvtType.IFaceLayouting, go)
		{
			LayoutingType = layoutingType;
			Target = go;
		}	
		public LayoutingType LayoutingType;
		public Rectangle PreviousSlot;
		public Result EndResult;
		public override void Finished ()
		{
			base.Finished ();
			saveTargetState ();
			PreviousSlot = Target.LastSlots;
		}

		public override string ToString ()
		{
			return string.Format ("{0} {1} {2} {3} => {4}", Target, LayoutingType, EndResult, PreviousSlot, Slot);
		}
	}
	#endif
}

