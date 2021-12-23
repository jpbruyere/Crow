// Copyright (c) 2019-2021  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Glfw;
using vke;
using Vulkan;
using static Vulkan.Vk;
using Device = vke.Device;

namespace Crow.Drawing {
	/// <summary>
	/// Base class for offscreen vulkan context without swapchain
	/// </summary>
	public abstract class VulkanContextBase : IDisposable {
		protected Interface iface;
		public uint width { get; protected set; }
		public uint height { get; protected set; }

		protected Instance instance;
		protected PhysicalDevice phy;
		protected vke.Device dev;
		protected Queue graphicQueue;//for vkvg, we must have at least a graphic queue
		public Crow.Drawing.Device VkvgDevice { get; protected set; }

		public void WaitIdle() => dev.WaitIdle ();

		public VulkanContextBase (Interface iface) {
			this.iface = iface;
		}


		protected void createVkvgDevice () => VkvgDevice =
			new Crow.Drawing.Device (instance.Handle, phy.Handle, dev.VkDev.Handle, graphicQueue.qFamIndex, SampleCount.Sample_8);

		public abstract void CreateSurface (int width, int height, ref Surface surf);
		public abstract bool render ();

		#region IDisposable Support
		protected bool isDisposed;
		protected virtual void Dispose (bool disposing) {
			if (!isDisposed) {
				dev.WaitIdle ();

				VkvgDevice.Dispose ();

				if (disposing) {
					dev.Dispose ();
					instance.Dispose ();
				} else
					Debug.WriteLine ("a VulkanContext has not been correctly disposed");

				isDisposed = true;
			}
		}
		~VulkanContextBase () {
			Dispose (false);
		}
		public void Dispose () {
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		#endregion
	}
	//vulkan context rendering to preallocated bitmap
	public class OffscreenVulkanContext : VulkanContextBase {
		public IntPtr bitmap { get; private set; }
		Surface surface;
		/// <summary>
		/// Preallocated Pointer to output bitmap
		/// </summary>
		/// <param name="pBitmap"></param>
		public OffscreenVulkanContext (Interface iface) : base (iface) {
#if DEBUG
			instance = new Instance (Ext.I.VK_EXT_debug_utils);
#else
			instance = new Instance ();
#endif
			phy = instance.GetAvailablePhysicalDevice ().FirstOrDefault ();
			VkPhysicalDeviceFeatures enabledFeatures = default;
			dev = new vke.Device (phy);
			graphicQueue = new Queue (dev, VkQueueFlags.Graphics);
			dev.Activate (enabledFeatures);

			createVkvgDevice ();
		}
		public override void CreateSurface (int width, int height, ref Surface surf) {
			surf?.Dispose ();
			surface = new Surface (VkvgDevice, width, height);
			surf = surface;

			this.width = (uint)width;
			this.height = (uint)height;

			if (bitmap != IntPtr.Zero)
				Marshal.FreeHGlobal (bitmap);
			bitmap = Marshal.AllocHGlobal (height * width * 4);
			Console.WriteLine($"vkCtx.CreateOffscreenSurface: w:{width} h:{height}");
		}
		public override bool render () {
			surface.WriteTo (bitmap);
			Console.WriteLine($"vkCtx.Render(WriteTo): w:{width} h:{height}");
			return true;
		}
		protected override void Dispose (bool disposing) {
			if (!isDisposed) {
				dev.WaitIdle ();

				surface?.Dispose ();
				if (bitmap != IntPtr.Zero)
					Marshal.FreeHGlobal (bitmap);

				base.Dispose (disposing);
			}
		}
	}
	/// <summary>
	/// Base class to build vulkan application.
	/// Provide default swapchain with its command pool and buffers per image and the main present queue
	/// </summary>
	public class VulkanContext : VulkanContextBase {
		IntPtr hWin;/** GLFW window native pointer. */
		/**Vulkan Surface */
		protected VkSurfaceKHR hSurf;
		protected SwapChain swapChain;
		protected CommandPool cmdPool;
		protected PrimaryCommandBuffer[] cmds;
		protected VkSemaphore[] drawComplete;
		protected Fence drawFence;

		protected uint fps { get; private set; }
		protected bool updateViewRequested = true;

		/// <summary>readonly GLFW window handle</summary>
		public IntPtr WindowHandle => hWin;

		uint frameCount;
		Stopwatch frameChrono;

		public VulkanContext (Interface iface, IntPtr hWin, uint _width, uint _height, bool vsync = false) : base (iface) {
			this.hWin = hWin;

			SwapChain.IMAGES_USAGE = VkImageUsageFlags.ColorAttachment | VkImageUsageFlags.TransferDst;
			SwapChain.PREFERED_FORMAT = VkFormat.B8g8r8a8Srgb;

			//Instance.VALIDATION = true;
			Instance.RENDER_DOC_CAPTURE = true;
			List<string> instExts = new List<string> (Glfw3.GetRequiredInstanceExtensions ());
#if DEBUG
			instExts.Add (Ext.I.VK_EXT_debug_utils);
#endif

			instance = new Instance (instExts.ToArray());
			hSurf = instance.CreateSurface (hWin);

			phy = instance.GetAvailablePhysicalDevice ().FirstOrDefault (p => p.HasSwapChainSupport);

			VkPhysicalDeviceFeatures enabledFeatures = default;

			//First create the c# device class
			dev = new vke.Device (phy);

			graphicQueue = new PresentQueue (dev, VkQueueFlags.Graphics, hSurf);

			//activate the device to have effective queues created accordingly to what's available
			dev.Activate (enabledFeatures, Ext.D.VK_KHR_swapchain);

			swapChain = new SwapChain (graphicQueue as PresentQueue, _width, _height, SwapChain.PREFERED_FORMAT,
				vsync ? VkPresentModeKHR.FifoKHR : VkPresentModeKHR.ImmediateKHR);
			swapChain.Activate ();

			width = swapChain.Width;
			height = swapChain.Height;

			cmdPool = new CommandPool (dev, graphicQueue.qFamIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
			cmds = cmdPool.AllocateCommandBuffer (swapChain.ImageCount);

			drawComplete = new VkSemaphore[swapChain.ImageCount];
			drawFence = new Fence (dev, true, "draw fence");

			for (int i = 0; i < swapChain.ImageCount; i++) {
				drawComplete[i] = dev.CreateSemaphore ();
				drawComplete[i].SetDebugMarkerName (dev, "Semaphore DrawComplete" + i);
			}

			cmdPool.SetName ("main CmdPool");

			createVkvgDevice ();
		}

		public override void CreateSurface (int width, int height, ref Surface surf) {
			WaitIdle();

			blitSource?.Dispose ();
			surf?.Dispose ();
			surf = new Surface (VkvgDevice, width, height);
			buildBlitCommand (surf);

			WaitIdle();
		}

		internal vke.Image blitSource;

		void buildBlitCommand (Crow.Drawing.Surface surf) {
			//Console.WriteLine ($"build blit w:{width} h:{height}");
			cmdPool.Reset();

			blitSource = new vke.Image (dev, new VkImage((ulong)surf.VkImage.ToInt64()), Vulkan.VkFormat.B8g8r8a8Srgb,
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
		}
		/// <summary>
		/// Main render method called each frame. get next swapchain image, process resize if needed, submit and present to the presentQueue.
		/// Wait QueueIdle after presenting.
		/// </summary>
		public override bool render () {
			WaitIdle();

			int idx = swapChain.GetNextImage ();
			if (idx < 0) {
				width = swapChain.Width;
				height = swapChain.Height;
				//Console.WriteLine ($"get next image failed w:{width} h:{height}");
				return false;
			}

			if (cmds[idx] == null)
				return false;

			drawFence.Wait ();
			drawFence.Reset ();

			graphicQueue.Submit (cmds[idx], swapChain.presentComplete, drawComplete[idx], drawFence);
			(graphicQueue as PresentQueue).Present (swapChain, drawComplete[idx]);

			WaitIdle();
			iface.IsDirty = false;
			return true;
		}
		protected override void Dispose (bool disposing) {
			if (!isDisposed) {
				dev.WaitIdle ();

				for (int i = 0; i < swapChain.ImageCount; i++) {
					dev.DestroySemaphore (drawComplete[i]);
					cmds[i].Free ();
				}
				drawFence.Dispose ();
				swapChain.Dispose ();

				vkDestroySurfaceKHR (instance.Handle, hSurf, IntPtr.Zero);

				if (disposing)
					cmdPool.Dispose ();

				base.Dispose (disposing);
			}
		}
	}
}
