﻿using System;
using System.Threading;
using Crow;
using CVKL;
using VK;

namespace Crow
{
	public class CrowVkWin : VkWindow, IValueChange {
        #region IValueChange implementation
        public event EventHandler<Crow.ValueChangeEventArgs> ValueChanged;
        public virtual void NotifyValueChanged(string MemberName, object _value)
        {
            if (ValueChanged != null)
                ValueChanged.Invoke(this, new Crow.ValueChangeEventArgs(MemberName, _value));
        }
        #endregion

        protected Crow.Interface crow;
        protected CVKL.Image uiImage;
        protected vkvg.Device vkvgDev;
        bool isRunning;

        protected CrowVkWin() : base()
        {

            Thread crowThread = new Thread(crow_thread_func);
            crowThread.IsBackground = true;
            crowThread.Start();

            while (crow == null)
                Thread.Sleep(2);

            initUISurface();
        }

		protected virtual void onLoad () { }
		public override void Run ()
		{
			onLoad ();
			base.Run ();
		}
		protected override void render () {
			int idx = swapChain.GetNextImage ();

			if (idx < 0) {
				OnResize ();
				return;
			}

			lock (crow.RenderMutex) {
				presentQueue.Submit (cmds[idx], swapChain.presentComplete, drawComplete[idx]);
				presentQueue.Present (swapChain, drawComplete[idx]);
				presentQueue.WaitIdle ();
			}
			Thread.Sleep (1);
		}

		void crow_thread_func () {
			vkvgDev = new vkvg.Device (instance.Handle, phy.Handle, dev.VkDev.Handle, presentQueue.qFamIndex,
	   			vkvg.SampleCount.Sample_8, presentQueue.index);

			crow = new Crow.Interface (vkvgDev, 800, 600);
			crow.MouseCursorChanged +=Crow_MouseCursorChanged;; 

			isRunning = true;
			while (isRunning) {
				crow.Update ();
				Thread.Sleep (1);
			}

			crow.Dispose ();
			vkvgDev.Dispose ();
			crow = null;
		}

		void Crow_MouseCursorChanged (object sender, MouseCursorChangedEventArgs e)
		{
			switch (e.NewCursor) {
			case MouseCursor.Arrow:
				SetCursor (Glfw.CursorShape.Arrow);
				break;
			case MouseCursor.IBeam:
				SetCursor (Glfw.CursorShape.IBeam);
				break;
			case MouseCursor.Crosshair:
				SetCursor (Glfw.CursorShape.Crosshair);
				break;
			case MouseCursor.Hand:
				SetCursor (Glfw.CursorShape.Hand);
				break;
			case MouseCursor.HResize:
				SetCursor (Glfw.CursorShape.HResize);
				break;
			case MouseCursor.VResize:
				SetCursor (Glfw.CursorShape.VResize);
				break;
			default:
				SetCursor (Glfw.CursorShape.Crosshair);
				break;
			}
		}

		void initUISurface () {
			lock (crow.UpdateMutex) {
				uiImage?.Dispose ();
				uiImage = new CVKL.Image (dev, new VkImage ((ulong)crow.surf.VkImage.ToInt64 ()), VkFormat.B8g8r8a8Unorm,
					VkImageUsageFlags.Sampled, swapChain.Width, swapChain.Height);
				uiImage.SetName ("uiImage");
				uiImage.CreateView (VkImageViewType.ImageView2D, VkImageAspectFlags.Color);
				uiImage.CreateSampler (VkFilter.Nearest, VkFilter.Nearest, VkSamplerMipmapMode.Nearest, VkSamplerAddressMode.ClampToBorder);
				uiImage.Descriptor.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
			}
		}

		protected virtual void recordDraw (CommandBuffer cmd, int imageIndex) { }

		void buildCommandBuffers () {
			for (int i = 0; i < swapChain.ImageCount; ++i) {
				cmds[i]?.Free ();
				cmds[i] = cmdPool.AllocateAndStart ();

				CommandBuffer cmd = cmds [i];

				recordDraw (cmd, i);

				swapChain.images [i].SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal,
					VkPipelineStageFlags.BottomOfPipe, VkPipelineStageFlags.Transfer);
				uiImage.SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.Undefined, VkImageLayout.TransferSrcOptimal,
					VkPipelineStageFlags.Transfer, VkPipelineStageFlags.Transfer);
					
				VkImageSubresourceLayers imgSubResLayer = new VkImageSubresourceLayers {
					aspectMask = VkImageAspectFlags.Color,
					mipLevel = 0,
					baseArrayLayer = 0,
					layerCount = 1
				};
				VkImageCopy cregion = new VkImageCopy {
					srcSubresource = imgSubResLayer,
					srcOffset = default (VkOffset3D),
					dstSubresource = imgSubResLayer,
					dstOffset = default (VkOffset3D),
					extent = new VkExtent3D { width = swapChain.Width, height = swapChain.Height }
				};
				Vk.vkCmdCopyImage (cmds [i].Handle, uiImage.Handle, VkImageLayout.TransferSrcOptimal,
					swapChain.images [i].Handle, VkImageLayout.TransferDstOptimal, 1, ref cregion);

				swapChain.images [i].SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.TransferDstOptimal, VkImageLayout.PresentSrcKHR,
					VkPipelineStageFlags.Transfer, VkPipelineStageFlags.BottomOfPipe);
				uiImage.SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.TransferSrcOptimal, VkImageLayout.ColorAttachmentOptimal,
					VkPipelineStageFlags.Transfer, VkPipelineStageFlags.ColorAttachmentOutput);

				cmds [i].End ();
			}
		}

		/// <summary>
		/// rebuild command buffers if needed
		/// </summary>
		public override void Update () {
			if (rebuildBuffers) {
				buildCommandBuffers ();
				rebuildBuffers = false;
			}
		}

		protected override void OnResize () {
			dev.WaitIdle ();

			crow.ProcessResize (new Crow.Rectangle (0, 0, (int)swapChain.Width, (int)swapChain.Height));

			initUISurface ();

			buildCommandBuffers ();
			dev.WaitIdle ();
		}

		#region Mouse and keyboard
		protected override void onScroll (double xOffset, double yOffset)
		{
			if (KeyModifiers.HasFlag (Glfw.Modifier.Shift))
				crow.ProcessMouseWheelChanged ((float)xOffset);
			else
				crow.ProcessMouseWheelChanged ((float)yOffset);
		}
		protected override void onMouseMove (double xPos, double yPos)
		{
			if (crow.ProcessMouseMove ((int)xPos, (int)yPos))
				return;
		}
		protected override void onMouseButtonDown (Glfw.MouseButton button)
		{
			if (crow.ProcessMouseButtonDown ((Crow.MouseButton)button))
				return;
			base.onMouseButtonDown (button);
		}
		protected override void onMouseButtonUp (Glfw.MouseButton button)
		{
			if (crow.ProcessMouseButtonUp ((Crow.MouseButton)button))
				return;
			base.onMouseButtonUp (button);
		}
		protected override void onKeyDown (Glfw.Key key, int scanCode, Glfw.Modifier modifiers)
		{
			if (crow.ProcessKeyDown ((Crow.Key)key))
				return;
		}
		protected override void onKeyUp (Glfw.Key key, int scanCode, Glfw.Modifier modifiers)
		{
			if (crow.ProcessKeyUp ((Crow.Key)key))
				return;
		}
		protected override void onChar (Glfw.CodePoint cp)
		{
			if (crow.ProcessKeyPress (cp.ToChar ()))
				return;
		}
		#endregion

		#region dispose
		protected override void Dispose (bool disposing) {
			if (disposing) {
				if (!isDisposed) {
					dev.WaitIdle ();

					isRunning = false;
					uiImage?.Dispose ();
					while (crow != null)
						Thread.Sleep (1);
				}
			}

			base.Dispose (disposing);
		}
		#endregion
	}
}