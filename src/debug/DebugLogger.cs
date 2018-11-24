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
using Cairo;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;

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
		GraphicObject					= 0x0200,
		GOClassCreation					= 0x0201,
		GOInitialization					= 0x0202,
		GOClippingRegistration			= 0x0203,
		GORegisterClip					= 0x0204,
		GORegisterForGraphicUpdate		= 0x0205,
		GOEnqueueForRepaint				= 0x0206,
		GOLayouting						= 0x0400,
		GORegisterLayouting				= 0x0607,
		GOProcessLayoutingWithNoParent	= 0x0608,
		GOProcessLayouting				= 0x0609,
		GODraw							= 0x020a,
		GORecreateCache					= 0x020b,
		GOUpdateCacheAndPaintOnCTX		= 0x020c,
		GOPaint							= 0x020d,
		GONewDataSource					= 0x020e,
	}
	public class DbgData {
		public int obj;
		public LayoutingType layout;
		public LayoutingQueueItem.Result result;

		public DbgData (int _obj) {
			obj = _obj;
		}
	}
	public class DbgEvent {
		public long begin, end;
		public DbgEvtType type;
		public object data = null;

		public DbgEvent() {}
			
		public DbgEvent(long timeStamp, DbgEvtType evt, object _data = null) {			
			data = _data;
			type = evt;
			begin = timeStamp;
		}

		public override string ToString ()
		{
			GraphicObject go = data as GraphicObject;
			if (go != null)
				return string.Format ("{0};{1};{2};{3}", begin, end, type, GraphicObject.GraphicObjects.IndexOf(go).ToString());
			if (!(data is LayoutingQueueItem))
				return string.Format ("{0};{1};{2}", begin, end, type);
			LayoutingQueueItem lqi = (LayoutingQueueItem)data;
			if (type == DbgEvtType.GOProcessLayouting)
				return string.Format ("{0};{1};{2};{3};{4};{5}", begin, end, type, GraphicObject.GraphicObjects.IndexOf(lqi.graphicObject).ToString(), lqi.LayoutType.ToString(), lqi.result.ToString());			
			return string.Format ("{0};{1};{2};{3};{4}", begin, end, type, GraphicObject.GraphicObjects.IndexOf(lqi.graphicObject).ToString(), lqi.LayoutType.ToString());
			
		}

		public static DbgEvent Parse (string str) {
			if (str == null)
				return null;
			string[] tmp = str.Trim().Split(';');


			DbgEvent evt = new DbgEvent ();
			evt.begin = long.Parse (tmp [0]);
			evt.end = long.Parse (tmp [1]);
			evt.type = (DbgEvtType)Enum.Parse (typeof(DbgEvtType), tmp [2]);

			if (evt.type.HasFlag (DbgEvtType.GraphicObject)) {
				DbgData data = new DbgData (int.Parse (tmp [3]));
				if (evt.type.HasFlag (DbgEvtType.GOLayouting)) {
					data.layout = (LayoutingType)Enum.Parse (typeof(LayoutingType), tmp [4]);
					if (evt.type == DbgEvtType.GOProcessLayouting)
						data.result = (LayoutingQueueItem.Result)Enum.Parse (typeof(LayoutingQueueItem.Result), tmp [5]);										
				}
				evt.data = data;
			}
			return evt;
		}
	}
	public class DbgGo {
		public int index;
		public string name;
		public int yIndex;
		public int xLevel;

		public static DbgGo Parse (string str) {
			DbgGo g = new DbgGo ();
			if (str == null)
				return null;
			string[] tmp = str.Trim().Split(';');
			g.index = int.Parse (tmp [0]);
			g.name = tmp [1];
			g.yIndex = int.Parse (tmp [2]);
			g.xLevel = int.Parse (tmp [3]);
			return g;
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

		static void parseTree (GraphicObject go) {
			if (go == null)
				return;
				
			go.yIndex = y++;
			go.xLevel = level++;

			Group gr = go as Group;
			if (gr != null) {
				foreach (GraphicObject g in gr.Children) {
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

			foreach (GraphicObject go in iface.GraphicTree) 
				parseTree (go);			

			using (StreamWriter s = new StreamWriter("debug.log")){
				s.WriteLine ("[GraphicObjects]");
				for (int i=0; i<GraphicObject.GraphicObjects.Count; i++) {
					GraphicObject g = GraphicObject.GraphicObjects [i];
					s.WriteLine ("{0};{1};{2};{3}", i, g.GetType().Name, g.yIndex, g.xLevel);	
				}
				s.WriteLine ("[Events]");

				foreach (DbgEvent e in events)
					s.WriteLine (e.ToString ());
			}
			/*
			List<GraphicObject> drawn = new List<GraphicObject> ();


			ctx.MoveTo (0.5 + xPenStart, 0.0);
			ctx.LineTo (0.5 + xPenStart, bounds.Height);
			ctx.LineWidth = 1.0;
			ctx.SetSourceColor (Crow.Color.Black);
			ctx.Stroke ();


			List<LayoutingQueueItem> lqis = new List<LayoutingQueueItem>();

			foreach (LayoutingQueueItem lqi in lqis) {
				GraphicObject go = lqi.graphicObject;
				if (go.yIndex == 0)
					continue;
				double penX = 0.0, penY = go.yIndex * ySpacing;

				if (!drawn.Contains (go)) {
					penX = go.xLevel * 5.0;

					ctx.MoveTo (penX, penY);
					ctx.SetSourceColor (Crow.Color.Black);

					ctx.ShowText (go.GetType ().ToString ());
					drawn.Add (go);
				}

				//penX = xPenStart + xResolution * lqi.begin;
				//ctx.Rectangle (penX, penY, Math.Max(1.0, (lqi.end - lqi.begin) * xResolution), ySpacing);


				ctx.Fill ();
			}
			surf.WriteToPng ("debug.png");
			*/
		}
	}
}
#endif

