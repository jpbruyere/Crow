//
// Main.cs
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
using System.Diagnostics;
using System.IO;
using OpenTK.Platform.Linux;
using System.Collections.Generic;
using OpenTK;
using OpenTK.Platform.Egl;
using Cairo;

namespace testDrm
{
	// Stores platform-specific information about a display
	class LinuxDisplay
	{
		public int FD;
		public IntPtr Connector;
		public IntPtr Crtc;
		public IntPtr Encoder;

		unsafe public ModeConnector* pConnector { get { return (ModeConnector*)Connector; } }
		unsafe public ModeCrtc* pCrtc { get { return (ModeCrtc*)Crtc; } }
		unsafe public ModeEncoder* pEncoder { get { return (ModeEncoder*)Encoder; } }
		/*
        public ModeInfo Mode
        {
            get
            {
                if (Crtc == IntPtr.Zero)
                    throw new InvalidOperationException();

                unsafe
                {
                    return pCrtc->mode;
                }
            }
        }
        */

		public ModeInfo OriginalMode;

		public int Id
		{
			get
			{
				if (Crtc == IntPtr.Zero)
					throw new InvalidOperationException();

				unsafe
				{
					return (int)pCrtc->crtc_id;
				}
			}
		}

		public LinuxDisplay(int fd, IntPtr c, IntPtr e, IntPtr r)
		{
			FD = fd;
			Connector = c;
			Encoder = e;
			Crtc = r;
			unsafe
			{
				OriginalMode = pCrtc->mode; // in case we change resolution later on
			}
		}
	}
	static class TestDrm
	{
		const string gpu_path = "/dev/dri"; // card0, card1, ...

		[STAThread]
		static void Main ()
		{
			

//			using (Surface s = new ImageSurface (Format.Argb32,800,600)) {
//				
//			}
//			IntPtr gbm_device;
//			IntPtr egl_display;
//
//			int gpu_fd = CreateDisplay(out gbm_device, out egl_display);
			using (DrmDevice drm = new DrmDevice ()) {
				drm.RenderingLoop ();
			}
		}

		static int CreateDisplay(out IntPtr gbm_device, out IntPtr egl_display)
		{
			// Query all GPUs until we find one that has a connected display.
			// This is necessary in multi-gpu systems, where only one GPU
			// can output a signal.
			// Todo: allow OpenTK to drive multiple GPUs
			// Todo: allow OpenTK to run on an offscreen GPU
			// Todo: allow the user to pick a GPU
			int fd = 0;
			gbm_device = IntPtr.Zero;
			egl_display = IntPtr.Zero;

			var files = Directory.GetFiles(gpu_path);
			foreach (var gpu in files)
			{
				if (System.IO.Path.GetFileName(gpu).StartsWith("card"))
				{
					int test_fd = SetupDisplay(gpu, out gbm_device, out egl_display);
					if (test_fd >= 0)
					{
						try
						{
							if (QueryDisplays(test_fd, null))
							{
								fd = test_fd;
								break;
							}
						}
						catch (Exception e)
						{
							Debug.WriteLine(e.ToString());
						}

						Console.WriteLine("[KMS] GPU '{0}' is not connected, skipping.", gpu);
						Libc.close(test_fd);
					}
				}
			}

			if (fd == 0)
			{
				Console.WriteLine("[Error] No valid GPU found, bailing out.");
				throw new PlatformNotSupportedException();
			}

			return fd;
		}
		static bool QueryDisplays(int fd, List<LinuxDisplay> displays)
		{
			unsafe
			{
				bool has_displays = false;
				if (displays != null)
				{
					displays.Clear();
				}

				ModeRes* resources = (ModeRes*)Drm.ModeGetResources(fd);
				if (resources == null)
				{
					Console.WriteLine("[KMS] Drm.ModeGetResources failed.");
					return false;
				}
				Console.WriteLine("[KMS] DRM found {0} connectors", resources->count_connectors);

				// Search for a valid connector
				ModeConnector* connector = null;
				for (int i = 0; i < resources->count_connectors; i++)
				{
					connector = (ModeConnector*)Drm.ModeGetConnector(fd, *(resources->connectors + i));
					if (connector != null)
					{
						bool success = false;
						LinuxDisplay display = null;
						try
						{
							if (connector->connection == ModeConnection.Connected && connector->count_modes > 0)
							{
								success = QueryDisplay(fd, connector, out display);
								has_displays |= success;
							}
						}
						catch (Exception e)
						{
							Console.WriteLine("[KMS] Failed to add display. Error: {0}", e);
						}

						if (success && displays != null)
						{
							displays.Add(display);
						}
						else
						{
							Drm.ModeFreeConnector((IntPtr)connector);
							connector = null;
						}
					}
				}

				return has_displays;
			}
		}
		unsafe static bool QueryDisplay(int fd, ModeConnector* c, out LinuxDisplay display)
		{
			display = null;

			// Find corresponding encoder
			ModeEncoder* encoder = GetEncoder(fd, c);
			if (encoder == null)
				return false;

			ModeCrtc* crtc = GetCrtc(fd, encoder);
			if (crtc == null)
				return false;

			display = new LinuxDisplay(fd, (IntPtr)c, (IntPtr)encoder, (IntPtr)crtc);
			return true;
		}
		unsafe static ModeEncoder* GetEncoder(int fd, ModeConnector* c)
		{
			ModeEncoder* encoder = null;
			for (int i = 0; i < c->count_encoders && encoder == null; i++)
			{
				ModeEncoder* e = (ModeEncoder*)Drm.ModeGetEncoder(fd, *(c->encoders + i));

				if (e == null)
					continue;
				
				if (e->encoder_id == c->encoder_id)
					encoder = e;					
				else
					Drm.ModeFreeEncoder((IntPtr)e);					
			}

			if (encoder != null)
				Console.WriteLine("[KMS] Encoder {0} found for connector {1}", encoder->encoder_id, c->connector_id);
			else
				Console.WriteLine("[KMS] Failed to find encoder for connector {0}", c->connector_id);

			return encoder;
		}

		unsafe static ModeCrtc* GetCrtc(int fd, ModeEncoder* encoder)
		{
			ModeCrtc* crtc = (ModeCrtc*)Drm.ModeGetCrtc(fd, encoder->crtc_id);
			if (crtc != null)
			{
				Console.WriteLine("[KMS] CRTC {0} found for encoder {1}",
					encoder->crtc_id, encoder->encoder_id);
			}
			else
			{
				Console.WriteLine("[KMS] Failed to find crtc {0} for encoder {1}",
					encoder->crtc_id, encoder->encoder_id);
			}
			return crtc;
		}

		unsafe static void GetModes(LinuxDisplay display, DisplayResolution[] modes, out DisplayResolution current)
		{
			int mode_count = display.pConnector->count_modes;
			Console.WriteLine("[KMS] Display supports {0} mode(s)", mode_count);
			for (int i = 0; i < mode_count; i++)
			{
				ModeInfo* mode = display.pConnector->modes + i;
				if (mode != null)
				{
					Console.WriteLine("Mode {0}: {1}x{2} @{3}", i,
						mode->hdisplay, mode->vdisplay, mode->vrefresh);
					DisplayResolution res = GetDisplayResolution(mode);
					modes[i] = res;
				}
			}

			if (display.pCrtc->mode_valid != 0)
			{
				ModeInfo cmode = display.pCrtc->mode;
				current = GetDisplayResolution(&cmode);
			}
			else
			{
				current = GetDisplayResolution(display.pConnector->modes);
			}
			Console.WriteLine("Current mode: {0}", current.ToString());
		}
		unsafe static DisplayResolution GetDisplayResolution(ModeInfo* mode)
		{
			return new DisplayResolution(
				0, 0,
				mode->hdisplay, mode->vdisplay,
				32, // This is actually part of the framebuffer, not the DisplayResolution
				mode->vrefresh);
		}

		unsafe static ModeInfo* GetModeInfo(LinuxDisplay display, DisplayResolution resolution)
		{
			for (int i = 0; i < display.pConnector->count_modes; i++)
			{
				ModeInfo* mode = display.pConnector->modes + i;
				if (mode != null &&
					mode->hdisplay == resolution.Width &&
					mode->vdisplay == resolution.Height)
				{
					return mode;
				}
			}
			return null;
		}

		static int SetupDisplay(string gpu, out IntPtr gbm_device, out IntPtr egl_display)
		{
			Console.WriteLine("[KMS] Attempting to use gpu '{0}'.", gpu);

			gbm_device = IntPtr.Zero;
			egl_display = IntPtr.Zero;

			int fd = Libc.open(gpu, OpenFlags.ReadWrite | OpenFlags.CloseOnExec);
			if (fd < 0)
			{
				Console.WriteLine("[KMS] Failed to open gpu");
				return fd;
			}
			Console.WriteLine("[KMS] GPU '{0}' opened as fd:{1}", gpu, fd);

			gbm_device = Gbm.CreateDevice(fd);
			if (gbm_device == IntPtr.Zero)
			{
				throw new NotSupportedException("[KMS] Failed to create GBM device");
			}
			Console.WriteLine("[KMS] GBM {0:x} created successfully; ", gbm_device);

			egl_display = Egl.GetDisplay(gbm_device);
			if (egl_display == IntPtr.Zero)
			{
				throw new NotSupportedException("[KMS] Failed to create EGL display");
			}
			Console.WriteLine("[KMS] EGL display {0:x} created successfully", egl_display);

			int major, minor;
			if (!Egl.Initialize(egl_display, out major, out minor))
			{
				ErrorCode error = Egl.GetError();
				throw new NotSupportedException("[KMS] Failed to initialize EGL display. Error code: " + error);
			}
			Console.WriteLine("[KMS] EGL {0}.{1} initialized successfully on display {2:x}", major, minor, egl_display);

			return fd;
		}
	}
}

