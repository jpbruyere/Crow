// Copyright (c) 2013-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;

namespace Crow
{
	[Flags]
	public enum DbgEvtType : Int32 {
		None							= 0,
		IFace							= 0x40000000,
		Widget 							= 0x20000000,

		Warning 						= 0x10000000,
		Error							= 0x08000000,

		Binding							= 0x800000,
		Lock 							= 0x400000,
		Layouting	 					= 0x200000,
		Clipping						= 0x100000,
		Drawing							= 0x080000,

		Focus							= 0x040000,
		Override						= 0x020000,
		TemplatedGroup					= 0x010000,
		Dispose		 					= 0x008000,

		Update							= IFace | 0x004000,
		ProcessLayouting				= IFace | Update | Lock | Layouting,
		ClippingRegistration			= IFace | Update | Lock | Clipping,
		ProcessDrawing					= IFace | Update | Lock | Drawing,
		IFaceLoad						= IFace | 0x01,
		IFaceInit						= IFace | 0x02,
		CreateITor						= IFace | 0x04,
		IFaceReloadTheme				= IFace | 0x08,

		HoverWidget						= Focus | Widget | 0x01,
		FocusedWidget					= Focus | Widget | 0x02,
		ActiveWidget					= Focus | Widget | 0x04,
		UnfocusedWidget					= Focus | Widget | 0x08,

		//10 nth bit set for graphic obj
		GOClassCreation					= Widget | 0x01,
		GOInitialization				= Widget | 0x02,
		GORegisterForGraphicUpdate		= Widget | 0x04,
		GOEnqueueForRepaint				= Widget | 0x08,
		GONewDataSource					= Widget | 0x10,
		GONewParent						= Widget | 0x20,
		GONewLogicalParent				= Widget | 0x40,
		GOAddChild		 				= Widget | 0x80,

		GOSearchLargestChild			= Widget | 0x09,
		GOSearchTallestChild 			= Widget | 0x0A,
		GORegisterForRedraw		 		= Widget | 0x0B,
		GOComputeChildrenPositions 		= Widget | 0x0C,
		GOOnChildLayoutChange	 		= Widget | 0x0D,

		AlreadyDisposed					= Dispose | Widget | Error | 0x01,
		DisposedByGC					= Dispose | Widget | Error | 0x02,
		Disposing 						= Dispose | Widget | 0x01,

		GOClippingRegistration			= Clipping | Widget | 0x01,
		GORegisterClip					= Clipping | Widget | 0x02,
		GORegisterLayouting 			= Layouting | Widget | 0x01,
		GOProcessLayouting				= Layouting | Widget | 0x02,
		GOProcessLayoutingWithNoParent 	= Layouting | Widget | Warning | 0x01,
		GOMeasure						= Widget | 0x03,
		GODraw							= Drawing | Widget | 0x01,
		GORecreateCache					= Drawing | Widget | 0x02,
		GOUpdateCache					= Drawing | Widget | 0x03,
		GOPaint							= Drawing | Widget | 0x04,

		GOLockUpdate					= Widget | Lock | 0x01,
		GOLockClipping					= Widget | Lock | 0x02,
		GOLockRender					= Widget | Lock | 0x03,
		GOLockLayouting					= Widget | Lock | 0x04,

		TGLoadingThread					= Widget | TemplatedGroup | 0x01,
		TGCancelLoadingThread			= Widget | TemplatedGroup | 0x02,

		All = 0x7FFFFF00
	}
}