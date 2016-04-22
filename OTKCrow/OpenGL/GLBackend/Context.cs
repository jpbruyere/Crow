#define	MPERF
//#define GPU_TIME_MSR

using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Drawing.Imaging;



namespace Crow.GLBackend
{
	public class Context : IDisposable
	{
		#region CTOR
		public Context (Surface _surf)
		{
			surf = _surf;

			saveGLConfig ();

			createStencilTexture ();

			fboId = createFbo (surf.texId);
				
			projectionMatrix = Matrix4.CreateOrthographicOffCenter 
				(0, surf.width, surf.height, 0, 0, 1);
			modelviewMatrix = Matrix4.Identity;

			if (shader == null)
				shader = new Shader ();
			if (texturedShader == null)
				texturedShader = new TexturedShader ();
			if (clipShader == null)
				clipShader = new ClipShader ();
			if (fontShader == null)
				fontShader = new FontShader ();


			GL.Viewport (0, 0, surf.width, surf.height);
			GL.PrimitiveRestartIndex (int.MaxValue);
			GL.Enable (EnableCap.PrimitiveRestart);
			GL.Enable (EnableCap.Blend);
			GL.BlendFunc (BlendingFactorSrc.SrcAlpha, BlendingFactorDest.OneMinusSrcAlpha);

			//GL.BindFramebuffer(FramebufferTarget.Framebuffer, fboId);

			//			GL.ClearColor(0.0f, 0.0f, 0.0f, 0.0f);
			//			GL.Clear(ClearBufferMask.ColorBufferBit);
		}
		#endregion

		#if (DEBUG && MPERF)
		Stopwatch sw_fill = new Stopwatch ();
		Stopwatch sw_stroke = new Stopwatch ();
		#endif

		#if GPU_TIME_MSR
		ulong gpuTime = 0;
		ulong gpuTimeCpt = 0;
		#endif

		public float coef1 = 1.0f;
		public float coef2 = 0.5f;
		public float coef3 = 0.0f;
		public float coefA = 1.0f;

		static Shader shader;
		static Shader clipShader;
		static Shader texturedShader;
		static FontShader fontShader;

		static List<glFont> fontsCache = new List<glFont>();

		#region Private Fields
		Surface surf;
		Surface sourceSurface;//should be a pattern object from which surf would be derrived

		double lineWidth = 1.0;
		Color color = Color.White;
		LineCap lineCap = LineCap.Butt;
		LineJoin lineJoint = LineJoin.Miter;

		FontFace fontFace;
		uint fontSize = 10;
		//path building temp fields
		Vector2 curPos = new Vector2(0f,0f);
		List<Path> pathes = new List<Path> ();
		Path cpath;//current path

		int fboId;
		int stencilTexId;
		VertexArrayObject vao;

		Matrix4 projectionMatrix, 
				modelviewMatrix;

		bool stencilTest = false;

		VertexArrayObject paint_vao; //quad used to paint source on context

		//path limits used in fill
		float	minX,maxX,minY,maxY,
				polyWidth, polyHeight;

		#endregion

		#region Public properties
		public double LineWidth {
			get { return lineWidth; }
			set { lineWidth = value; }
		}
		public Color Color {
			get { return color; }
			set { color = value; }
		}
		public LineCap LineCap {
			get { return lineCap;}
			set {lineCap = value;}
		}
		public LineJoin LineJoint {
			get {return lineJoint;}
			set {lineJoint = value;}
		}
		#endregion

		#region Private functions
		int createFbo(int texID)
		{
			//GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			int fbo;
			GL.GenFramebuffers(1,out fbo);
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, fbo);
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, 
				FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D,texID,0);

			//GL.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
			FramebufferErrorCode fbErr = GL.CheckFramebufferStatus (FramebufferTarget.Framebuffer);

			switch (fbErr) {
			case FramebufferErrorCode.FramebufferUndefined:
				break;
			case FramebufferErrorCode.FramebufferComplete:
				//Debug.WriteLine ("FBO complete");
				break;
			case FramebufferErrorCode.FramebufferIncompleteAttachment:
				Debug.WriteLine ("FBO incomplete attachment");
				break;
			case FramebufferErrorCode.FramebufferIncompleteMissingAttachment:
				Debug.WriteLine ("FBO missing attachment");
				break;
			default:
				Debug.WriteLine ("FBO error");
				break;
			}
			return fbo;
		}
		public int copyCurentSurfaceToTexture()
		{
			//create a new fbo to blit to with an empty texture
			Surface tmp = surf.CreateSimilar ();
			int dstFbo = createFbo (tmp.texId);//we are binded to new fbo (read&write)
			//bind read to Context fbo;
			GL.BindFramebuffer (FramebufferTarget.ReadFramebuffer, fboId);
			GL.BindFramebuffer (FramebufferTarget.DrawFramebuffer, dstFbo);

			GL.BlitFramebuffer (0, 0, surf.width, surf.height, 0, 0, surf.width, surf.height,
				ClearBufferMask.ColorBufferBit,BlitFramebufferFilter.Nearest);

			//bind write fbo to Context
			GL.BindFramebuffer (FramebufferTarget.Framebuffer, fboId);
			//delete temp fbo
			GL.DeleteFramebuffer (dstFbo);
			return tmp.texId;
		}
		void createStencilTexture(){
			stencilTexId = GL.GenTexture();
			GL.BindTexture(TextureTarget.Texture2D, stencilTexId);
			GL.TexImage2D(TextureTarget.Texture2D,0,
				PixelInternalFormat.R8, surf.width, surf.height,0,
				OpenTK.Graphics.OpenGL.PixelFormat.Red, PixelType.UnsignedByte,IntPtr.Zero);

			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
			GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
			GL.BindTexture(TextureTarget.Texture2D, 0);
		}
		void clear_path()
		{
			pathes.Clear ();
			curPos = new Vector2 (0f, 0f);
			cpath = null;
		}
		void addPath(Path _path){
			if (_path == null)
				return;
			if (_path.Count > 1)
				pathes.Add (_path);
		}
		//TODO: 360° will fail
		double NormalizeAngle(double a)
		{
			double res = a % (2*Math.PI);
			return res < 0 ? res + 2*Math.PI : res;
		}
		#endregion

		#region Public functions
		#region Main drawing functions
		public void Stroke()
		{
			stroke_internal ();
			clear_path ();
		}
		public void StrokePreserve()
		{
			stroke_internal ();
		}
		public void Fill()
		{
			fill_internal ();
			clear_path ();
		}
		public void FillPreserve()
		{
			fill_internal ();
		}
		public void Paint()
		{
			if (paint_vao == null)
				return;
			texturedShader.Enable ();
			//shader.LineWidth = lineWidth;
			texturedShader.ProjectionMatrix = projectionMatrix;
			texturedShader.ModelViewMatrix = modelviewMatrix;
			texturedShader.Color = new Vector4(1f,1f,1f,1f);

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, sourceSurface.texId);

			paint_vao.Render (PrimitiveType.TriangleStrip);

			GL.BindTexture (TextureTarget.Texture2D, 0);

			texturedShader.Disable ();
		}
		public void SetSourceSurface (Surface _source, int x, int y)
		{
			sourceSurface = _source;

			if (paint_vao != null)
				paint_vao.Dispose ();


			paint_vao = new QuadVAO (x,y,sourceSurface.width,sourceSurface.height);
		}
		#region Clipping
		public void Clip()
		{
			clip_internal ();
			clear_path ();
		}
		public void ClipPreserve()
		{
			clip_internal ();
			clear_path ();
		}
		public void ResetClip()
		{
			stencilTest = false;

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, 
				FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D,stencilTexId,0);


			GL.Clear (ClearBufferMask.ColorBufferBit);

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, 
				FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D,surf.texId,0);
		}
		#endregion

		#region Fonts & text handling
		public void SelectFontFace(string _fontName, FontSlant _slant, FontWeight _weight){
			Font tmp = _fontName;
			switch (_slant) {
			case FontSlant.Italic:
				tmp.Style |= FontStyle.Italic;
				break;
			case FontSlant.Oblique:
				tmp.Style |= FontStyle.Oblique;
				break;
			}
			if (_weight == FontWeight.Bold)
				tmp.Style |= FontStyle.Bold;

			fontFace = FontFace.SearchFont (tmp);
		}
		public void SetFontSize(uint _fontSize){
			fontSize = _fontSize;
			if (!fontFace.glyphesCache.ContainsKey(fontSize))
				fontFace.buildGlyphesTextures (fontSize, 0, 0xFF);
		}
		public FontExtents FontExtents {
			get {
				return fontFace.originalFontExtents;
			}
		}
		public TextExtents TextExtents(string s)
		{
			TextExtents te = new Crow.GLBackend.TextExtents ();

			int penX = 0,
				penY = 0;

			for (int i = 0; i < s.Length; i++) {
				uint c = (uint)s [i];
				glGlyph g = fontFace.glyphesCache [fontSize] [c];
				penX += (int)g.advanceX;
				penY += (int)g.advanceY;
			}

			te.XAdvance = penX;
			te.YAdvance = penY;
			return te;
		}
		public void FillText(string text, Point _pt)
		{
			buildTextVAO (text,_pt);

			int backTex = copyCurentSurfaceToTexture ();

			fontShader.Enable ();
			//shader.LineWidth = lineWidth;
			fontShader.ProjectionMatrix = projectionMatrix;
			fontShader.ModelViewMatrix = modelviewMatrix;
			fontShader.Color = new Vector4(
				(float)color.R,
				(float)color.G,
				(float)color.B,
				(float)color.A);
			fontShader.Resolution = new Vector2 (surf.width, surf.height);
			fontShader.coef1 = coef1;
			fontShader.coef2 = coef2;
			fontShader.coef3 = coef3;
			fontShader.coefA = coefA;
//			shader.StencilTest = stencilTest;
//
//			if (stencilTest) {
//					shader.Resolution = new Vector2 (surf.width, surf.height);
//				GL.ActiveTexture (TextureUnit.Texture0);
//				GL.BindTexture (TextureTarget.Texture2D, stencilTexId);
//			}

			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, curFontTex);
			GL.ActiveTexture (TextureUnit.Texture1);
			GL.BindTexture (TextureTarget.Texture2D, backTex);

			vao.Render (PrimitiveType.TriangleStrip);
			vao.Dispose ();

			//			#if DEBUG
			//			ErrorCode err = GL.GetError ();
			//			if (err != ErrorCode.NoError)
			//				Debug.WriteLine ("Stroke_internal error: " + err.ToString ());
			//			#endif

			fontShader.Disable ();

			GL.BindTexture (TextureTarget.Texture2D, 0);
			GL.ActiveTexture (TextureUnit.Texture0);
			GL.BindTexture (TextureTarget.Texture2D, 0);

			//destroy background texture
			GL.DeleteTexture (backTex);
			//GL.Enable (EnableCap.Blend);
		}
		#endregion

		#endregion



		#region Path handling functions
		public void MoveTo(double x, double y)
		{
			addPath (cpath);
			cpath = new Path ();
			curPos = new Vector2 ((float)x, (float)y);
		}
		public void LineTo(double x, double y)
		{
			if (cpath==null)
				cpath = new Path ();
			if (cpath.Count == 0)
				cpath.Add (curPos);
			curPos = new Vector2 ((float)x, (float)y);
			cpath.Add (curPos);
		}
		public void Rectangle(Rectangle r)
		{
			MoveTo (r.X, r.Y);
			LineTo (r.Right, r.Y);
			LineTo (r.Right, r.Bottom);
			LineTo (r.X, r.Bottom);
			ClosePath ();
		}
		public void Arc(double xc, double yc, double radius, double angle1, double angle2)
		{
			angle1 = NormalizeAngle (angle1);
			angle2 = NormalizeAngle (angle2);

			if (angle1 > angle2)
				angle2 += Math.PI * 2.0;
			double 	a = angle1,
			//step = Math.PI/16;
			step =Math.PI * 2/(radius+20);

			Vector2 v = new Vector2 ((float)(Math.Cos (a) * radius + xc), (float)(Math.Sin (a) * radius + yc));
			if (v!=curPos)
				cpath.Add (v);
			int cpt = 0;
			while(true){
				if (a<angle2){
					a+=step;
					if (a > angle2)
						break;
						//a = angle2;
				}else
					break;
				cpt++;
//				if (a == angle2) {//add a point very close to a to get good termination angle
//					v = new Vector2 ((float)(Math.Cos (a-0.001) * radius + xc), (float)(Math.Sin (a-0.001) * radius + yc));
//					cpath.Add (v);
//				}

				v = new Vector2 ((float)(Math.Cos (a) * radius + xc), (float)(Math.Sin (a) * radius + yc));
				cpath.Add (v);

			}
			curPos = cpath [cpath.Count - 1];
		}
		public void ClosePath()
		{
			if (cpath == null)
				return;
			if (cpath.Count < 3)//cannot close path with less than 3 points
				return;
				
			cpath.IsClosed = true;

			MoveTo (cpath [0].X, cpath [0].Y);
		}
		#endregion

		#region Transformations
		public void Translate(double tx, double ty)
		{
			modelviewMatrix *= Matrix4.CreateTranslation ((float)tx, (float)ty, 0f);
		}
		public void Scale(double sx, double sy)
		{
			modelviewMatrix *= Matrix4.CreateScale ((float)sx, (float)sy, 0f);
		}
		public void Rotate(double angle){
			modelviewMatrix *= Matrix4.CreateRotationZ ((float)angle);
		}
		#endregion
		#endregion

		//temp Lists used to build arrays for vao
		List<Vector2> vertices = new List<Vector2> ();
		List<int> indices = new List<int> ();
		List<Vector2> texCoords = new List<Vector2> ();
		int idxOffset = 0;	//offset in vertices array between pathes


		void initVAOsCache()
		{
			if (vertices != null)
				return;
			vertices = new List<Vector2> ();
			indices = new List<int> ();
			texCoords = new List<Vector2> ();
			idxOffset = 0;
		}
		void flush(){
			vao = new VertexArrayObject (
				vertices.ToArray (),
				texCoords.ToArray (), 
				indices.ToArray ());

			vertices = null;
			indices = null;
			texCoords = null;
		}

		void stroke_internal()
		{
			buildStrokeVAOs ();

			#if GPU_TIME_MSR
			ulong gpuTimeStart, gpuTimeStop;
			gpuTimeStart = (ulong)GL.GetInteger64 (GetPName.Timestamp);
			#endif

			shader.Enable ();
			//shader.LineWidth = lineWidth;
			shader.ProjectionMatrix = projectionMatrix;
			shader.ModelViewMatrix = modelviewMatrix;
			shader.Color = new Vector4(
				(float)color.R,
				(float)color.G,
				(float)color.B,
				(float)color.A);
			shader.StencilTest = stencilTest;
				
			if (stencilTest) {
				shader.Resolution = new Vector2 (surf.width, surf.height);
				GL.ActiveTexture (TextureUnit.Texture0);
				GL.BindTexture (TextureTarget.Texture2D, stencilTexId);
			}

			vao.Render (PrimitiveType.TriangleStrip);
			vao.Dispose ();

//			#if DEBUG
//			ErrorCode err = GL.GetError ();
//			if (err != ErrorCode.NoError)
//				Debug.WriteLine ("Stroke_internal error: " + err.ToString ());
//			#endif

			shader.Disable ();

			GL.BindTexture (TextureTarget.Texture2D, 0);

			#if GPU_TIME_MSR
			gpuTimeStop = (ulong)GL.GetInteger64 (GetPName.Timestamp);
			Debug.WriteLine(gpuTimeStop - gpuTimeStart);
			#endif
		}
		void fill_internal()
		{
			buildFillVAOs ();

			shader.Enable ();
			//shader.LineWidth = lineWidth;
			shader.ProjectionMatrix = projectionMatrix;
			shader.ModelViewMatrix = modelviewMatrix;
			shader.Color = new Vector4(
				(float)color.R,
				(float)color.G,
				(float)color.B,
				(float)color.A);
			shader.StencilTest = stencilTest;

			if (stencilTest) {
				shader.Resolution = new Vector2 (surf.width, surf.height);
				GL.ActiveTexture (TextureUnit.Texture0);
				GL.BindTexture (TextureTarget.Texture2D, stencilTexId);
			}

			vao.Render (PrimitiveType.Triangles);
			vao.Dispose ();

			shader.Disable ();

			GL.BindTexture (TextureTarget.Texture2D, 0);
		}
		void clip_internal()
		{
			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, 
				FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D,stencilTexId,0);

			if (!stencilTest)
				GL.Clear (ClearBufferMask.ColorBufferBit);
			stencilTest = true;

			buildFillVAOs ();

			clipShader.Enable ();
			//shader.LineWidth = lineWidth;
			clipShader.ProjectionMatrix = projectionMatrix;
			clipShader.ModelViewMatrix = modelviewMatrix;

			vao.Render (PrimitiveType.Triangles);
			vao.Dispose ();

			clipShader.Disable ();

			GL.FramebufferTexture2D(FramebufferTarget.Framebuffer, 
				FramebufferAttachment.ColorAttachment0,
				TextureTarget.Texture2D,surf.texId,0);
		}

		void buildFillVAOs()
		{
			#if (DEBUG && MPERF)
			sw_fill.Start();
			#endif

			addPath (cpath);

			initVAOsCache ();

			float hlw = (float)lineWidth / 2.0f;//half line width

			Vector2 vDir,vDir2;
			bool genTexCoords = false;

			for (int j = 0; j < pathes.Count; j++) {
				Path path = new Path(pathes[j]);
				//keep an array containing real vertex indice
				List<int> vertIdx = new List<int> (Enumerable.Range(0,path.Count));

				#region get polygon bounds for texture mapping
				if (genTexCoords){
					minX = float.MaxValue;
					maxX = float.MinValue;
					minY = float.MaxValue;
					maxY = float.MinValue;

					foreach (Vector2 p in path) {
						if (p.X < minX)
							minX = p.X;
						if (p.Y < minY)
							minY = p.Y;
						if (p.X > maxX)
							maxX = p.X;
						if (p.Y > maxY)
							maxY = p.Y;
					}
					polyWidth = maxX - minX;
					polyHeight = maxY - minY;
				}
				#endregion

				vertices.AddRange (path);//we only play on indices
				int ptrS = 0, ptrS1, ptrS2; //index du sommet dans le path
				while (path.Count > 3) {
					if (ptrS == path.Count)
						ptrS = 0;

					if (ptrS < path.Count - 2) {
						ptrS1 = ptrS + 1;
						ptrS2 = ptrS + 2;
					} else if (ptrS < path.Count - 1) {
						ptrS1 = ptrS + 1;
						ptrS2 = 0;
					} else {
						ptrS1 = 0;
						ptrS2 = 1;
					}

					//should test only concavity points if any
					vDir = Vector2.NormalizeFast (path [ptrS1] - path [ptrS]);
					vDir2 = Vector2.NormalizeFast (path [ptrS2] - path [ptrS]);

					float dotP = Vector2.Dot (vDir.PerpendicularLeft, vDir2);
					//double angle = Math.Acos((double)dotP);
//
//					Debug.WriteLine (dotP);
//					Debug.WriteLine (angle);

					if (dotP <= 0) {
						//triangle is invalid
						ptrS++;
						continue;
					}
						
					//check if no other points of path is inside tri
//					bool triangleIsValid = true;
//					int i = ptrS + 3;
//					if (i >= path.Count)
//						i -= path.Count;
//					while (i != ptrS) {
//						if (PointInTriangle (path [i],path [ptrS] , path [ptrS1], path [ptrS2])) {
//							ptrS++;
//							triangleIsValid = false;
//							break;
//						}
//						i++;
//						if (i == path.Count)
//							i = 0;
//					}
//					if (!triangleIsValid)
//						continue;
					//triangle is valid, add tri to primitive to draw
					//and remove middle sommet from path[];
					addFillTriangle (j, vertIdx [ptrS], vertIdx [ptrS1], vertIdx [ptrS2]);

					path.RemoveAt (ptrS1);
					vertIdx.RemoveAt (ptrS1);
				}
				if (path.Count == 3)
					addFillTriangle (j, vertIdx [0], vertIdx [1], vertIdx [2]);

				if (j < pathes.Count - 1) {				
					idxOffset += pathes[j].Count;
				}
			}
			flush ();

			#if (DEBUG && MPERF)
			sw_fill.Stop();
			#endif
		}

		int curFontTex;
		void buildTextVAO(string text, Point pt)
		{
			initVAOsCache ();

			int penX = pt.X,
			penY = pt.Y + (int)fontFace.originalFontExtents.Ascent;

			for (int i = 0; i < text.Length; i++) {
				uint c = (uint)text [i];
				glGlyph g = fontFace.glyphesCache [fontSize] [c];
				curFontTex = g.texId;
				vertices.Add (new Vector2(penX + g.bmpLeft, penY - g.bmpTop));
				vertices.Add (new Vector2(penX + g.bmpLeft, penY - g.bmpTop + g.dims.Height));
				vertices.Add (new Vector2(penX + g.bmpLeft + g.dims.Width, penY - g.bmpTop));
				vertices.Add (new Vector2(penX + g.bmpLeft + g.dims.Width, penY - g.bmpTop + g.dims.Height));
				indices.Add (int.MaxValue);//primitive restart
				indices.Add (i*4);
				indices.Add (i*4 + 1);
				indices.Add (i*4 + 2);
				indices.Add (i*4 + 3);
				texCoords.Add (new Vector2(g.texX,g.texY));
				texCoords.Add (new Vector2(g.texX,g.texY + g.texHeight));
				texCoords.Add (new Vector2(g.texX + g.texWidth,g.texY));
				texCoords.Add (new Vector2(g.texX + g.texWidth,g.texY + g.texHeight));

				penX += (int)g.advanceX;
				penY += (int)g.advanceY;
			}

			flush ();
		}
		void buildStrokeVAOs()
		{
			addPath (cpath);

			initVAOsCache ();

			float hlw = (float)lineWidth / 2.0f;//half line width

			Vector2 vDir,vDir2, vPerp;
			float lPerp;
			int i;
			//current offset in index
			for (int j = 0; j < pathes.Count; j++) {
				Path path = pathes[j];	
				for (i = 0; i < path.Count; i++) {
					lPerp = hlw;
					vDir2 = Vector2.Zero;

					if (i < path.Count - 1) {
						vDir = Vector2.Normalize (path [i + 1] - path [i]);
						if (i > 0) {
							vDir2 = Vector2.Normalize (path [i] - path [i - 1]);
						} else if (path.IsClosed)
							vDir2 = Vector2.Normalize (path [0] - path [path.Count - 1]);
					} else {
						vDir = Vector2.Normalize (path [i] - path [i - 1]);
						if (path.IsClosed)
							vDir2 = Vector2.Normalize (path [0]-path [i]);
					}

					vPerp = Vector2.Normalize(vDir+vDir2).PerpendicularLeft;

					if (vDir2 != Vector2.Zero) {
						double dotP = Vector2.Dot (vDir.PerpendicularLeft, vPerp);
						double angle = Math.Acos(dotP);
						double x = Math.Tan (angle) * hlw;

						if (dotP == 0)
							lPerp = hlw;
						else
							lPerp = (float)Math.Sqrt (Math.Pow (x, 2) + Math.Pow (hlw, 2));
					}

					vertices.Add (path [i] + vPerp * (float)lPerp);
					vertices.Add (path [i] - vPerp * (float)lPerp);
					indices.Add (idxOffset + i*2);
					indices.Add (idxOffset + i*2 + 1);
					texCoords.Add (Vector2.Zero);
					texCoords.Add (Vector2.One);
				}
				if (path.IsClosed) {
					indices.Add (idxOffset);
					indices.Add (idxOffset+1);
					texCoords.Add (Vector2.Zero);
					texCoords.Add (Vector2.One);
				}
				if (j < pathes.Count - 1) {
					indices.Add (int.MaxValue);	//restart primitive idex
					idxOffset += path.Count * 2;
				}
			}
			flush ();
		}
			
		void addFillTriangle(int pathIdx, int vx1, int vx2, int vx3){
			addFillVertex (pathIdx, vx1);
			addFillVertex (pathIdx, vx2);
			addFillVertex (pathIdx, vx3);
		}

		void addFillVertex(int pathIdx, int vx){
			Vector2 p = pathes [pathIdx] [vx];
			indices.Add (idxOffset + vx);
			//texCoords.Add (new Vector2(1.0f/polyWidth*(p.X-minX),1.0f/polyHeight*(p.Y-minY)));
		}

		float sign (Vector2 p1, Vector2 p2, Vector2 p3)
		{
			return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
		}
		bool PointInTriangle (Vector2 pt, Vector2 v1, Vector2 v2, Vector2 v3)
		{
			bool b1, b2, b3;

			b1 = sign(pt, v1, v2) < 0.0f;
			b2 = sign(pt, v2, v3) < 0.0f;
			b3 = sign(pt, v3, v1) < 0.0f;

			return ((b1 == b2) && (b2 == b3));
		}

		#region GL Ctx Save and restore
		int savedFbo,
		savedPriRstIdx,
		savedBlendSrc,
		savedBlendDst;
		int[] viewport = new int[4];
		float[] savedClsColor = new float[4];
		bool savedBlend,savedPriRstCap;
		void saveGLConfig(){
			GL.GetInteger (GetPName.FramebufferBinding, out savedFbo);
			GL.GetInteger ((GetPName)All.PrimitiveRestartIndex, out savedPriRstIdx);
			GL.GetBoolean ((GetPName)All.PrimitiveRestart, out savedPriRstCap);
			GL.GetInteger(GetPName.Viewport, viewport);
			GL.GetFloat (GetPName.ColorClearValue, savedClsColor);
			GL.GetBoolean (GetPName.Blend, out savedBlend);
			GL.GetInteger (GetPName.BlendSrc, out savedBlendSrc);
			GL.GetInteger (GetPName.BlendDst, out savedBlendDst);
		}
		void restoreGLConfig(){
			GL.BindFramebuffer(FramebufferTarget.Framebuffer, savedFbo);

			GL.Viewport (viewport[0],viewport[1],viewport[2],viewport[3]);
			GL.PrimitiveRestartIndex (savedPriRstIdx);
			GL.ClearColor (savedClsColor [0], savedClsColor [1], savedClsColor [2], savedClsColor [3]);
			if (!savedBlend)
				GL.Disable (EnableCap.Blend);
			if (!savedPriRstCap)
				GL.Disable (EnableCap.PrimitiveRestart);
			GL.BlendFunc ((BlendingFactorSrc) savedBlendSrc,(BlendingFactorDest)savedBlendDst);
		}
		#endregion

		#region IDisposable implementation

		public void Dispose ()
		{
			restoreGLConfig ();

			if (GL.IsFramebuffer (fboId))
				GL.DeleteFramebuffer (fboId);
			if (GL.IsTexture(stencilTexId))
				GL.DeleteTexture(stencilTexId);
		}

		#endregion

	}
}

