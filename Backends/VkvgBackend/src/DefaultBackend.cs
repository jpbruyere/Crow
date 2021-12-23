// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Drawing2D;
using Glfw;
using vke;
using Vulkan;
using static Vulkan.Vk;
using Device = vke.Device;

namespace Crow.Backends
{
	public class DefaultBackend : CrowBackend
	{
		protected Instance instance;
		protected PhysicalDevice phy;
		protected vke.Device dev;
		protected Queue graphicQueue;	//for vkvg, we must have at least a graphic queue
		protected VkSurfaceKHR hSurf;	//Vulkan Surface
		protected SwapChain swapChain;
		protected CommandPool cmdPool;
		protected PrimaryCommandBuffer[] cmds;
		protected VkSemaphore[] drawComplete;
		protected Fence drawFence;
		Crow.VkvgBackend.Surface surf;
		Crow.VkvgBackend.Device vkvgDev;
		SampleCount samples = SampleCount.Sample_1;
		bool vsync = false;

		bool tryGetPhy (vke.PhysicalDeviceCollection physicalDevices, VkPhysicalDeviceType phyType, out PhysicalDevice phy, bool swapchainSupport) {
			phy = physicalDevices.FirstOrDefault (p => p.Properties.deviceType == phyType && p.HasSwapChainSupport == swapchainSupport);
			return phy != null;
		}
		public DefaultBackend (int width, int height)
		: base (width, height, IntPtr.Zero)
		{
#if DEBUG
			//Instance.RENDER_DOC_CAPTURE = true;
			instance = new Instance (Ext.I.VK_EXT_debug_utils);
#else
			instance = new Instance ();
#endif
			vke.PhysicalDeviceCollection phys = instance.GetAvailablePhysicalDevice ();
			if (phys.Count() == 0)
				throw new Exception ("[Crow.VkvgBackend] No vulkan hardware found.");

			if (!tryGetPhy (phys, VkPhysicalDeviceType.DiscreteGpu, out phy, false))
				if (!tryGetPhy (phys, VkPhysicalDeviceType.IntegratedGpu, out phy, false))
					phy = phys[0];

			VkPhysicalDeviceFeatures enabledFeatures = default;
			dev = new vke.Device (phy);
			graphicQueue = new Queue (dev, VkQueueFlags.Graphics);
			dev.Activate (enabledFeatures);

			vkvgDev = new Crow.VkvgBackend.Device (
				instance.Handle, phy.Handle, dev.VkDev.Handle, graphicQueue.qFamIndex, samples);

			surf = new Crow.VkvgBackend.Surface (vkvgDev, (int)width, (int)height);
		}
		public DefaultBackend (int width, int height, IntPtr nativeWindoPointer)
		: base (width, height, nativeWindoPointer)
		{
			if (nativeWindoPointer == IntPtr.Zero) {
				Glfw3.Init ();
				Glfw3.WindowHint (WindowAttribute.ClientApi, 0);
				Glfw3.WindowHint (WindowAttribute.Resizable, 1);
				Glfw3.WindowHint (WindowAttribute.Decorated, 1);

				hWin = Glfw3.CreateWindow (width, height, "win name", MonitorHandle.Zero, IntPtr.Zero);
				if (hWin == IntPtr.Zero)
					throw new Exception ("[GLFW3] Unable to create Window");

			}

			SwapChain.IMAGES_USAGE = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst;
			SwapChain.PREFERED_FORMAT = VkFormat.B8g8r8a8Unorm;

			List<string> instExts = new List<string> (Glfw3.GetRequiredInstanceExtensions ());
#if DEBUG
			//Instance.RENDER_DOC_CAPTURE = true;
			instExts.Add (Ext.I.VK_EXT_debug_utils);
#endif
			instance = new Instance (instExts.ToArray());
			hSurf = instance.CreateSurface (hWin);

			vke.PhysicalDeviceCollection phys = instance.GetAvailablePhysicalDevice ();
			if (phys.Count() == 0)
				throw new Exception ("[Crow.VkvgBackend] No vulkan hardware found.");

			if (!tryGetPhy (phys, VkPhysicalDeviceType.DiscreteGpu, out phy, true))
				if (!tryGetPhy (phys, VkPhysicalDeviceType.IntegratedGpu, out phy, true))
					phy = phys[0];

			VkPhysicalDeviceFeatures enabledFeatures = default;
			dev = new vke.Device (phy);
			graphicQueue = new PresentQueue (dev, VkQueueFlags.Graphics, hSurf);

			dev.Activate (enabledFeatures, Ext.D.VK_KHR_swapchain);

			swapChain = new SwapChain (graphicQueue as PresentQueue, (uint)width, (uint)height, SwapChain.PREFERED_FORMAT,
				vsync ? VkPresentModeKHR.FifoKHR : VkPresentModeKHR.ImmediateKHR);
			swapChain.Activate ();

			/*width = swapChain.Width;
			height = swapChain.Height;*/

			cmdPool = new CommandPool (dev, graphicQueue.qFamIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
			cmds = cmdPool.AllocateCommandBuffer (swapChain.ImageCount);

			drawComplete = new VkSemaphore[swapChain.ImageCount];
			drawFence = new Fence (dev, true, "draw fence");

			for (int i = 0; i < swapChain.ImageCount; i++) {
				drawComplete[i] = dev.CreateSemaphore ();
				drawComplete[i].SetDebugMarkerName (dev, "Semaphore DrawComplete" + i);
			}

			cmdPool.SetName ("main CmdPool");

			vkvgDev = new Crow.VkvgBackend.Device (
				instance.Handle, phy.Handle, dev.VkDev.Handle, graphicQueue.qFamIndex, samples);
			vkvgDev.SetDpy (72,72);

			createMainSurface ((uint)width, (uint)height);
		}
		~DefaultBackend ()
		{
			Dispose (false);
		}
		public override ISurface CreateSurface(int width, int height)
			=> new Crow.VkvgBackend.Surface (vkvgDev, width, height);
		public override ISurface CreateSurface(byte[] data, int width, int height)
			=> new Crow.VkvgBackend.Surface (vkvgDev, data, width, height);
		public override ISurface MainSurface => surf;
		public override IRegion CreateRegion () => new Crow.VkvgBackend.Region ();
		public override IContext CreateContext (ISurface surf)
		{
			Crow.VkvgBackend.Context gr = new Crow.VkvgBackend.Context (surf as Crow.VkvgBackend.Surface);
			return gr;
		}
		//IPattern CreatePattern (PatternType patternType);
		public override IGradient CreateGradient (GradientType gradientType, Rectangle bounds)
		{
			switch (gradientType) {
			case GradientType.Vertical:
				return new Crow.VkvgBackend.LinearGradient (0, bounds.Top, 0, bounds.Bottom);
			case GradientType.Horizontal:
				return new Crow.VkvgBackend.LinearGradient (bounds.Left, 0, bounds.Right, 0);
			case GradientType.Oblic:
				return new Crow.VkvgBackend.LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
			case GradientType.Radial:
				throw new NotImplementedException ();
			}
			return null;
		}
		public override byte[] LoadBitmap (Stream stream, out Size dimensions)
		{
			byte[] image;
#if STB_SHARP
			StbImageSharp.ImageResult stbi = StbImageSharp.ImageResult.FromStream (stream, StbImageSharp.ColorComponents.RedGreenBlueAlpha);
			image = new byte[stbi.Data.Length];

			Array.Copy (stbi.Data, image, stbi.Data.Length);
			dimensions = new Size (stbi.Width, stbi.Height);
#else
			using (StbImage stbi = new StbImage (stream)) {
				image = new byte [stbi.Size];
				Marshal.Copy (stbi.Handle, image, 0, stbi.Size);
				dimensions = new Size (stbi.Width, stbi.Height);
			}
#endif
			return image;
		}
		public override ISvgHandle LoadSvg(Stream stream)
		{
			using (BinaryReader sr = new BinaryReader (stream))
				return new Crow.VkvgBackend.SvgHandle(vkvgDev, sr.ReadBytes ((int)stream.Length));
		}
		public override ISvgHandle LoadSvg(string svgFragment) =>
			new Crow.VkvgBackend.SvgHandle (vkvgDev, System.Text.Encoding.Unicode.GetBytes (svgFragment));
		bool disposeContextOnFlush;
		IRegion clipping;
		protected void clear(IContext ctx) {
			for (int i = 0; i < clipping.NumRectangles; i++)
				ctx.Rectangle (clipping.GetRectangle (i));

			ctx.ClipPreserve ();
			ctx.Operator = Operator.Clear;
			ctx.Fill ();
			ctx.Operator = Operator.Over;
		}
		public override IContext PrepareUIFrame(IContext existingContext, IRegion clipping)
		{
			this.clipping = clipping;
			IContext ctx = existingContext;
			if (ctx == null) {
				disposeContextOnFlush = true;
				ctx = new Crow.VkvgBackend.Context (surf);
			} else
				disposeContextOnFlush = false;

			clear (ctx);

			return ctx;
		}
		public override void FlushUIFrame(IContext ctx)
		{
			if (disposeContextOnFlush)
				ctx.Dispose ();
			clipping = null;

			dev.WaitIdle();

			int idx = swapChain.GetNextImage ();
			if (idx < 0) {
				createMainSurface (swapChain.Width, swapChain.Height);
				return;
			}

			drawFence.Wait ();
			drawFence.Reset ();

			graphicQueue.Submit (cmds[idx], swapChain.presentComplete, drawComplete[idx], drawFence);
			(graphicQueue as PresentQueue).Present (swapChain, drawComplete[idx]);

			dev.WaitIdle();
		}
		public override void ResizeMainSurface (int width, int height) {
			//resize is done on swapchain image aquisition failure
		}
		vke.Image blitSource;
		void createMainSurface (uint width, uint height) {
			dev.WaitIdle();

			blitSource?.Dispose ();
			surf?.Dispose ();
			surf = new Crow.VkvgBackend.Surface (vkvgDev, (int)width, (int)height);

			cmdPool.Reset();

			blitSource = new vke.Image (dev,
				new Vulkan.VkImage((ulong)surf.VkImage.ToInt64()),
				Vulkan.VkFormat.B8g8r8a8Unorm,
				Vulkan.VkImageUsageFlags.TransferSrc | Vulkan.VkImageUsageFlags.TransferDst | Vulkan.VkImageUsageFlags.ColorAttachment,
				width, height);

			for (int i = 0; i < swapChain.ImageCount; i++) {
				vke.Image blitDest = swapChain.images[i];
				vke.PrimaryCommandBuffer cmd = cmds[i];
				cmd.Start();

				blitDest.SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal,
					VkPipelineStageFlags.BottomOfPipe, VkPipelineStageFlags.Transfer);

				blitSource.SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.ColorAttachmentOptimal, VkImageLayout.TransferSrcOptimal,
					VkPipelineStageFlags.ColorAttachmentOutput, VkPipelineStageFlags.Transfer);

				blitSource.BlitTo (cmd, blitDest, VkFilter.Nearest);

				blitDest.SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.TransferDstOptimal, VkImageLayout.PresentSrcKHR,
					VkPipelineStageFlags.Transfer, VkPipelineStageFlags.BottomOfPipe);

				blitSource.SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.TransferSrcOptimal, VkImageLayout.ColorAttachmentOptimal,
					VkPipelineStageFlags.Transfer, VkPipelineStageFlags.ColorAttachmentOutput);

				cmd.End ();
			}
			dev.WaitIdle ();
		}
		#region IDispose implementation
		public override void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		protected virtual void Dispose (bool disposing)
		{
			if (!isDisposed && disposing) {
				dev.WaitIdle ();

				surf.Dispose ();
				vkvgDev.Dispose ();

				for (int i = 0; i < swapChain.ImageCount; i++) {
					dev.DestroySemaphore (drawComplete[i]);
					cmds[i].Free ();
				}
				drawFence.Dispose ();
				swapChain.Dispose ();
				cmdPool.Dispose ();

				vkDestroySurfaceKHR (instance.Handle, hSurf, IntPtr.Zero);

				dev.Dispose ();
				instance.Dispose ();
			}
			isDisposed = true;
		}
		#endregion
	}
}
