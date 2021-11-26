// Copyright (c) 2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Crow.SkiaBackend;
using Drawing2D;
using Glfw;
using SkiaSharp;

using vke;
using Vulkan;
using static Vulkan.Vk;

namespace Crow.SkiaBackend
{
	public class VulkanBackend : IBackend
	{
		protected Instance instance;
		protected PhysicalDevice phy;
		protected vke.Device dev;
		protected Queue graphicQueue;	//for vkvg, we must have at least a graphic queue
		protected IntPtr hWin;			//GLFW window native pointer.
		protected VkSurfaceKHR hSurf;	//Vulkan Surface
		protected SwapChain swapChain;
		protected CommandPool cmdPool;
		protected PrimaryCommandBuffer[] cmds;
		protected VkSemaphore[] drawComplete;
		protected Fence drawFence;
		VkSampleCountFlags samples = VkSampleCountFlags.SampleCount1;
		bool vsync = false;

		GRContext gr;
		VkSurface surf;
		public ISurface MainSurface => surf;

		bool tryGetPhy (vke.PhysicalDeviceCollection physicalDevices, VkPhysicalDeviceType phyType, out PhysicalDevice phy, bool swapchainSupport) {
			phy = physicalDevices.FirstOrDefault (p => p.Properties.deviceType == phyType && p.HasSwapChainSupport == swapchainSupport);
			return phy != null;
		}

		IntPtr getVkProcAddress(string name, IntPtr instance, IntPtr device) {
			using (FixedUtf8String n = new FixedUtf8String (name))
			{
				return device == IntPtr.Zero ?
					Vk.vkGetInstanceProcAddr (instance, n) :
					Vk.vkGetDeviceProcAddr (device, n);
			}
		}
		#region  CTOR
		/// <summary>
		/// Create a new offscreen backend, used in perfTests
		/// </summary>
		/// <param name="width">backend surface width</param>
		/// <param name="height">backend surface height</param>
		public VulkanBackend (int width, int height) : base () {
#if DEBUG
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

		}
		public VulkanBackend (ref IntPtr nativeWindoPointer, out bool ownGlfwWinHandle, int width, int height)
		: base ()
		{
			if (nativeWindoPointer == IntPtr.Zero) {
				Glfw3.Init ();
				Glfw3.WindowHint (WindowAttribute.ClientApi, 0);
				Glfw3.WindowHint (WindowAttribute.Resizable, 1);
				Glfw3.WindowHint (WindowAttribute.Decorated, 1);

				hWin = Glfw3.CreateWindow (width, height, "win name", MonitorHandle.Zero, IntPtr.Zero);
				if (hWin == IntPtr.Zero)
					throw new Exception ("[GLFW3] Unable to create Window");

				nativeWindoPointer = hWin;
				ownGlfwWinHandle = true;
			} else {
				hWin = nativeWindoPointer;
				ownGlfwWinHandle = false;
			}

			SwapChain.IMAGES_USAGE = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst;
			SwapChain.PREFERED_FORMAT = VkFormat.B8g8r8a8Srgb;

			List<string> instExts = new List<string> (Glfw3.GetRequiredInstanceExtensions ());
#if DEBUG
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

			cmdPool = new CommandPool (dev, graphicQueue.qFamIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
			cmds = cmdPool.AllocateCommandBuffer (swapChain.ImageCount);

			drawComplete = new VkSemaphore[swapChain.ImageCount];
			drawFence = new Fence (dev, true, "draw fence");

			for (int i = 0; i < swapChain.ImageCount; i++) {
				drawComplete[i] = dev.CreateSemaphore ();
				drawComplete[i].SetDebugMarkerName (dev, "Semaphore DrawComplete" + i);
			}

			cmdPool.SetName ("main CmdPool");
			GRVkBackendContext grVkCtx = new GRVkBackendContext {
				VkInstance = instance.Handle,
				VkPhysicalDevice = phy.Handle,
				VkQueue = graphicQueue.Handle.Handle,
				VkDevice = dev.VkDev.Handle,
				GraphicsQueueIndex = 0,
				GetProcedureAddress = getVkProcAddress
			};
			gr = GRContext.CreateVulkan (grVkCtx);

			createMainSurface ((uint)width, (uint)height);
		}
		~VulkanBackend ()
		{
			Dispose (false);
		}
		#endregion

		public IContext CreateContext(ISurface surf) => new Context (surf as VkSurface);
		public IGradient CreateGradient(GradientType gradientType, Rectangle bounds)
		{
			switch (gradientType) {
			case GradientType.Vertical:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Left, bounds.Bottom);
			case GradientType.Horizontal:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Top);
			case GradientType.Oblic:
				return new LinearGradient (bounds.Left, bounds.Top, bounds.Right, bounds.Bottom);
			case GradientType.Radial:
				throw new NotImplementedException ();
			}
			return null;
		}

		public IRegion CreateRegion () => new Crow.SkiaBackend.Region ();
		public ISurface CreateSurface(int width, int height)
			=> new VkSurface (dev, graphicQueue, gr, (int)width, (int)height, samples);
		public ISurface CreateSurface(byte[] data, int width, int height)
		{
			throw new NotImplementedException();
		}

		public byte[] LoadBitmap(Stream stream, out Size dimensions)
		{
			throw new NotImplementedException();
		}

		public ISvgHandle LoadSvg(Stream stream)
			=> new SvgHandle (stream);

		public ISvgHandle LoadSvg(string svgFragment)
		{
			throw new NotImplementedException();
		}

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
		public IContext PrepareUIFrame(IContext existingContext, IRegion clipping)
		{
			this.clipping = clipping;
			IContext ctx = existingContext;
			if (ctx == null) {
				disposeContextOnFlush = true;
				ctx = new Context (surf);
			} else
				disposeContextOnFlush = false;

			clear (ctx);

			return ctx;
		}
		public void FlushUIFrame(IContext ctx)
		{
			//surf.Canvas.Flush ();
			surf.Flush();

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
		public void ResizeMainSurface (int width, int height) {
			//resize is done on swapchain image aquisition failure
		}
		void createMainSurface (uint width, uint height) {
			dev.WaitIdle();

			surf?.Dispose ();
			surf = new VkSurface (dev, graphicQueue, gr, (int)width, (int)height, samples);

			cmdPool.Reset();

			vke.Image blitSource = surf.Img;

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
		#region IDisposable implementation
		bool isDisposed;
		public void Dispose ()
		{
			Dispose (true);
			GC.SuppressFinalize (this);
		}

		protected virtual void Dispose (bool disposing)
		{
			if (!isDisposed && disposing) {
				dev.WaitIdle ();

				surf.Dispose ();
				gr.Dispose ();

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