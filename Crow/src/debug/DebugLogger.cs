//
// CrowDebugger.cs
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
using Crow.Cairo;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

#if DEBUG_LOG
namespace Crow
{
	/*public class LayoutingEvent : DbgEvent {
		public List<LayoutingQueueItem> lqis = new List<LayoutingQueueItem>();

	}*/
	[Flags]
	public enum DbgEvtType {
		////9 nth bit set for iface event
		IFace							= 0x0100,
		IFaceStartLayouting				= 0x0101,
		IFaceEndLayouting				= 0x0102,
		IFaceStartClipping				= 0x0103,
		IFaceEndClipping				= 0x0104,
		IFaceStartDrawing				= 0x0105,
		IFaceEndDrawing					= 0x0106,
		IFaceUpdate						= 0x0107,
		//10 nth bit set for graphic obj
		GraphicObject					= 0x0100,
		Warning							= 0x4000,
		Error							= 0x8000,
		GOClassCreation					= GraphicObject | 0x01,
		GOInitialization				= GraphicObject | 0x02,
		GOClippingRegistration			= GraphicObject | 0x03,
		GORegisterClip					= GraphicObject | 0x04,
		GORegisterForGraphicUpdate		= GraphicObject | 0x05,
		GOEnqueueForRepaint				= GraphicObject | 0x06,
		GONewDataSource					= GraphicObject | 0x07,
		GOLayouting						= 0x0200,
		Drawing 						= 0x0400,
		GOLock							= 0x0800,
		TemplatedGroup					= 0x1000,
		GORegisterLayouting 			= GraphicObject | GOLayouting | 0x01,
		GOProcessLayouting				= GraphicObject | GOLayouting | 0x02,
		GOProcessLayoutingWithNoParent 	= Warning | GraphicObject | GOLayouting | 0x01,
		GODraw							= GraphicObject | Drawing | 0x01,
		GORecreateCache					= GraphicObject | Drawing | 0x02,
		GOUpdateCacheAndPaintOnCTX		= GraphicObject | Drawing | 0x03,
		GOPaint							= GraphicObject | Drawing | 0x04,

		GOLockUpdate					= GraphicObject | GOLock | 0x01,
		GOLockClipping					= GraphicObject | GOLock | 0x02,
		GOLockRender					= GraphicObject | GOLock | 0x03,
		GOLockLayouting					= GraphicObject | GOLock | 0x04,

		TGCancelLoadingThread			= GraphicObject | TemplatedGroup | 0x01,
	}

	/// <summary>
	/// debug events as recorded, another class is used in the viewer
	/// </summary>
	public class DbgEvent {
		public long begin, end;
		public DbgEvtType type;
		public object data = null;

		public DbgEvent() {}
			
		public DbgEvent(long timeStamp, DbgEvtType evt, object _data = null) {			
			data = _data;
			type = evt;
			begin = timeStamp;
			end = timeStamp;
		}

		public override string ToString ()
		{
			Widget go = data as Widget;
			if (go != null)
				return string.Format ("{0};{1};{2};{3}", begin, end, type, Widget.GraphicObjects.IndexOf(go).ToString());
			if (!(data is LayoutingQueueItem))
				return string.Format ("{0};{1};{2}", begin, end, type);
			LayoutingQueueItem lqi = (LayoutingQueueItem)data;
			if (type == DbgEvtType.GOProcessLayouting)
				return string.Format ("{0};{1};{2};{3};{4};{5}", begin, end, type, Widget.GraphicObjects.IndexOf(lqi.graphicObject).ToString(), lqi.LayoutType.ToString(), lqi.result.ToString());			
			return string.Format ("{0};{1};{2};{3};{4}", begin, end, type, Widget.GraphicObjects.IndexOf(lqi.graphicObject).ToString(), lqi.LayoutType.ToString());
			
		}
	}

	public static class DebugLog
	{
		static Surface surf;
		static Context ctx;

		static Crow.Rectangle bounds = new Crow.Rectangle(0,0,8182,4096);
		static double penX = 1.0;
		static double ySpacing = 10.0;
		static double xPenStart = 250.0;
		static double xResolution = 0.001; //per tick
		public static Stopwatch chrono = Stopwatch.StartNew();

		public static List<DbgEvent> events = new List<DbgEvent>();
		public static DbgEvent currentEvent = null;

		public static DbgEvent AddEvent (DbgEvtType evtType, object data = null) {
			DbgEvent evt = new DbgEvent(chrono.ElapsedTicks, evtType, data);
			events.Add (evt);
			return evt;
		}

		static int y, level;

		static void parseTree (Widget go) {
			if (go == null)
				return;


			go.yIndex = y++;
			go.xLevel = level++;

			Group gr = go as Group;
			if (gr != null) {
				foreach (Widget g in gr.Children) {
					parseTree (g);
				}
			} else {
				PrivateContainer pc = go as PrivateContainer;
				if (pc != null)
					parseTree (pc.getTemplateRoot);				
			}
			level--;		
		}

		public static void save(Interface iface) {
			y = 1;
			level = 0;

			foreach (Widget go in iface.GraphicTree) 
				parseTree (go);			

			using (StreamWriter s = new StreamWriter("debug.log")){
				s.WriteLine ("[GraphicObjects]");
				lock (Widget.GraphicObjects) {
					Widget.GraphicObjects = Widget.GraphicObjects.OrderBy (o => o.yIndex).ToList();
					for (int i = 0; i < Widget.GraphicObjects.Count; i++) {
						Widget g = Widget.GraphicObjects [i];
						s.WriteLine ("{0};{1};{2};{3}", i, g.GetType ().Name, g.yIndex, g.xLevel);	
					}
				}
				s.WriteLine ("[Events]");

				foreach (DbgEvent e in events)
					if (e != null)
						s.WriteLine (e.ToString ());
			}
		}
	}
}
#endif

