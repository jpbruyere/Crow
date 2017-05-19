//
// Context.cs
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
using System.Runtime.InteropServices;
using Linux;
using GBM = Linux.GBM;

namespace EGL
{
	using EGLNativeDisplayType = IntPtr;
	using EGLNativeWindowType = IntPtr;
	using EGLNativePixmapType = IntPtr;
	using EGLConfig = IntPtr;
	using EGLContext = IntPtr;
	using EGLDisplay = IntPtr;
	using EGLSurface = IntPtr;
	using EGLClientBuffer = IntPtr;

	public enum RenderApi
	{
		ES = Egl.OPENGL_ES_API,
		GL = Egl.OPENGL_API,
		VG = Egl.OPENVG_API
	}
	[Flags]public enum RenderableFlags
	{
		ES = Egl.OPENGL_ES_BIT,
		ES2 = Egl.OPENGL_ES2_BIT,
		ES3 = Egl.OPENGL_ES3_BIT,
		GL = Egl.OPENGL_BIT,
		VG = Egl.OPENVG_BIT,
	}
	public enum ErrorCode
	{
		SUCCESS = 12288,
		NOT_INITIALIZED = 12289,
		BAD_ACCESS = 12290,
		BAD_ALLOC = 12291,
		BAD_ATTRIBUTE = 12292,
		BAD_CONFIG = 12293,
		BAD_CONTEXT = 12294,
		BAD_CURRENT_SURFACE = 12295,
		BAD_DISPLAY = 12296,
		BAD_MATCH = 12297,
		BAD_NATIVE_PIXMAP = 12298,
		BAD_NATIVE_WINDOW = 12299,
		BAD_PARAMETER = 12300,
		BAD_SURFACE = 12301,
		CONTEXT_LOST = 12302,
	}

	#region consts
	public static class Egl {
		public const int VERSION_1_0 = 1;
		public const int VERSION_1_1 = 1;
		public const int VERSION_1_2 = 1;
		public const int VERSION_1_3 = 1;
		public const int VERSION_1_4 = 1;
		public const int FALSE = 0;
		public const int TRUE = 1;
		public const int DONT_CARE = -1;
		public const int CONTEXT_LOST = 12302;
		public const int BUFFER_SIZE = 12320;
		public const int ALPHA_SIZE = 12321;
		public const int BLUE_SIZE = 12322;
		public const int GREEN_SIZE = 12323;
		public const int RED_SIZE = 12324;
		public const int DEPTH_SIZE = 12325;
		public const int STENCIL_SIZE = 12326;
		public const int CONFIG_CAVEAT = 12327;
		public const int CONFIG_ID = 12328;
		public const int LEVEL = 12329;
		public const int MAX_PBUFFER_HEIGHT = 12330;
		public const int MAX_PBUFFER_PIXELS = 12331;
		public const int MAX_PBUFFER_WIDTH = 12332;
		public const int NATIVE_RENDERABLE = 12333;
		public const int NATIVE_VISUAL_ID = 12334;
		public const int NATIVE_VISUAL_TYPE = 12335;
		public const int PRESERVED_RESOURCES = 12336;
		public const int SAMPLES = 12337;
		public const int SAMPLE_BUFFERS = 12338;
		public const int SURFACE_TYPE = 12339;
		public const int TRANSPARENT_TYPE = 12340;
		public const int TRANSPARENT_BLUE_VALUE = 12341;
		public const int TRANSPARENT_GREEN_VALUE = 12342;
		public const int TRANSPARENT_RED_VALUE = 12343;
		public const int NONE = 12344;
		public const int BIND_TO_TEXTURE_RGB = 12345;
		public const int BIND_TO_TEXTURE_RGBA = 12346;
		public const int MIN_SWAP_INTERVAL = 12347;
		public const int MAX_SWAP_INTERVAL = 12348;
		public const int LUMINANCE_SIZE = 12349;
		public const int ALPHA_MASK_SIZE = 12350;
		public const int COLOR_BUFFER_TYPE = 12351;
		public const int RENDERABLE_TYPE = 12352;
		public const int MATCH_NATIVE_PIXMAP = 12353;
		public const int CONFORMANT = 12354;
		public const int SLOW_CONFIG = 12368;
		public const int NON_CONFORMANT_CONFIG = 12369;
		public const int TRANSPARENT_RGB = 12370;
		public const int RGB_BUFFER = 12430;
		public const int LUMINANCE_BUFFER = 12431;
		public const int NO_TEXTURE = 12380;
		public const int TEXTURE_RGB = 12381;
		public const int TEXTURE_RGBA = 12382;
		public const int TEXTURE_2D = 12383;
		public const int PBUFFER_BIT = 1;
		public const int PIXMAP_BIT = 2;
		public const int WINDOW_BIT = 4;
		public const int VG_COLORSPACE_LINEAR_BIT = 32;
		public const int VG_ALPHA_FORMAT_PRE_BIT = 64;
		public const int MULTISAMPLE_RESOLVE_BOX_BIT = 512;
		public const int SWAP_BEHAVIOR_PRESERVED_BIT = 1024;
		public const int OPENGL_ES_BIT = 1;
		public const int OPENVG_BIT = 2;
		public const int OPENGL_ES2_BIT = 4;
		public const int OPENGL_BIT = 8;
		public const int OPENGL_ES3_BIT = 64;
		public const int VENDOR = 12371;
		public const int VERSION = 12372;
		public const int EXTENSIONS = 12373;
		public const int CLIENT_APIS = 12429;
		public const int HEIGHT = 12374;
		public const int WIDTH = 12375;
		public const int LARGEST_PBUFFER = 12376;
		public const int TEXTURE_FORMAT = 12416;
		public const int TEXTURE_TARGET = 12417;
		public const int MIPMAP_TEXTURE = 12418;
		public const int MIPMAP_LEVEL = 12419;
		public const int RENDER_BUFFER = 12422;
		public const int VG_COLORSPACE = 12423;
		public const int VG_ALPHA_FORMAT = 12424;
		public const int HORIZONTAL_RESOLUTION = 12432;
		public const int VERTICAL_RESOLUTION = 12433;
		public const int PIXEL_ASPECT_RATIO = 12434;
		public const int SWAP_BEHAVIOR = 12435;
		public const int MULTISAMPLE_RESOLVE = 12441;
		public const int BACK_BUFFER = 12420;
		public const int SINGLE_BUFFER = 12421;
		public const int VG_COLORSPACE_sRGB = 12425;
		public const int VG_COLORSPACE_LINEAR = 12426;
		public const int VG_ALPHA_FORMAT_NONPRE = 12427;
		public const int VG_ALPHA_FORMAT_PRE = 12428;
		public const int DISPLAY_SCALING = 10000;
		public const int UNKNOWN = -1;
		public const int BUFFER_PRESERVED = 12436;
		public const int BUFFER_DESTROYED = 12437;
		public const int OPENVG_IMAGE = 12438;
		public const int CONTEXT_CLIENT_TYPE = 12439;
		public const int CONTEXT_CLIENT_VERSION = 12440;
		public const int MULTISAMPLE_RESOLVE_DEFAULT = 12442;
		public const int MULTISAMPLE_RESOLVE_BOX = 12443;
		public const int OPENGL_ES_API = 12448;
		public const int OPENVG_API = 12449;
		public const int OPENGL_API = 12450;
		public const int DRAW = 12377;
		public const int READ = 12378;
		public const int CORE_NATIVE_ENGINE = 12379;
		public const int COLORSPACE = VG_COLORSPACE;
		public const int ALPHA_FORMAT = VG_ALPHA_FORMAT;
		public const int COLORSPACE_sRGB = VG_COLORSPACE_sRGB;
		public const int COLORSPACE_LINEAR = VG_COLORSPACE_LINEAR;
		public const int ALPHA_FORMAT_NONPRE = VG_ALPHA_FORMAT_NONPRE;
		public const int ALPHA_FORMAT_PRE = VG_ALPHA_FORMAT_PRE;

		// EGL_ANGLE_d3d_share_handle_client_buffer
		public const int D3D_TEXTURE_2D_SHARE_HANDLE_ANGLE = 0x3200;
		// EGL_ANGLE_window_fixed_size
		public const int FIXED_SIZE_ANGLE = 0x3201;
		// EGL_ANGLE_query_surface_pointer 
		[DllImport("libEGL.dll", EntryPoint = "eglQuerySurfacePointerANGLE")]
		public static extern bool QuerySurfacePointerANGLE(EGLDisplay display, EGLSurface surface, int attribute, out IntPtr value);
		// EGL_ANGLE_software_display
		public static readonly EGLNativeDisplayType SOFTWARE_DISPLAY_ANGLE = new EGLNativeDisplayType(-1);
		// EGL_ANGLE_direct3d_display
		public static readonly EGLNativeDisplayType D3D11_ELSE_D3D9_DISPLAY_ANGLE = new EGLNativeDisplayType(-2);
		public static readonly EGLNativeDisplayType D3D11_ONLY_DISPLAY_ANGLE = new EGLNativeDisplayType(-3);
		// EGL_ANGLE_device_d3d
		public const int D3D9_DEVICE_ANGLE = 0x33A0;
		public const int D3D11_DEVICE_ANGLE = 0x33A1;
		// EGL_ANGLE_platform_angle
		public const int PLATFORM_ANGLE_ANGLE = 0x3202;
		public const int PLATFORM_ANGLE_TYPE_ANGLE = 0x3203;
		public const int PLATFORM_ANGLE_MAX_VERSION_MAJOR_ANGLE = 0x3204;
		public const int PLATFORM_ANGLE_MAX_VERSION_MINOR_ANGLE = 0x3205;
		public const int PLATFORM_ANGLE_TYPE_DEFAULT_ANGLE = 0x3206;
		// EGL_ANGLE_platform_angle_d3d
		public const int PLATFORM_ANGLE_TYPE_D3D9_ANGLE = 0x3207;
		public const int PLATFORM_ANGLE_TYPE_D3D11_ANGLE = 0x3208;
		public const int PLATFORM_ANGLE_DEVICE_TYPE_ANGLE = 0x3209;
		public const int PLATFORM_ANGLE_DEVICE_TYPE_HARDWARE_ANGLE = 0x320A;
		public const int PLATFORM_ANGLE_DEVICE_TYPE_WARP_ANGLE = 0x320B;
		public const int PLATFORM_ANGLE_DEVICE_TYPE_REFERENCE_ANGLE = 0x320C;
		public const int PLATFORM_ANGLE_ENABLE_AUTOMATIC_TRIM_ANGLE = 0x320F;
		// EGL_ANGLE_platform_angle_opengl
		public const int PLATFORM_ANGLE_TYPE_OPENGL_ANGLE = 0x320D;
		public const int PLATFORM_ANGLE_TYPE_OPENGLES_ANGLE = 0x320E;
	}
	#endregion

	unsafe public class Context : IDisposable
	{
		#region pinvoke
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetError")]
		public static extern ErrorCode GetError();
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetDisplay")]
		public static extern EGLDisplay GetDisplay(EGLNativeDisplayType display_id);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglInitialize")]
		public static extern bool Initialize(EGLDisplay dpy, out int major, out int minor);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglTerminate")]
		public static extern bool Terminate(EGLDisplay dpy);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglQueryString")]
		public static extern IntPtr QueryString(EGLDisplay dpy, int name);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetConfigs")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool GetConfigs(EGLDisplay dpy, EGLConfig[] configs, int config_size, out int num_config);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglChooseConfig")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool ChooseConfig(EGLDisplay dpy, int[] attrib_list, [In, Out] EGLConfig[] configs, int config_size, out int num_config);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetConfigAttrib")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool GetConfigAttrib(EGLDisplay dpy, EGLConfig config, int attribute, out int value);

		[DllImportAttribute("libEGL.dll", EntryPoint = "eglBindAPI")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool BindAPI(RenderApi api);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglQueryAPI")]
		public static extern int QueryAPI();
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglWaitClient")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool WaitClient();
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglReleaseThread")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool ReleaseThread();

		[DllImportAttribute("libEGL.dll", EntryPoint = "eglSwapInterval")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool SwapInterval(EGLDisplay dpy, int interval);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglCreateContext")]
		static extern IntPtr eglCreateContext(EGLDisplay dpy, EGLConfig config, EGLContext share_context, int[] attrib_list);
		public static EGLContext CreateContext(EGLDisplay dpy, EGLConfig config, EGLContext share_context, int[] attrib_list)
		{
			IntPtr ptr = eglCreateContext(dpy, config, share_context, attrib_list);
			if (ptr == IntPtr.Zero)
				throw new Exception(String.Format("Failed to create EGL context, error: {0}.", GetError()));
			return ptr;
		}
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglDestroyContext")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool DestroyContext(EGLDisplay dpy, EGLContext ctx);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglMakeCurrent")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool MakeCurrent(EGLDisplay dpy, EGLSurface draw, EGLSurface read, EGLContext ctx);

		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetCurrentContext")]
		public static extern EGLContext GetCurrentContext();
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetCurrentSurface")]
		public static extern EGLSurface GetCurrentSurface(int readdraw);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetCurrentDisplay")]
		public static extern EGLDisplay GetCurrentDisplay();
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglQueryContext")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool QueryContext(EGLDisplay dpy, EGLContext ctx, int attribute, out int value);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglWaitGL")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool WaitGL();
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglWaitNative")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool WaitNative(int engine);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglCopyBuffers")]
		[return: MarshalAsAttribute(UnmanagedType.I1)]
		public static extern bool CopyBuffers(EGLDisplay dpy, EGLSurface surface, EGLNativePixmapType target);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetProcAddress")]
		public static extern IntPtr GetProcAddress(string funcname);
		[DllImportAttribute("libEGL.dll", EntryPoint = "eglGetProcAddress")]
		public static extern IntPtr GetProcAddress(IntPtr funcname);
		// EGL_EXT_platform_base
		[DllImport("libEGL.dll", EntryPoint = "eglGetPlatformDisplayEXT")]
		public static extern EGLDisplay GetPlatformDisplayEXT(int platform, EGLNativeDisplayType native_display, int[] attrib_list);
		[DllImport("libEGL.dll", EntryPoint = "eglCreatePlatformWindowSurfaceEXT")]
		public static extern EGLSurface CreatePlatformWindowSurfaceEXT(EGLDisplay dpy, EGLConfig config, EGLNativeWindowType native_window, int[] attrib_list);
		[DllImport("libEGL.dll", EntryPoint = "eglCreatePlatformPixmapSurfaceEXT")]
		public static extern EGLSurface CreatePlatformPixmapSurfaceEXT(EGLDisplay dpy, EGLConfig config, EGLNativePixmapType native_pixmap, int[] attrib_list);
		// Returns true if Egl drivers exist on the system.
		public static bool IsSupported
		{
			get
			{
				try { GetCurrentContext(); }
				catch (Exception) { return false;  }
				return true;
			}
		}
		#endregion

		int fd_gpu;
		internal EGLDisplay dpy;
		internal EGLContext ctx;
		internal EGLConfig currentCfg;

		int major, minor;

		public string Version { get { return Marshal.PtrToStringAuto (QueryString (dpy, Egl.VERSION)); }}
		public string Vendor { get { return Marshal.PtrToStringAuto (QueryString (dpy, Egl.VENDOR)); }}
		public string Extensions { get { return Marshal.PtrToStringAuto (QueryString (dpy, Egl.EXTENSIONS)); }}	
		public string OffScreenExtensions { get { return Marshal.PtrToStringAuto (QueryString (IntPtr.Zero, Egl.EXTENSIONS)); }}	

		#region ctor
		public Context (GBM.Device gbmDev)
		{
			dpy = GetDisplay(gbmDev.handle);

			if (dpy == IntPtr.Zero)
				throw new NotSupportedException("[EGL] GetDisplay failed.: " + GetError());

			if (!Initialize(dpy, out major, out minor))
				throw new NotSupportedException("[EGL] Failed to initialize EGL display. Error code: " + GetError());

			if (!BindAPI (RenderApi.GL))
				throw new NotSupportedException("[EGL] Failed to bind EGL Api: " + GetError());

			int[] contextAttrib = new int[] {
				Egl.CONTEXT_CLIENT_VERSION, 2,
				Egl.NONE
			};
			int[] desiredConfig = new int[] 
			{				
				Egl.SURFACE_TYPE, Egl.WINDOW_BIT,
				Egl.RENDERABLE_TYPE, Egl.OPENGL_BIT,
				Egl.RED_SIZE, 1, 
				Egl.GREEN_SIZE, 1, 
				Egl.BLUE_SIZE, 1,
				Egl.ALPHA_SIZE, 0,
				//Egl.DEPTH_SIZE, 24,
				//Egl.STENCIL_SIZE, 0,
				//Egl.SAMPLE_BUFFERS, 2,
				//Egl.SAMPLES, 0,
				Egl.NONE
			};
			int num_configs;
			IntPtr[] configs = new IntPtr[1];
			if (!ChooseConfig(dpy, desiredConfig, configs, 1, out num_configs))
				throw new NotSupportedException(String.Format("[EGL] Failed to retrieve GraphicsMode, error {0}", GetError()));

			currentCfg = configs [0];

			ctx = CreateContext(dpy, currentCfg, IntPtr.Zero, contextAttrib);
			if (ctx == IntPtr.Zero)
				throw new NotSupportedException(String.Format("[EGL] Failed to create egl context, error {0}.", GetError()));
			
//			int[] attribs = new int[] {
//				(int)Attribute.BufferSize,
//				Egl.RED_SIZE,
//				Egl.GREEN_SIZE,
//				Egl.BLUE_SIZE,
//				Egl.ALPHA_SIZE,
//				(int)Attribute.DepthSize,
//				//(int)Attribute.Height,
//				Egl.WIDTH,
//				(int)Attribute.Width,
//				(int)Attribute.Samples,
//				(int)Attribute.SampleBuffers,
//				(int)Attribute.RenderableType,
//				(int)Attribute.SurfaceType,
//				(int)Attribute.Level,
//				(int)Attribute.ConfigCaveat,
//			};
//
//			for (int i = 0; i < configs.Length; i++) {
//				IntPtr conf = configs[i];
//				Console.Write ("{0,-3}:", i);
//				for (int j = 0; j < attribs.Length; j++) {
//					int value;
//					GetConfigAttrib (dpy, conf, attribs[j], out value);	
//					Console.Write ("\t{0} = {1}, ", EglConstToString ((int)attribs[j]), value);
//				}
//				Console.Write ("\n");
//			}

		}
		#endregion

		enum ConfigAttribute {
			RedSize ,
			GreenSize,
			BlueSize,
			AlphaSize
		}

		#region IDisposable implementation
		~Context(){
			Dispose (false);
		}
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing){
			if (GetCurrentContext () == ctx) {
				Console.WriteLine ("destroying context");
				DestroyContext (dpy, ctx);
			}else
				Console.WriteLine ("not current");
			
			if (dpy != IntPtr.Zero)
				Terminate (dpy);
			dpy = IntPtr.Zero;
		}
		#endregion


		static string EglConstToString (int cst){
			switch (cst) {
/*			not hex value: EGL_DONT_CARE                     ((EGLint)-1)
			not hex value: EGL_FALSE                         0
			not hex value: EGL_NO_CONTEXT                    ((EGLContext)0)
			not hex value: EGL_NO_DISPLAY                    ((EGLDisplay)0)
			not hex value: EGL_NO_SURFACE                    ((EGLSurface)0)
			not hex value: EGL_TRUE                          1
			not hex value: EGL_VERSION_1_1 1
			not hex value: EGL_VERSION_1_2 1
			not hex value: EGL_DISPLAY_SCALING               10000
			not hex value: EGL_UNKNOWN                       ((EGLint)-1)
			not hex value: EGL_VERSION_1_3 1
			not hex value: EGL_VERSION_1_4 1
			not hex value: EGL_DEFAULT_DISPLAY               ((EGLNativeDisplayType)0)
			not hex value: EGL_VERSION_1_5 1
			parsing error: EGL_FOREVER                       0xFFFFFFFFFFFFFFFFull
			not hex value: EGL_NO_SYNC                       ((EGLSync)0)
			not hex value: EGL_NO_IMAGE                      ((EGLImage)0)*/
			case 1:
				return "PBUFFER_BIT|OPENGL_ES_BIT|CONTEXT_OPENGL_CORE_PROFILE_BIT|SYNC_FLUSH_COMMANDS_BIT";
			case 2:
				return "PIXMAP_BIT|OPENVG_BIT|CONTEXT_OPENGL_COMPATIBILITY_PROFILE_BIT";
			case 4:
				return "WINDOW_BIT|OPENGL_ES2_BIT";
			case 8:
				return "OPENGL_BIT";
			case 32:
				return "VG_COLORSPACE_LINEAR_BIT";
			case 64:
				return "VG_ALPHA_FORMAT_PRE_BIT|OPENGL_ES3_BIT";
			case 512:
				return "MULTISAMPLE_RESOLVE_BOX_BIT";
			case 1024:
				return "SWAP_BEHAVIOR_PRESERVED_BIT";
			case 12288:
				return "SUCCESS";
			case 12289:
				return "NOT_INITIALIZED";
			case 12290:
				return "BAD_ACCESS";
			case 12291:
				return "BAD_ALLOC";
			case 12292:
				return "BAD_ATTRIBUTE";
			case 12293:
				return "BAD_CONFIG";
			case 12294:
				return "BAD_CONTEXT";
			case 12295:
				return "BAD_CURRENT_SURFACE";
			case 12296:
				return "BAD_DISPLAY";
			case 12297:
				return "BAD_MATCH";
			case 12298:
				return "BAD_NATIVE_PIXMAP";
			case 12299:
				return "BAD_NATIVE_WINDOW";
			case 12300:
				return "BAD_PARAMETER";
			case 12301:
				return "BAD_SURFACE";
			case 12302:
				return "CONTEXT_LOST";
			case 12320:
				return "BUFFER_SIZE";
			case 12321:
				return "ALPHA_SIZE";
			case 12322:
				return "BLUE_SIZE";
			case 12323:
				return "GREEN_SIZE";
			case 12324:
				return "RED_SIZE";
			case 12325:
				return "DEPTH_SIZE";
			case 12326:
				return "STENCIL_SIZE";
			case 12327:
				return "CONFIG_CAVEAT";
			case 12328:
				return "CONFIG_ID";
			case 12329:
				return "LEVEL";
			case 12330:
				return "MAX_PBUFFER_HEIGHT";
			case 12331:
				return "MAX_PBUFFER_PIXELS";
			case 12332:
				return "MAX_PBUFFER_WIDTH";
			case 12333:
				return "NATIVE_RENDERABLE";
			case 12334:
				return "NATIVE_VISUAL_ID";
			case 12335:
				return "NATIVE_VISUAL_TYPE";
			case 12337:
				return "SAMPLES";
			case 12338:
				return "SAMPLE_BUFFERS";
			case 12339:
				return "SURFACE_TYPE";
			case 12340:
				return "TRANSPARENT_TYPE";
			case 12341:
				return "TRANSPARENT_BLUE_VALUE";
			case 12342:
				return "TRANSPARENT_GREEN_VALUE";
			case 12343:
				return "TRANSPARENT_RED_VALUE";
			case 12344:
				return "NONE";
			case 12345:
				return "BIND_TO_TEXTURE_RGB";
			case 12346:
				return "BIND_TO_TEXTURE_RGBA";
			case 12347:
				return "MIN_SWAP_INTERVAL";
			case 12348:
				return "MAX_SWAP_INTERVAL";
			case 12349:
				return "LUMINANCE_SIZE";
			case 12350:
				return "ALPHA_MASK_SIZE";
			case 12351:
				return "COLOR_BUFFER_TYPE";
			case 12352:
				return "RENDERABLE_TYPE";
			case 12353:
				return "MATCH_NATIVE_PIXMAP";
			case 12354:
				return "CONFORMANT";
			case 12368:
				return "SLOW_CONFIG";
			case 12369:
				return "NON_CONFORMANT_CONFIG";
			case 12370:
				return "TRANSPARENT_RGB";
			case 12371:
				return "VENDOR";
			case 12372:
				return "VERSION";
			case 12373:
				return "EXTENSIONS";
			case 12374:
				return "HEIGHT";
			case 12375:
				return "WIDTH";
			case 12376:
				return "LARGEST_PBUFFER";
			case 12377:
				return "DRAW";
			case 12378:
				return "READ";
			case 12379:
				return "CORE_NATIVE_ENGINE";
			case 12380:
				return "NO_TEXTURE";
			case 12381:
				return "TEXTURE_RGB";
			case 12382:
				return "TEXTURE_RGBA";
			case 12383:
				return "TEXTURE_2D";
			case 12416:
				return "TEXTURE_FORMAT";
			case 12417:
				return "TEXTURE_TARGET";
			case 12418:
				return "MIPMAP_TEXTURE";
			case 12419:
				return "MIPMAP_LEVEL";
			case 12420:
				return "BACK_BUFFER";
			case 12421:
				return "SINGLE_BUFFER";
			case 12422:
				return "RENDER_BUFFER";
			case 12423:
				return "COLORSPACE|VG_COLORSPACE";
			case 12424:
				return "ALPHA_FORMAT|VG_ALPHA_FORMAT";
			case 12425:
				return "COLORSPACE_sRGB|VG_COLORSPACE_sRGB|GL_COLORSPACE_SRGB";
			case 12426:
				return "COLORSPACE_LINEAR|VG_COLORSPACE_LINEAR|GL_COLORSPACE_LINEAR";
			case 12427:
				return "ALPHA_FORMAT_NONPRE|VG_ALPHA_FORMAT_NONPRE";
			case 12428:
				return "ALPHA_FORMAT_PRE|VG_ALPHA_FORMAT_PRE";
			case 12429:
				return "CLIENT_APIS";
			case 12430:
				return "RGB_BUFFER";
			case 12431:
				return "LUMINANCE_BUFFER";
			case 12432:
				return "HORIZONTAL_RESOLUTION";
			case 12433:
				return "VERTICAL_RESOLUTION";
			case 12434:
				return "PIXEL_ASPECT_RATIO";
			case 12435:
				return "SWAP_BEHAVIOR";
			case 12436:
				return "BUFFER_PRESERVED";
			case 12437:
				return "BUFFER_DESTROYED";
			case 12438:
				return "OPENVG_IMAGE";
			case 12439:
				return "CONTEXT_CLIENT_TYPE";
			case 12440:
				return "CONTEXT_CLIENT_VERSION|CONTEXT_MAJOR_VERSION";
			case 12441:
				return "MULTISAMPLE_RESOLVE";
			case 12442:
				return "MULTISAMPLE_RESOLVE_DEFAULT";
			case 12443:
				return "MULTISAMPLE_RESOLVE_BOX";
			case 12444:
				return "CL_EVENT_HANDLE";
			case 12445:
				return "GL_COLORSPACE";
			case 12448:
				return "OPENGL_ES_API";
			case 12449:
				return "OPENVG_API";
			case 12450:
				return "OPENGL_API";
			case 12465:
				return "GL_TEXTURE_2D";
			case 12466:
				return "GL_TEXTURE_3D";
			case 12467:
				return "GL_TEXTURE_CUBE_MAP_POSITIVE_X";
			case 12468:
				return "GL_TEXTURE_CUBE_MAP_NEGATIVE_X";
			case 12469:
				return "GL_TEXTURE_CUBE_MAP_POSITIVE_Y";
			case 12470:
				return "GL_TEXTURE_CUBE_MAP_NEGATIVE_Y";
			case 12471:
				return "GL_TEXTURE_CUBE_MAP_POSITIVE_Z";
			case 12472:
				return "GL_TEXTURE_CUBE_MAP_NEGATIVE_Z";
			case 12473:
				return "GL_RENDERBUFFER";
			case 12476:
				return "GL_TEXTURE_LEVEL";
			case 12477:
				return "GL_TEXTURE_ZOFFSET";
			case 12498:
				return "IMAGE_PRESERVED";
			case 12528:
				return "SYNC_PRIOR_COMMANDS_COMPLETE";
			case 12529:
				return "SYNC_STATUS";
			case 12530:
				return "SIGNALED";
			case 12531:
				return "UNSIGNALED";
			case 12533:
				return "TIMEOUT_EXPIRED";
			case 12534:
				return "CONDITION_SATISFIED";
			case 12535:
				return "SYNC_TYPE";
			case 12536:
				return "SYNC_CONDITION";
			case 12537:
				return "SYNC_FENCE";
			case 12539:
				return "CONTEXT_MINOR_VERSION";
			case 12541:
				return "CONTEXT_OPENGL_PROFILE_MASK";
			case 12542:
				return "SYNC_CL_EVENT";
			case 12543:
				return "SYNC_CL_EVENT_COMPLETE";
			case 12720:
				return "CONTEXT_OPENGL_DEBUG";
			case 12721:
				return "CONTEXT_OPENGL_FORWARD_COMPATIBLE";
			case 12722:
				return "CONTEXT_OPENGL_ROBUST_ACCESS";
			case 12733:
				return "CONTEXT_OPENGL_RESET_NOTIFICATION_STRATEGY";
			case 12734:
				return "NO_RESET_NOTIFICATION";
			case 12735:
				return "LOSE_CONTEXT_ON_RESET";
			default:
				return "unknown";
			}
		}
	}
}

