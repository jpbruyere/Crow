using System;
using System.Runtime.InteropServices;

namespace XLib
{
    public class Display : IDisposable
    {
        internal IntPtr handle;
        internal Int32 screen;
        IntPtr lastEvent;

        public Display()
        {
            handle = NativeMethods.XOpenDisplay(IntPtr.Zero);
            if (handle == IntPtr.Zero)
                throw new NotSupportedException("[XLib] Failed to open display.");

            screen = NativeMethods.XDefaultScreen(handle);
            lastEvent = Marshal.AllocHGlobal(96);
        }

        /*public IntPtr NextEvent {
            get {                
                NativeMethods.XNextEvent(handle, lastEvent);
                return lastEvent;
            }
        }*/

		#region IDisposable Support
		private bool disposedValue = false; // To detect redundant calls

        protected virtual void Dispose(bool disposing)
        {
            if (!disposedValue)
            {
                if (disposing)
                {
                    // TODO: dispose managed state (managed objects).
                }

                //Marshal.FreeHGlobal (lastEvent);
                NativeMethods.XCloseDisplay (handle);

                disposedValue = true;
            }
        }

        // TODO: override a finalizer only if Dispose(bool disposing) above has code to free unmanaged resources.
        ~Display() {
           // Do not change this code. Put cleanup code in Dispose(bool disposing) above.
            Dispose(false);
        }

        // This code added to correctly implement the disposable pattern.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        #endregion


    }
}
