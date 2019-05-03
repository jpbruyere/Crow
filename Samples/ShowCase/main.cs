using System;
using System.IO;
using System.Threading;
using Crow;
using CVKL;
using VK;

namespace HelloWorld
{
	class Program : VkWindow, IValueChange {
		static void Main (string[] args) {
			using (Program vke = new Program ()) {
				vke.Run ();
			}
		}
		bool isRunning;

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

		#region crow
		#region IValueChange implementation
		public event EventHandler<Crow.ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged (string MemberName, object _value)
		{
			if (ValueChanged != null)
				ValueChanged.Invoke (this, new Crow.ValueChangeEventArgs (MemberName, _value));
		}
		#endregion

		Crow.Interface crow;

		void crow_thread_func () {
			vkvgDev = new vkvg.Device (instance.Handle, phy.Handle, dev.VkDev.Handle, presentQueue.qFamIndex,
	   			vkvg.SampleCount.Sample_8, presentQueue.index);

			crow = new Crow.Interface (vkvgDev, 800, 600);

			isRunning = true;
			while (isRunning) {
				crow.Update ();
				Thread.Sleep (5);
			}

			crow.Dispose ();
			vkvgDev.Dispose ();

			crow = null;
		}
		#endregion


		CVKL.Image uiImage;
		vkvg.Device vkvgDev;

		void initUISurface () {
			lock (crow.UpdateMutex) {
				try {
					uiImage?.Dispose ();
					uiImage = new CVKL.Image (dev, new VkImage ((ulong)crow.surf.VkImage.ToInt64 ()), VkFormat.B8g8r8a8Unorm,
						VkImageUsageFlags.Sampled, swapChain.Width, swapChain.Height);
					uiImage.SetName ("uiImage");
					uiImage.CreateView (VkImageViewType.ImageView2D, VkImageAspectFlags.Color);
					uiImage.CreateSampler (VkFilter.Nearest, VkFilter.Nearest, VkSamplerMipmapMode.Nearest, VkSamplerAddressMode.ClampToBorder);
					uiImage.Descriptor.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
				} catch (Exception ex) {
					Console.WriteLine (ex);
				}
			}
		}


		Container crowContainer;

		Program () {		
			Thread crowThread = new Thread (crow_thread_func);
			crowThread.IsBackground = true;
			crowThread.Start ();

			while (crow == null)
				Thread.Sleep (2);

			initUISurface ();

			Widget w = crow.Load ("#ShowCase.showcase.crow");
			w.DataSource = this;
			crowContainer = w.FindByName ("CrowContainer") as Container;
			crowContainer.DataSource = new object ();
			hideError ();
		}

		void Dv_SelectedItemChanged (object sender, SelectionChangeEventArgs e)
		{
			FileSystemInfo fi = e.NewValue as FileSystemInfo;
			if (fi == null)
				return;
			if (fi is DirectoryInfo)
				return;
			hideError ();
			lock (crow.UpdateMutex) {
				try {
					Widget g = crow.Load (fi.FullName);
					crowContainer.SetChild (g);
					g.DataSource = this;
				} catch (Crow.IML.InstantiatorException ex) {
					showError (ex);
				}
			}

			string source = "";
			using (Stream s = new FileStream (fi.FullName, FileMode.Open)) {
				using (StreamReader sr = new StreamReader (s)) {
					source = sr.ReadToEnd ();
				}
			}
			NotifyValueChanged ("source", source);
		}

		void showError (Crow.IML.InstantiatorException ex)
		{
			NotifyValueChanged ("ErrorMessage", ex.Path + ": " + ex.InnerException.Message);
			NotifyValueChanged ("ShowError", true);
		}
		void hideError ()
		{
			NotifyValueChanged ("ShowError", false);
		}

		void buildCommandBuffers () {
			for (int i = 0; i < swapChain.ImageCount; ++i) {
				cmds[i]?.Free ();
				cmds[i] = cmdPool.AllocateAndStart ();

				CommandBuffer cmd = cmds [i];

				swapChain.images [i].SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.Undefined, VkImageLayout.TransferDstOptimal,
					VkPipelineStageFlags.BottomOfPipe, VkPipelineStageFlags.Transfer);
				uiImage.SetLayout (cmd, VkImageAspectFlags.Color,
					VkImageLayout.ColorAttachmentOptimal, VkImageLayout.TransferSrcOptimal,
					VkPipelineStageFlags.ColorAttachmentOutput, VkPipelineStageFlags.Transfer);
					
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


		#region update
		int frameCount = 0;

		public override void Update () {
			if (++frameCount > 20) {
				NotifyValueChanged ("fps", fps);
				frameCount = 0;
			}
		}
		#endregion


		protected override void OnResize () {
			dev.WaitIdle ();

			crow.ProcessResize (new Crow.Rectangle (0, 0, (int)swapChain.Width, (int)swapChain.Height));

			initUISurface ();

			buildCommandBuffers ();
			dev.WaitIdle ();
		}
		#region Mouse and keyboard
		//protected override void onScroll (double xOffset, double yOffset)
		//{
		//	if (KeyModifiers.HasFlag (Glfw.Modifier.Shift))
		//		crow.ProcessMouseWheelChanged ((float)xOffset);
		//	else
		//		crow.ProcessMouseWheelChanged ((float)yOffset);
		//}
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
		//protected override void onChar (Glfw.CodePoint cp)
		//{
		//	if (crow.ProcessKeyPress (cp.ToChar ()))
		//		return;
		//}
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