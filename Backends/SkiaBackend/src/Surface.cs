// Copyright (c) 2018-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Drawing2D;
using SkiaSharp;
using vke;
using Vulkan;

namespace Crow.SkiaBackend
{
	public class VkSurface : ISurface {
		GRBackendRenderTarget rt;
		SKSurface skSurf;
		GRVkImageInfo imgInfo;
		vke.Image img;

		internal VkSurface (Device dev, Queue queue, GRContext gr, int width, int height, VkSampleCountFlags samples)
		{
			img = new vke.Image (dev, VkFormat.R8g8b8a8Unorm,
				VkImageUsageFlags.TransferSrc | VkImageUsageFlags.ColorAttachment, VkMemoryPropertyFlags.DeviceLocal,
				(uint)width, (uint)height);

			imgInfo = new GRVkImageInfo {
				CurrentQueueFamily = queue.qFamIndex,
				Format = (uint)img.Format,
				Image = img.Handle.Handle,
				ImageLayout = (uint)VkImageLayout.ColorAttachmentOptimal,
				ImageTiling = (uint)VkImageTiling.Optimal,
				LevelCount = img.CreateInfo.mipLevels,
				Protected = false,
				Alloc = new GRVkAlloc()
				{
					Memory = img.Memory.Handle,
					Flags = 0,
					Offset = 0,
					Size = img.AllocatedDeviceMemorySize
				}
			};
			rt = new GRBackendRenderTarget (width, height, (int)samples, imgInfo);
			skSurf = SKSurface.Create (gr, rt, GRSurfaceOrigin.TopLeft, SKColorType.Rgba8888, SKColorSpace.CreateSrgb());
		}
		~VkSurface ()
		{
			Dispose (false);
		}

		public IntPtr Handle => throw new NotImplementedException();
		public int Width => (int)img.Width;
		public int Height => (int)img.Height;
		internal SKCanvas Canvas => skSurf.Canvas;
		internal SKSurface SkSurf => skSurf;
		internal vke.Image Img => img;


		public void Flush() => skSurf.Flush ();

		public void Resize(int width, int height)
		{
			//throw new NotImplementedException();
		}
		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || skSurf == null)
				return;
			skSurf.Dispose ();
			rt.Dispose ();
			img.Dispose ();
		}
		#endregion

	}
	public class Surface: ISurface
	{
		protected SKSurface skSurf;
		SKImageInfo sKImgInfo;
		public IntPtr Handle => throw new NotImplementedException();
		public int Width => sKImgInfo.Width;
		public int Height => sKImgInfo.Height;
		internal SKCanvas Canvas => skSurf.Canvas;
		internal SKSurface SkSurf => skSurf;

		#region CTOR
		public Surface (int width, int height)
		{
			sKImgInfo = new SKImageInfo (width, height);
			skSurf = SKSurface.Create (sKImgInfo);
		}
		Surface (IntPtr devHandle, int width, int height)
		{

		}
		#endregion
		~Surface ()
		{
			Dispose (false);
		}

		public void Resize(int width, int height)
		{

		}
		public void Flush ()
		{
			skSurf.Canvas.Flush ();
		}

		/*public void WriteToPng (string path) {
		}
		public void WriteTo (IntPtr bitmap) {
		}
		public void Clear () {
		}*/

		#region IDisposable implementation
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!disposing || skSurf == null)
				return;
			skSurf.Dispose ();
		}
		#endregion
	}
}

