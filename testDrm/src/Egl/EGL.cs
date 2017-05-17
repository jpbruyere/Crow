//
// EGL.cs
//
// Author:
//       Jean-Philippe Bruyère <jp.bruyere@hotmail.com>
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

namespace EGL
{
	public enum Error {
		NoContext	= 0,
		NoDisplay	= 0,
		NoSurface	= 0,

		NotInitialized	= 0x3001,
		BadAccess		= 0x3002,
		BadAlloc		= 0x3003,
		BadAttribute	= 0x3004,
		BadConfig		= 0x3005,
		BadContext		= 0x3006,
		BadCurrentSurface= 0x3007,
		BadDisplay		= 0x3008,
		BadMatch		= 0x3009,
		BadNativePixmap	= 0x300A,
		BadNativeWindow	= 0x300B,
		BadParameter	= 0x300C,
		BadSurface		= 0x300D,

		ContextLost		= 0x300E,

	}
	public enum Attribute {
		BufferSize	= 0x3020,
		AlphaSize	= 0x3021,
		BlueSize	= 0x3022,
		GreenSize	= 0x3023,
		RedSize		= 0x3024,
		DepthSize	= 0x3025,
		StencilSize	= 0x3026,
		ConfigCaveat= 0x3027,
		ConfigId	= 0x3028,
		Level		= 0x3029,
		Samples			= 0x3031,
		SampleBuffers	= 0x3032,
		Height			= 0x3056,
		Width			= 0x3057,
		LargestPbuffer	= 0x3058,
		MaxPbufferHeight	= 0x302A,
		MaxPbufferPixels	= 0x302B,
		MaxPbufferWidth		= 0x302C,
		SurfaceType				= 0x3033,
		TransparentType			= 0x3034,
		TransparentBlueValue	= 0x3035,
		TransparentGreenValue	= 0x3036,
		TransparentRedValue		= 0x3037,
		BindToTextureRgb	= 0x3039,
		RenderableType		= 0x3040,
		BindToTextureRgba	= 0x303A,
		MinSwapInterval		= 0x303B,
		MaxSwapInterval		= 0x303C,
		AlphaMaskSize		= 0x303E,
		ColorBufferType		= 0x303F,
		MatchNativePixmap	= 0x3041,
		TransparentRgb		= 0x3052,
	}

	[Flags]public enum SurfaceType {
		DontCare	= -1,
		None		= 0x3038,

		Pbuffer		= 0x0001,
		Pixmap		= 0x0002,
		Window		= 0x0004,
		VgColorspaceLinear		= 0x0020,
		VgAlphaFormatPre		= 0x0040,
		MultisampleResolveBox	= 0x0200,
		SwapBehaviorPreserved	= 0x0400,
	}
	[Flags]public enum RenderableType {
		DontCare	= -1,
		None		= 0x3038,

		OpenglEs	= 0x0001,
		Openvg		= 0x0002,
		OpenglEs2	= 0x0004,
		Opengl		= 0x0008,
		OpenglEs3	= 0x00000040,
	}
	[Flags]enum ConformantType {
		DontCare	= -1,
		None	= 0x3038,

		SlowConfig	= 0x3050,
		NonConformantConfig	= 0x3051,
	}
	[Flags]enum ColorBufferType {
		RgbBuffer		= 0x308E,
		LuminanceBuffer	= 0x308F,
	}

	[Flags]public enum EglConsts {
		Version10	= 1,

		CoreNativeEngine	= 0x305B,


		Draw	= 0x3059,
		Extensions	= 0x3055,
		False	= 0,
		NativeRenderable	= 0x302D,
		NativeVisualId	= 0x302E,
		NativeVisualType	= 0x302F,


		Read	= 0x305A,


		Success	= 0x3000,


		True	= 1,

		Vendor	= 0x3053,
		Version	= 0x3054,


		Version11	= 1,

		BackBuffer	= 0x3084,


		MipmapTexture	= 0x3082,
		MipmapLevel	= 0x3083,
		NoTexture	= 0x305C,
		Texture2d	= 0x305F,
		TextureFormat	= 0x3080,
		TextureRgb	= 0x305D,
		TextureRgba	= 0x305E,
		TextureTarget	= 0x3081,
		Version12	= 1,
		AlphaFormat	= 0x3088,
		AlphaFormatNonpre	= 0x308B,
		AlphaFormatPre	= 0x308C,

		BufferPreserved	= 0x3094,
		BufferDestroyed	= 0x3095,
		ClientApis	= 0x308D,
		Colorspace	= 0x3087,
		ColorspaceSrgb	= 0x3089,
		ColorspaceLinear	= 0x308A,

		ContextClientType	= 0x3097,
		DisplayScaling	= 10000,
		HorizontalResolution	= 0x3090,
		LuminanceSize	= 0x303D,

		OpenglEsApi	= 0x30A0,
		OpenvgApi	= 0x30A1,
		OpenvgImage	= 0x3096,
		PixelAspectRatio	= 0x3092,

		RenderBuffer	= 0x3086,



		SingleBuffer	= 0x3085,
		SwapBehavior	= 0x3093,
		Unknown	= -1,
		VerticalResolution	= 0x3091,
		Version13	= 1,
		Conformant	= 0x3042,
		ContextClientVersion	= 0x3098,

		VgAlphaFormat	= 0x3088,
		VgAlphaFormatNonpre	= 0x308B,
		VgAlphaFormatPre	= 0x308C,

		VgColorspace	= 0x3087,
		VgColorspaceSrgb	= 0x3089,
		VgColorspaceLinear	= 0x308A,

		Version14	= 1,

		DefaultDisplay	= 0,

		MultisampleResolve	= 0x3099,
		MultisampleResolveDefault	= 0x309A,
		MultisampleResolveBox	= 0x309B,
		OpenglApi	= 0x30A2,
		Version15	= 1,
		ContextMajorVersion	= 0x3098,
		ContextMinorVersion	= 0x30FB,
		ContextOpenglProfileMask	= 0x30FD,
		ContextOpenglResetNotificationStrategy	= 0x31BD,
		NoResetNotification	= 0x31BE,
		LoseContextOnReset	= 0x31BF,
		ContextOpenglCoreProfileBit	= 0x00000001,
		ContextOpenglCompatibilityProfileBit	= 0x00000002,
		ContextOpenglDebug	= 0x31B0,
		ContextOpenglForwardCompatible	= 0x31B1,
		ContextOpenglRobustAccess	= 0x31B2,

		ClEventHandle	= 0x309C,
		SyncClEvent	= 0x30FE,
		SyncClEventComplete	= 0x30FF,
		SyncPriorCommandsComplete	= 0x30F0,
		SyncType	= 0x30F7,
		SyncStatus	= 0x30F1,
		SyncCondition	= 0x30F8,
		Signaled	= 0x30F2,
		Unsignaled	= 0x30F3,
		SyncFlushCommandsBit	= 0x0001,
		Forever	= int.MinValue,
		TimeoutExpired	= 0x30F5,
		ConditionSatisfied	= 0x30F6,
		NoSync	= 0,
		SyncFence	= 0x30F9,

		GlColorspace	= 0x309D,
		GlColorspaceSrgb	= 0x3089,
		GlColorspaceLinear	= 0x308A,
		GlRenderbuffer	= 0x30B9,
		GlTexture2d	= 0x30B1,
		GlTextureLevel	= 0x30BC,
		GlTexture3d	= 0x30B2,
		GlTextureZoffset	= 0x30BD,
		GlTextureCubeMapPositiveX	= 0x30B3,
		GlTextureCubeMapNegativeX	= 0x30B4,
		GlTextureCubeMapPositiveY	= 0x30B5,
		GlTextureCubeMapNegativeY	= 0x30B6,
		GlTextureCubeMapPositiveZ	= 0x30B7,
		GlTextureCubeMapNegativeZ	= 0x30B8,
		ImagePreserved	= 0x30D2,
		NoImage	= 0,		
	}
}

