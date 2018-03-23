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
		RequeueLQI		= 0x82,
		DiscardLQI		= 0x83,
		DeleteLQI		= 0x84,
		SucceedLQI		= 0x85,
		RegisterLayouting= 0x86,
	}

	public class DebugEvent
	{
		public int ThreadId;
		public DbgEvtType EventType;
		public long Ticks;
		public string Message;

		public override string ToString ()
		{
			return string.Format ("{0,2}:{1,10}:{2} {3}", ThreadId, Ticks, EventType, Message);
		}
	}
	public class WidgetDebugEvent : DebugEvent {
		public string FullName;
		public LayoutingType RegisteredLayoutings;
		public LayoutingType RequestedLayoutings;
		public Rectangle Slot;
		public Measure Width;
		public Measure Height;

		public GraphicObject Target {
			set {
				FullName = value.ToString ();
				RegisteredLayoutings = value.registeredLayoutings;
				RequestedLayoutings = value.requestedLayoutings;
				Width = value.Width;
				Height = value.Height;
				Slot = value.Slot;				
			}
		}
		public string Name {
			get { return string.IsNullOrEmpty(FullName) ? "" : FullName.Substring(FullName.LastIndexOf('.')+1); }
		}
		public override string ToString ()
		{
			return base.ToString() + ";" + string.Format ("{0};{1};{2};{3}", Name, RegisteredLayoutings, RequestedLayoutings, Slot);
		}
	}
	#endif
}

