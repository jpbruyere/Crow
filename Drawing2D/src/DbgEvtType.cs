// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	[Flags]
	public enum DbgEvtType : Int32 {
		None							= 0,
		IFace							= 0x00000100,
		Widget 							= 0x00000200,

		Warning 						= 0x00000400,
		Error							= 0x00000800,

		Binding							= 0x00001000,
		Lock 							= 0x00002000,
		Layouting	 					= 0x00004000,
		Clipping						= 0x00008000,
		Drawing							= 0x00010000,

		Focus							= 0x00020000,
		Override						= 0x00040000,
		TemplatedGroup					= 0x00080000,
		Dispose		 					= 0x00100000,
		Mouse		 					= 0x00200000,
		DragNDrop	 					= 0x00400000,

		Update							= IFace | 0x10000000,
		ProcessLayouting				= IFace | Update | Lock | Layouting,
		ClippingRegistration			= IFace | Update | Lock | Clipping,
		ProcessDrawing					= IFace | Update | Lock | Drawing,
		IFaceLoad						= IFace | 0x01,
		IFaceInit						= IFace | 0x02,
		CreateITor						= IFace | 0x03,
		IFaceReloadTheme				= IFace | 0x04,

		HoverWidget						= Focus | Widget | 0x01,
		FocusedWidget					= Focus | Widget | 0x02,
		ActiveWidget					= Focus | Widget | 0x04,
		UnfocusedWidget					= Focus | Widget | 0x08,

		//10 nth bit set for graphic obj
		GOClassCreation					= Widget | 0x01,
		GOInitialization				= Widget | 0x02,
		GORegisterForGraphicUpdate		= Widget | 0x03,
		GOEnqueueForRepaint				= Widget | 0x04,
		GONewDataSource					= Widget | 0x05,
		GONewParent						= Widget | 0x06,
		GONewLogicalParent				= Widget | 0x07,
		GOAddChild		 				= Widget | 0x08,

		GOMeasure						= Widget | 0x09,
		GOSearchLargestChild			= Widget | 0x0A,
		GOSearchTallestChild 			= Widget | 0x0B,
		GORegisterForRedraw		 		= Widget | 0x0C,
		GOComputeChildrenPositions 		= Widget | 0x0D,
		GOOnChildLayoutChange	 		= Widget | 0x0E,
		GOAdjustStretchedGo		 		= Widget | 0x0F,
		GOSetProperty			 		= Widget | 0x10,

		AlreadyDisposed					= Widget | Dispose | Error | 0x01,
		DisposedByGC					= Widget | Dispose | Error | 0x02,
		Disposing 						= Widget | Dispose | 0x01,

		GOClippingRegistration			= Widget | Clipping | 0x01,
		GORegisterClip					= Widget | Clipping | 0x02,
		GOResetClip						= Widget | Clipping | 0x03,
		GORegisterLayouting 			= Widget | Layouting | 0x01,
		GOProcessLayouting				= Widget | Layouting | 0x02,
		GOProcessLayoutingWithNoParent 	= Widget | Layouting | Warning | 0x01,
		GOProcessLayoutingWhileClipReg 	= Widget | Layouting | Warning | 0x02,
		GODraw							= Widget | Drawing | 0x01,
		GORecreateCache					= Widget | Drawing | 0x02,
		GOUpdateCache					= Widget | Drawing | 0x03,
		GOPaintCache					= Widget | Drawing | 0x04,
		GOPaint							= Widget | Drawing | 0x05,
		GOCreateSurface					= Widget | Drawing | 0x06,
		GOCreateContext					= Widget | Drawing | 0x07,


		GOLockUpdate					= Widget | Lock | 0x01,
		GOLockClipping					= Widget | Lock | 0x02,
		GOLockRender					= Widget | Lock | 0x03,
		GOLockLayouting					= Widget | Lock | 0x04,

		TGLoadingThread					= Widget | TemplatedGroup | 0x01,
		TGCancelLoadingThread			= Widget | TemplatedGroup | 0x02,

		MouseDown						= IFace | Mouse | 0x01,
		MouseUp							= IFace | Mouse | 0x02,
		MouseMove						= IFace | Mouse | 0x03,
		MouseEnter						= Widget | Mouse | 0x01,
		MouseLeave						= Widget | Mouse | 0x02,
		WidgetMouseMove					= Widget | Mouse | 0x03,
		WidgetMouseDown					= Widget | Mouse | 0x04,
		WidgetMouseUp					= Widget | Mouse | 0x05,
		WidgetMouseWheel				= Widget | Mouse | 0x06,
		WidgetMouseClick				= Widget | Mouse | 0x07,
		WidgetMouseDblClick				= Widget | Mouse | 0x08,
		Drag							= Widget | DragNDrop | 0x01,
		DragEnter						= Widget | DragNDrop | 0x02,
		DragLeave						= Widget | DragNDrop | 0x03,
		StartDrag						= Widget | DragNDrop | 0x04,
		EndDrag							= Widget | DragNDrop | 0x05,
		Drop							= Widget | DragNDrop | 0x06,

		All = 0x7FFFFF00
	}
}