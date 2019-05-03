using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Glfw;
using VK;
using CVKL;
using System.Threading;

namespace pbrSachaWillem {
	class Program : VkWindow, Crow.IValueChange {
		static void Main (string[] args) {
			using (Program vke = new Program ()) {
				vke.Run ();
			}
		}
		bool isRunning;

		protected override void render () {
			int idx = swapChain.GetNextImage ();

			lock (crow.RenderMutex) {
				if (idx < 0) {
					OnResize ();
					return;
				}

				presentQueue.Submit (cmds[idx], swapChain.presentComplete, drawComplete[idx]);
				presentQueue.Present (swapChain, drawComplete[idx]);
				presentQueue.WaitIdle ();
			}
		}

		#region crow

		void crow_thread_func () {
			vkvgDev = new vkvg.Device (instance.Handle, phy.Handle, dev.VkDev.Handle, presentQueue.qFamIndex,
	   			vkvg.SampleCount.Sample_4, presentQueue.index);

			crow = new Crow.Interface (vkvgDev, 800, 600);

			isRunning = true;
			//int frameCount = 0;

			while (isRunning) {
				crow.Update ();

				/*if (frameCount++ > 100) {
					for (int i = 0; i < crow.PerfMeasures.Count; i++) 
						crow.PerfMeasures[i].NotifyChanges ();
					frameCount = 0;
				}*/

				Thread.Sleep (2);
			}
			dev.WaitIdle ();
			crow.Dispose ();
			vkvgDev.Dispose ();
		}

		#region IValueChange implementation
		public event EventHandler<Crow.ValueChangeEventArgs> ValueChanged;
		public virtual void NotifyValueChanged(string MemberName, object _value)
		{
			if (ValueChanged != null)
				ValueChanged.Invoke(this, new Crow.ValueChangeEventArgs(MemberName, _value));
		}
		#endregion

		public float Gamma {
			get { return pbrPipeline.matrices.gamma; }
			set {
				if (value == pbrPipeline.matrices.gamma)
					return;
				pbrPipeline.matrices.gamma = value;
				NotifyValueChanged ("Gamma", value);
				updateViewRequested = true;
			}
		}
		public float Exposure {
			get { return pbrPipeline.matrices.exposure; }
			set {
				if (value == pbrPipeline.matrices.exposure)
					return;
				pbrPipeline.matrices.exposure = value;
				NotifyValueChanged ("Exposure", value);
				updateViewRequested = true;
			}
		}
		Crow.Interface crow;
		#endregion

		protected override void configureEnabledFeatures (VkPhysicalDeviceFeatures available_features, ref VkPhysicalDeviceFeatures features) {
			base.configureEnabledFeatures (available_features, ref features);
#if PIPELINE_STATS
			features.pipelineStatisticsQuery = true;
#endif
			features.samplerAnisotropy = true;
		}

		VkSampleCountFlags samples = VkSampleCountFlags.SampleCount4;

		Framebuffer[] frameBuffers;
		PBRPipeline pbrPipeline;

		enum DebugView {
			none,
			color,
			normal,
			occlusion,
			emissive,
			metallic,
			roughness
		}

		DebugView currentDebugView = DebugView.none;

#if PIPELINE_STATS
		PipelineStatisticsQueryPool statPool;
		TimestampQueryPool timestampQPool;
		ulong[] results;
#endif


		bool queryUpdatePrefilCube, showDebugImg, showUI = true;


		Image uiImage;

		#region ui
		//DescriptorSet dsDebugImg;
		//void initDebugImg () {
		//	dsDebugImg = descriptorPool.Allocate (descLayoutMain);
		//	pbrPipeline.envCube.debugImg.Descriptor.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
		//	DescriptorSetWrites uboUpdate = new DescriptorSetWrites (dsDebugImg, descLayoutMain);
		//	uboUpdate.Write (dev, pbrPipeline.envCube.debugImg.Descriptor);
		//}

		vkvg.Device vkvgDev;

		Pipeline uiPipeline;

		void initUIPipeline () {
			GraphicPipelineConfig cfg = GraphicPipelineConfig.CreateDefault (VkPrimitiveTopology.TriangleList, samples, false);
			cfg.RenderPass = pbrPipeline.RenderPass;
			cfg.Layout = pbrPipeline.Layout;
			cfg.AddShader (VkShaderStageFlags.Vertex, "shaders/FullScreenQuad.vert.spv");
			cfg.AddShader (VkShaderStageFlags.Fragment, "shaders/simpletexture.frag.spv");
			cfg.blendAttachments[0] = new VkPipelineColorBlendAttachmentState (true);

			uiPipeline = new GraphicPipeline (cfg);

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


		void recordDrawOverlay (CommandBuffer cmd) {
			uiPipeline.Bind (cmd);

			uiImage.SetLayout (cmd, VkImageAspectFlags.Color, VkImageLayout.ColorAttachmentOptimal, VkImageLayout.ShaderReadOnlyOptimal,
				VkPipelineStageFlags.ColorAttachmentOutput, VkPipelineStageFlags.FragmentShader);

			cmd.Draw (3, 1, 0, 0);

			uiImage.SetLayout (cmd, VkImageAspectFlags.Color, VkImageLayout.ShaderReadOnlyOptimal, VkImageLayout.ColorAttachmentOptimal,
				VkPipelineStageFlags.FragmentShader, VkPipelineStageFlags.BottomOfPipe);

			//if (!showDebugImg)
			//	return;
			//const uint debugImgSize = 256;
			//const uint debugImgMargin = 10;

			//cmd.BindDescriptorSet (uiPipeline.Layout, dsDebugImg);

			//cmd.SetViewport (debugImgSize, debugImgSize, debugImgMargin, swapChain.Height - debugImgSize - debugImgMargin);
			//cmd.SetScissor (debugImgSize, debugImgSize, (int)debugImgMargin, (int)(swapChain.Height - debugImgSize - debugImgMargin));

			//cmd.Draw (3, 1, 0, 0);
		}
		#endregion


		Vector4 lightPos = new Vector4 (1, 0, 0, 0);
		BoundingBox modelAABB;

		Program () {		
			camera.SetPosition (0, 0, 5);

			Thread crowThread = new Thread (crow_thread_func);
			crowThread.IsBackground = true;
			crowThread.Start ();

			while (crow == null)
				Thread.Sleep (5);

			initUISurface ();

			pbrPipeline = new PBRPipeline (presentQueue,
				new RenderPass (dev, swapChain.ColorFormat, dev.GetSuitableDepthFormat (), samples), uiImage);

			initUIPipeline ();

			modelAABB = pbrPipeline.model.DefaultScene.AABB;

			crow.Load ("ui/fps.crow").DataSource = this;
			//crow.Load ("#vkvgCrowTest.perfMeasures.crow").DataSource = crow;

			//crow.LoadIMLFragment ("<Window Height='200' Width='200' CornerRadius='5'/>").DataSource=this;
			//crow.LoadIMLFragment ("<Window Height='200' Width='200'/>").DataSource = this;
			crow.LoadIMLFragment (@"<Image Margin='1' Path='/mnt/devel/gts/vk.net/crow/Images/crow.png' Width='100' Height='100' Background='White' MouseEnter='{Background=Blue}' MouseLeave='{Background=White}'/>");
			//crow.LoadIMLFragment ("<Window Height='200' Width='200'/>").DataSource = this;
			UpdateFrequency = 20;
		}

		void buildCommandBuffers () {
			for (int i = 0; i < swapChain.ImageCount; ++i) {
				cmds[i]?.Free ();
				cmds[i] = cmdPool.AllocateAndStart ();
#if PIPELINE_STATS
				statPool.Begin (cmds[i]);
				recordDraw (cmds[i], frameBuffers[i]);
				statPool.End (cmds[i]);
#else
				recordDraw (cmds[i], frameBuffers[i]);
#endif

				cmds[i].End ();
			}
		}
		void recordDraw (CommandBuffer cmd, Framebuffer fb) {
			pbrPipeline.RenderPass.Begin (cmd, fb);

			cmd.SetViewport (fb.Width, fb.Height);
			cmd.SetScissor (fb.Width, fb.Height);

			pbrPipeline.RecordDraw (cmd);

			if (showUI)
				recordDrawOverlay (cmd);

			pbrPipeline.RenderPass.End (cmd);
		}


		#region update
		public override void UpdateView () {
			camera.AspectRatio = (float)swapChain.Width / swapChain.Height;

			pbrPipeline.matrices.lightDir = lightPos;
			pbrPipeline.matrices.projection = camera.Projection;
			pbrPipeline.matrices.view = camera.View;
			pbrPipeline.matrices.model = camera.Model;


			pbrPipeline.matrices.camPos = new Vector4 (
				-camera.Position.Z * (float)Math.Sin (camera.Rotation.Y) * (float)Math.Cos (camera.Rotation.X),
				 camera.Position.Z * (float)Math.Sin (camera.Rotation.X),
				 camera.Position.Z * (float)Math.Cos (camera.Rotation.Y) * (float)Math.Cos (camera.Rotation.X),
				 0
			);
			pbrPipeline.matrices.debugViewInputs = (float)currentDebugView;

			pbrPipeline.uboMats.Update (pbrPipeline.matrices, (uint)Marshal.SizeOf<PBRPipeline.Matrices> ());

			updateViewRequested = false;
		}

		int frameCount = 0;

		public override void Update () {
#if PIPELINE_STATS
			results = statPool.GetResults ();
#endif
			camera.Model *= Matrix4x4.CreateRotationY(0.05f);
			updateViewRequested = true;

			if (rebuildBuffers) {
				buildCommandBuffers ();
				rebuildBuffers = false;
			}

			if (++frameCount > 20) {
				NotifyValueChanged ("fps", fps);
				frameCount = 0;
			}
		}
		#endregion


		protected override void OnResize () {
			crow.ProcessResize (new Crow.Rectangle (0,0,(int)swapChain.Width, (int)swapChain.Height));

			initUISurface ();

			uiImage.Descriptor.imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
			DescriptorSetWrites uboUpdate = new DescriptorSetWrites (pbrPipeline.dsMain, pbrPipeline.Layout.DescriptorSetLayouts[0].Bindings[5]);
			uboUpdate.Write (dev, uiImage.Descriptor);

			UpdateView ();

			if (frameBuffers != null)
				for (int i = 0; i < swapChain.ImageCount; ++i)
					frameBuffers[i]?.Dispose ();

			frameBuffers = new Framebuffer[swapChain.ImageCount];

			for (int i = 0; i < swapChain.ImageCount; ++i) {
				frameBuffers[i] = new Framebuffer (pbrPipeline.RenderPass, swapChain.Width, swapChain.Height,
					(pbrPipeline.RenderPass.Samples == VkSampleCountFlags.SampleCount1) ? new Image[] {
						swapChain.images[i],
						null
					} : new Image[] {
						null,
						null,
						swapChain.images[i]
					});
			}

			buildCommandBuffers ();
		}

		#region Mouse and keyboard
		protected override void onMouseMove (double xPos, double yPos) {
			if (crow.ProcessMouseMove ((int)xPos, (int)yPos))
				return;

			double diffX = lastMouseX - xPos;
			double diffY = lastMouseY - yPos;
			if (MouseButton[0]) {
				camera.Rotate ((float)-diffX, (float)-diffY);
			} else if (MouseButton[1]) {
				camera.SetZoom ((float)diffY);
			} else
				return;

			updateViewRequested = true;
		}
		protected override void onMouseButtonDown (Glfw.MouseButton button) {
			if (crow.ProcessMouseButtonDown ((Crow.MouseButton)button))
				return;
			base.onMouseButtonDown (button);
		}
		protected override void onMouseButtonUp (Glfw.MouseButton button) {
			if (crow.ProcessMouseButtonUp ((Crow.MouseButton)button))
				return;
			base.onMouseButtonUp (button);
		}

		protected override void onKeyDown (Key key, int scanCode, Modifier modifiers) {
			switch (key) {
				case Key.F:
					if (modifiers.HasFlag (Modifier.Shift)) {
						pbrPipeline.envCube.debugFace--;
						if (pbrPipeline.envCube.debugFace < 0)
							pbrPipeline.envCube.debugFace = 5;
					} else {
						pbrPipeline.envCube.debugFace++;
						if (pbrPipeline.envCube.debugFace > 5)
							pbrPipeline.envCube.debugFace = 0;
					}
					queryUpdatePrefilCube = updateViewRequested = true;
					break;
				case Key.M:
					if (modifiers.HasFlag (Modifier.Shift)) {
						pbrPipeline.envCube.debugMip--;
						if (pbrPipeline.envCube.debugMip < 0)
							pbrPipeline.envCube.debugMip = (int)pbrPipeline.envCube.prefilterCube.CreateInfo.mipLevels - 1;
					} else {
						pbrPipeline.envCube.debugMip++;
						if (pbrPipeline.envCube.debugMip > pbrPipeline.envCube.prefilterCube.CreateInfo.mipLevels)
							pbrPipeline.envCube.debugMip = 0;
					}
					queryUpdatePrefilCube = updateViewRequested = true;
					break;
				case Key.P:
					showDebugImg = !showDebugImg;
					queryUpdatePrefilCube = updateViewRequested = true;
					break;
				case Key.Keypad0:
					currentDebugView = DebugView.none;
					break;
				case Key.Keypad1:
					currentDebugView = DebugView.color;
					break;
				case Key.Keypad2:
					currentDebugView = DebugView.normal;
					break;
				case Key.Keypad3:
					currentDebugView = DebugView.occlusion;
					break;
				case Key.Keypad4:
					currentDebugView = DebugView.emissive;
					break;
				case Key.Keypad5:
					currentDebugView = DebugView.metallic;
					break;
				case Key.Keypad6:
					currentDebugView = DebugView.roughness;
					break;
				case Key.Up:
					if (modifiers.HasFlag (Modifier.Shift))
						lightPos -= Vector4.UnitZ;
					else
						camera.Move (0, 0, 1);
					break;
				case Key.Down:
					if (modifiers.HasFlag (Modifier.Shift))
						lightPos += Vector4.UnitZ;
					else
						camera.Move (0, 0, -1);
					break;
				case Key.Left:
					if (modifiers.HasFlag (Modifier.Shift))
						lightPos -= Vector4.UnitX;
					else
						camera.Move (1, 0, 0);
					break;
				case Key.Right:
					if (modifiers.HasFlag (Modifier.Shift))
						lightPos += Vector4.UnitX;
					else
						camera.Move (-1, 0, 0);
					break;
				case Key.PageUp:
					if (modifiers.HasFlag (Modifier.Shift))
						lightPos += Vector4.UnitY;
					else
						camera.Move (0, 1, 0);
					break;
				case Key.PageDown:
					if (modifiers.HasFlag (Modifier.Shift))
						lightPos -= Vector4.UnitY;
					else
						camera.Move (0, -1, 0);
					break;
				case Key.F1:
					showUI = !showUI;
					rebuildBuffers = true;
					break;
				case Key.F2:
					if (modifiers.HasFlag (Modifier.Shift))
						pbrPipeline.matrices.exposure -= 0.3f;
					else
						pbrPipeline.matrices.exposure += 0.3f;
					break;
				case Key.F3:
					if (modifiers.HasFlag (Modifier.Shift))
						pbrPipeline.matrices.gamma -= 0.1f;
					else
						pbrPipeline.matrices.gamma += 0.1f;
					break;
				case Key.F4:
					if (camera.Type == Camera.CamType.FirstPerson)
						camera.Type = Camera.CamType.LookAt;
					else
						camera.Type = Camera.CamType.FirstPerson;
					Console.WriteLine ($"camera type = {camera.Type}");
					break;
				default:
					base.onKeyDown (key, scanCode, modifiers);
					return;
			}
			updateViewRequested = true;
		}
		#endregion

		#region dispose
		protected override void Dispose (bool disposing) {
			if (disposing) {
				if (!isDisposed) {
					dev.WaitIdle ();

					isRunning = false;

					for (int i = 0; i < swapChain.ImageCount; ++i)
						frameBuffers[i]?.Dispose ();

					pbrPipeline.Dispose ();

					uiPipeline.Dispose ();

					uiImage?.Dispose ();
				}
			}

			base.Dispose (disposing);
		}
		#endregion
	}
}







//		protected override void onKeyDown (Glfw.Key key, int scanCode, Modifier modifiers) {
//			switch (key) {
//				case Glfw.Key.F1:
//					if (modifiers.HasFlag (Modifier.Shift))
//						pbrPipeline.matrices.exposure -= 0.3f;
//					else
//						pbrPipeline.matrices.exposure += 0.3f;
//					break;
//				case Glfw.Key.F2:
//					if (modifiers.HasFlag (Modifier.Shift))
//						pbrPipeline.matrices.gamma -= 0.1f;
//					else
//						pbrPipeline.matrices.gamma += 0.1f;
//					break;
//				case Glfw.Key.F3:
//					if (camera.Type == Camera.CamType.FirstPerson)
//						camera.Type = Camera.CamType.LookAt;
//					else
//						camera.Type = Camera.CamType.FirstPerson;
//					Console.WriteLine ($"camera type = {camera.Type}");
//					break;
//				default:
//					base.onKeyDown (key, scanCode, modifiers);
//					return;
//			}
//			updateViewRequested = true;
//		}
//		#endregion

//		protected override void Dispose (bool disposing) {
//			if (disposing) {
//				if (!isDisposed) {
//					dev.WaitIdle ();
//					for (int i = 0; i < swapChain.ImageCount; ++i)
//						frameBuffers[i]?.Dispose ();

//					pbrPipeline.Dispose ();

//					uiPipeline.Dispose ();
//					descLayoutMain.Dispose ();
//					descriptorPool.Dispose ();

//					uiImage.Dispose ();

//					crow.Dispose ();

//					vkvgDev.Dispose ();


//				}
//			}

//			base.Dispose (disposing);
//		}


//	}
//}
