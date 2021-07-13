// Copyright (c) 2019  Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
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

namespace vkvg {
	/// <summary>
	/// Base class to build vulkan application.
	/// Provide default swapchain with its command pool and buffers per image and the main present queue
	/// </summary>
	public class VulkanContext : IDisposable {

		/** GLFW window native pointer. */
		IntPtr hWin;
		/**Vulkan Surface */
		protected VkSurfaceKHR hSurf;
		/**vke Instance encapsulating a VkInstance. */
		protected Instance instance;	
		/**vke Physical device associated with this window*/
		protected PhysicalDevice phy;
		/**vke logical device */			
		protected vke.Device dev;
		protected PresentQueue presentQueue;
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

		string[] EnabledInstanceExtensions => null;

		string[] EnabledDeviceExtensions => new string[] { Ext.D.VK_KHR_swapchain };

		uint width, height;

		public VulkanContext (IntPtr hWin, uint _width, uint _height, bool vsync = false) {
			this.hWin = hWin;

			List<string> instExts = new List<string> (Glfw3.GetRequiredInstanceExtensions ());
			if (EnabledInstanceExtensions != null)
				instExts.AddRange (EnabledInstanceExtensions);

			instance = new Instance (instExts.ToArray());
			hSurf = instance.CreateSurface (hWin);

			phy = instance.GetAvailablePhysicalDevice ().FirstOrDefault (p => p.HasSwapChainSupport);

			VkPhysicalDeviceFeatures enabledFeatures = default;

			//First create the c# device class
			dev = new vke.Device (phy);

			presentQueue = new PresentQueue (dev, VkQueueFlags.Graphics, hSurf);

			//activate the device to have effective queues created accordingly to what's available
			dev.Activate (enabledFeatures, EnabledDeviceExtensions);

			swapChain = new SwapChain (presentQueue as PresentQueue, _width, _height, SwapChain.PREFERED_FORMAT,
				vsync ? VkPresentModeKHR.FifoKHR : VkPresentModeKHR.ImmediateKHR);
			swapChain.Activate ();

			width = swapChain.Width;
			height = swapChain.Height;

			cmdPool = new CommandPool (dev, presentQueue.qFamIndex, VkCommandPoolCreateFlags.ResetCommandBuffer);
			cmds = cmdPool.AllocateCommandBuffer (swapChain.ImageCount);

			drawComplete = new VkSemaphore[swapChain.ImageCount];
			drawFence = new Fence (dev, true, "draw fence");

			for (int i = 0; i < swapChain.ImageCount; i++) {
				drawComplete[i] = dev.CreateSemaphore ();
				drawComplete[i].SetDebugMarkerName (dev, "Semaphore DrawComplete" + i);
			}

			cmdPool.SetName ("main CmdPool");			
		}

		public vkvg.Device CreateVkvgDevice () => 
			new vkvg.Device (instance.Handle, phy.Handle, dev.VkDev.Handle, presentQueue.qFamIndex);

		internal vke.Image blitSource;
		
		public void BuildBlitCommand (vkvg.Surface surf) {
			
			blitSource = new vke.Image (dev, new VkImage((ulong)surf.VkImage.ToInt64()), Vulkan.VkFormat.R8g8b8a8Unorm,
				Vulkan.VkImageUsageFlags.TransferSrc | Vulkan.VkImageUsageFlags.TransferDst | Vulkan.VkImageUsageFlags.ColorAttachment,
				width, height);

			for (int i = 0; i < swapChain.ImageCount; i++) {
				vke.Image blitDest = swapChain.images[i];
				vke.PrimaryCommandBuffer cmd = cmds[i];
				cmd.Start();
				
				blitDest.SetLayout (cmd, VkImageAspectFlags.Color, VkImageLayout.TransferDstOptimal);
				blitSource.SetLayout (cmd, VkImageAspectFlags.Color, VkImageLayout.TransferSrcOptimal);

				blitSource.BlitTo (cmd, blitDest, VkFilter.Nearest);

				blitDest.SetLayout (cmd, VkImageAspectFlags.Color, VkImageLayout.PresentSrcKHR);
				blitSource.SetLayout (cmd, VkImageAspectFlags.Color, VkImageLayout.ColorAttachmentOptimal);

				cmd.End ();
			}
		}
		/// <summary>
		/// Main render method called each frame. get next swapchain image, process resize if needed, submit and present to the presentQueue.
		/// Wait QueueIdle after presenting.
		/// </summary>
		public void render () {
			int idx = swapChain.GetNextImage ();
			if (idx < 0) {
				width = swapChain.Width;
				height = swapChain.Height;
				return;
			}

			if (cmds[idx] == null)
				return;

			drawFence.Wait ();
			drawFence.Reset ();

			presentQueue.Submit (cmds[idx], swapChain.presentComplete, drawComplete[idx], drawFence);
			presentQueue.Present (swapChain, drawComplete[idx]);			
		}

		#region IDisposable Support
		protected bool isDisposed;

		protected virtual void Dispose (bool disposing) {
			if (!isDisposed) {
				dev.WaitIdle ();

				for (int i = 0; i < swapChain.ImageCount; i++) {
					dev.DestroySemaphore (drawComplete[i]);
					cmds[i].Free ();
				}
				drawFence.Dispose ();
				swapChain.Dispose ();

				vkDestroySurfaceKHR (instance.Handle, hSurf, IntPtr.Zero);

				if (disposing) {
					cmdPool.Dispose ();
					dev.Dispose ();
					instance.Dispose ();
				} else
					Debug.WriteLine ("a VkWindow has not been correctly disposed");

				isDisposed = true;
			}
		}

		~VulkanContext () {
			Dispose (false);
		}
		public void Dispose () {
			Dispose (true);
			GC.SuppressFinalize (this);
		}
		#endregion
	}
}
