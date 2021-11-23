// Copyright (c) 2019-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Drawing2D {
    public class StbImage : IDisposable {
        const string stblib = "stb";

        [DllImport (stblib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "stbi_load")]
        static extern IntPtr Load ([MarshalAs (UnmanagedType.LPStr)] string filename, out int x, out int y, out int channels_in_file, int desired_channels);

		[DllImport (stblib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "stbi_load_from_memory")]
        static extern IntPtr Load (IntPtr bitmap, int byteCount, out int x, out int y, out int channels_in_file, int desired_channels);        

        [DllImport (stblib, CallingConvention = CallingConvention.Cdecl, EntryPoint = "stbi_image_free")]
        static extern void FreeImage (IntPtr img);

		public readonly IntPtr Handle;
		public readonly int Width;
		public readonly int Height;
		public readonly int Channels;
		public int Size => Width * Height * Channels;

		/// <summary>
		/// Open image file with STBI library
		/// </summary>
		/// <param name="path">file path</param>
		/// <param name="requestedChannels">Force returned channels count, set 0 for original count</param>
		public StbImage (string path, int requestedChannels = 4) {
			Handle = StbImage.Load (path, out Width, out Height, out Channels, requestedChannels);
			if (Handle == IntPtr.Zero)
				throw new Exception ($"STBI image loading error.");
			if (requestedChannels > 0)
				Channels = requestedChannels;
		}
		/// <summary>
		/// Open image in memory with STBI library
		/// </summary>
		/// <param name="bitmap">raw bitmap datas</param>
		/// <param name="bitmapByteCount">Bitmap byte count.</param>
		/// <param name="requestedChannels">Force returned channels count, set 0 for original count</param>
		public StbImage (IntPtr bitmap, ulong bitmapByteCount, int requestedChannels = 4) {
			Handle = StbImage.Load (bitmap, (int)bitmapByteCount, out Width, out Height, out Channels, requestedChannels);
			if (Handle == IntPtr.Zero)
				throw new Exception ($"STBI image loading error.");
			if (requestedChannels > 0)
				Channels = requestedChannels;
		}
		public StbImage (Stream stream, int requestedChannels = 4)
		{
			byte [] buff = new byte [stream.Length];
			stream.Read (buff, 0, (int)stream.Length);
			GCHandle hnd = GCHandle.Alloc (buff, GCHandleType.Pinned);
			Handle = StbImage.Load (hnd.AddrOfPinnedObject(), (int)stream.Length, out Width, out Height, out Channels, requestedChannels);
			hnd.Free ();
			if (Handle == IntPtr.Zero)
				throw new Exception ($"STBI image loading error.");
			if (requestedChannels > 0)
				Channels = requestedChannels;
		}
		public void Dispose () {
			StbImage.FreeImage (Handle);
		}
	}
}
