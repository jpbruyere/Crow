// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Threading;

namespace Crow.DebugLogger
{	
	public class DbgWidgetEvent : DbgEvent
	{
		public int InstanceIndex;
		public override bool IsWidgetEvent => true;
		public DbgWidgetEvent () { }
		public DbgWidgetEvent (long timeStamp, DbgEvtType evt, int widgetInstanceIndex) : base (timeStamp, evt)
		{
			InstanceIndex = widgetInstanceIndex;
		}
		//public override string Print() => $"{base.Print()}:{InstanceIndex}"
		
        public override string ToString ()
			=> $"{base.ToString ()};{InstanceIndex}";
		public override Color Color {
			get {
				switch (type) {
				case DbgEvtType.GOSetProperty:
					return Colors.Lime;
				case DbgEvtType.GOMeasure:
					return Colors.Pink;
				case DbgEvtType.GOSearchLargestChild:
				case DbgEvtType.GOSearchTallestChild:
					return Colors.HotPink;
				case DbgEvtType.GOOnChildLayoutChange:
					return Colors.DarkViolet;
				case DbgEvtType.GOAdjustStretchedGo:
					return Colors.PaleVioletRed;
				case DbgEvtType.GOClassCreation:
					return Colors.DarkSlateGrey;
				case DbgEvtType.GOInitialization:
					return Colors.DarkOliveGreen;
				case DbgEvtType.GOClippingRegistration:
					return Colors.MediumTurquoise;
				case DbgEvtType.GORegisterClip:
					return Colors.Turquoise;
				case DbgEvtType.GOResetClip:
					return Colors.DarkSalmon;
				case DbgEvtType.GORegisterForGraphicUpdate:
					return Colors.LightPink;
				case DbgEvtType.GOEnqueueForRepaint:
					return Colors.LightSalmon;
				case DbgEvtType.GONewDataSource:
					return Colors.MediumVioletRed;
				case DbgEvtType.GODraw:
					return Colors.SteelBlue;
				case DbgEvtType.GOCreateSurface:
					return Colors.SkyBlue;
				case DbgEvtType.GOCreateContext:
					return Colors.DeepSkyBlue;
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
					return Colors.White;
				}
			}
		}			
	}	
}