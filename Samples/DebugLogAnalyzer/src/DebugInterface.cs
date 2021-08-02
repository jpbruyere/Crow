// Copyright (c) 2013-2019  Bruyère Jean-Philippe <jp_bruyere@hotmail.com>
//
// This code is licensed under the MIT license (MIT) (http://opensource.org/licenses/MIT)

using System;
using Crow.Drawing;
using System.Threading;
using Samples;

namespace Crow
{
	public class DebugInterface : SampleBase {
		static DebugInterface() {
			DbgLogger.IncludeEvents = DbgEvtType.None;
			DbgLogger.DiscardEvents = DbgEvtType.None;
			DbgLogger.ConsoleOutput = false;
		}
		public DebugInterface (IntPtr hWin) : base (hWin)
		{
			SolidBackground = false;
			surf = new ImageSurface (Format.Argb32, 100, 100);
		}

		public override void Run()
		{
			Init();

			Thread t = new Thread (interfaceThread) {
				IsBackground = true
			};
			t.Start ();
		}
		public bool Terminate;
		string source;
		Action delRegisterForRepaint;//call RegisterForRepaint in the container widget (DebugInterfaceWidget)
		Action<Exception> delSetCurrentException;
		Func<object> delGetScreenCoordinate;

		void interfaceThread () {
			while (!Terminate) {
				try
				{
					Update();	
				}
				catch (System.Exception ex)
				{
					while (Monitor.IsEntered(LayoutMutex)) {
						Console.WriteLine ($"[DebugIFace] trying to exit LayoutMutex on error");
						Monitor.Exit (LayoutMutex);
					}
					while (Monitor.IsEntered(UpdateMutex)) {
						Console.WriteLine ($"[DebugIFace] trying to exit UpdateMutex on error");
						Monitor.Exit (UpdateMutex);
					}
					while (Monitor.IsEntered(ClippingMutex)) {
						Console.WriteLine ($"[DebugIFace] trying to exit ClippingMutex on error");
						Monitor.Exit (ClippingMutex);
					}


					/*while (Monitor.IsEntered(LayoutMutex))
						Monitor.Exit (LayoutMutex);
					while (Monitor.IsEntered(UpdateMutex))
						Monitor.Exit (UpdateMutex);
					while (Monitor.IsEntered(ClippingMutex))
						Monitor.Exit (ClippingMutex);*/
					delSetCurrentException (ex);
					Console.WriteLine ($"[DbgIFace] {ex}");
					ClearInterface();
					Thread.Sleep(1000);	
				}
				
				if (IsDirty) {
					delRegisterForRepaint();
					//Console.WriteLine ("[DbgIFace]RegisterForRepaint");
				}
					
				Thread.Sleep (UPDATE_INTERVAL);
			}
		}
		public IntPtr SurfacePointer {
			get {
				lock(UpdateMutex)
					return surf.Handle;
			}
		}
		public void RegisterDebugInterfaceCallback (object w){
			Type t = w.GetType();
			delRegisterForRepaint = (Action)Delegate.CreateDelegate(typeof(Action), w, t.GetMethod("RegisterForRepaint"));
			delSetCurrentException = (Action<Exception>)Delegate.CreateDelegate(typeof(Action<Exception>), w, t.GetProperty("CurrentException").GetSetMethod());
			delGetScreenCoordinate = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), w, t.GetMethod("GetScreenCoordinates"));
		}
		public void ResetDirtyState () {
			IsDirty = false;
		}
		public string Source {
			set {
				if (source == value)
					return;
				source = value;
				if (string.IsNullOrEmpty(source))
					return;
				delSetCurrentException(null);
				try
				{
					lock (UpdateMutex) {
						Widget tmp = CreateITorFromIMLFragment (source).CreateInstance();
						ClearInterface();
						AddWidget (tmp);
						tmp.DataSource = this;
					}					
				}
				catch (IML.InstantiatorException iTorEx)
				{
					delSetCurrentException(iTorEx.InnerException);
				}
				catch (System.Exception ex)
				{
					delSetCurrentException(ex);
				}
			}
		}
		public void Resize (int width, int height) {
			
			lock (UpdateMutex) {
				clientRectangle = new Rectangle (0, 0, width, height);
				surf?.Dispose();
				surf = new ImageSurface (Format.Argb32, width, height);				
				foreach (Widget g in GraphicTree)
					g.RegisterForLayouting (LayoutingType.All);
				RegisterClip (clientRectangle);
			}				
		}
		public override void ForceMousePosition()
		{
			Point p = Point.Parse(delGetScreenCoordinate().ToString());
			Glfw.Glfw3.SetCursorPosition (WindowHandle, p.X + MousePosition.X, p.Y + MousePosition.Y);
		}
	}
}
