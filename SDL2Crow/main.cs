//
//  main.cs
//
//  Author:
//       Jean-Philippe Bruyère <jp_bruyere@hotmail.com>
//
//  Copyright (c) 2016 jp
//
//  This program is free software: you can redistribute it and/or modify
//  it under the terms of the GNU General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
//
//  This program is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU General Public License for more details.
//
//  You should have received a copy of the GNU General Public License
//  along with this program.  If not, see <http://www.gnu.org/licenses/>.
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SDL2;
using Cairo;

namespace SDL2Crow
{
	public class SDL2Window : IDisposable
	{
		int width, height;
		IntPtr win, rend;
		SDL.SDL_Surface sdlSurface;

		public SDL2Window (int _width = 800, int _height = 600) {
			width = _width;
			height = _height;

			SDL.SDL_Init (SDL.SDL_INIT_VIDEO);


			SDL.SDL_CreateWindowAndRenderer (width, height,
											SDL.SDL_WindowFlags.SDL_WINDOW_BORDERLESS, out win, out rend);

			IntPtr sdlSurfPtr = SDL.SDL_GetWindowSurface (win);
			sdlSurface = (SDL.SDL_Surface)Marshal.PtrToStructure (sdlSurfPtr, typeof (SDL.SDL_Surface));
			sdlSurface.
			//uint colorkey = SDL.SDL_MapRGB (sdlSurface.format, 0xFF, 0x00, 0xFF);
			//// Set all pixels of colour R(255), G(0), B(255) to be transparent
			//SDL.SDL_SetColorKey (sdlSurfPtr, 4, colorkey);

			//using (Cairo.Surface surf = new Cairo.ImageSurface (sdlSurface.pixels, Cairo.Format.ARGB32, sdlSurface.w, sdlSurface.h, sdlSurface.pitch)) {
			//	using (Context ctx = new Context (surf)) {
			//		ctx.Rectangle (0, 0, width, height);
			//		ctx.SetSourceRGBA (1, 0, 0, 0);
			//		ctx.Fill ();
			//	}
			//}

			//SDL.SDL_UpdateWindowSurface (win);

		}
		void Run () {
			SDL.SDL_Event e;

			while(true){
				SDL.SDL_PollEvent (out e);

				if (e.type == SDL.SDL_EventType.SDL_QUIT)
					break;
				
				SDL.SDL_SetRenderDrawBlendMode (rend, SDL.SDL_BlendMode.SDL_BLENDMODE_BLEND);
				SDL.SDL_SetRenderDrawColor (rend, 0xFF, 0x00, 0x00, 0x20);
				SDL.SDL_RenderClear (rend);

				SDL.SDL_RenderPresent (rend);
			}
		
		}

		[STAThread]
		static void Main ()
		{
			using (SDL2Window win = new SDL2Window ()) {
				win.Run ();
			}
		}

		public void Dispose ()
		{
			SDL.SDL_DestroyRenderer (rend);
			SDL.SDL_DestroyWindow (win);
			SDL.SDL_Quit ();
		}
	}
}

