using System;
using OpenTK.Graphics.OpenGL;
using OpenTK;

namespace Crow.GLBackend
{
	public class FontShader : Shader
	{
		protected int coef1Location, coef2Location, coef3Location, coefALocation, resolutionLoc;
		public float coef1 {
			set {GL.Uniform1 (coef1Location, value);}
		}
		public float coef2 {
			set {GL.Uniform1 (coef2Location, value);}
		}
		public float coef3 {
			set {GL.Uniform1 (coef3Location, value);}
		}
		public float coefA {
			set {GL.Uniform1 (coefALocation, value);}
		}
		public Vector2 Resolution {
			set { GL.Uniform2 (resolutionLoc, value); }
		}
		public FontShader ()
		{
			vertSource = @"
				#version 130

				precision highp float;

				uniform mat4 projection_matrix;
				uniform mat4 modelview_matrix;

				in vec2 in_position;
				in vec2 in_tex;
				out vec2 texCoord;


				void main(void)
				{
					texCoord = in_tex;
					gl_Position = projection_matrix * modelview_matrix * vec4(in_position,0, 1);
				}";

			fragSource = @"
				#version 130
				precision highp float;

				uniform vec4 color;
				uniform sampler2D tex;
				uniform sampler2D backTex;
				uniform sampler2D stencil;
				uniform vec2 resolution;
				uniform float coef1;
				uniform float coef2;
				uniform float coef3;
				uniform float coefA;


				in vec2 texCoord;
				out vec4 out_frag_color;

				void main(void)
				{
					vec2 uv		= gl_FragCoord.xy / resolution;
					vec2 pix	= 1.0 / resolution;

				    float	c		= texture( tex, texCoord).r;
					vec4	bc		= texture( backTex, uv);

					vec4 diff	= bc - color;

				    if (c==0.)
				        discard;					

//					if (c > 0.75)
//						out_frag_color = vec4(color.rgb,c);
//					else{
						float p = texture( tex, texCoord + vec2(-pix.x,0)).r;
						float n = texture( tex, texCoord + vec2( pix.x,0)).r;
						
						vec3 current = vec3(0,0,0);
					
						float a = n - c;
						current.r += coef1 * a * diff.r;
						current.g += coef2 * a * diff.g;
						current.b += coef3 * a * diff.b;
						//current.a *= coefA * a * diff.a;
					
						a = p - c;
						current.r += coef3 * a * diff.r;
						current.g += coef2 * a * diff.g;
						current.b += coef1 * a * diff.b;
						//current.a *= coefA * a * diff.a;
					
						out_frag_color = vec4(color.rgb+current,c*coefA);

//					}					
				}";

			Compile ();

		}
		protected override void GetUniformLocations ()
		{
			base.GetUniformLocations ();

			resolutionLoc = GL.GetUniformLocation (pgmId, "resolution");
			coef1Location = GL.GetUniformLocation (pgmId, "coef1");
			coef2Location = GL.GetUniformLocation (pgmId, "coef2");
			coef3Location = GL.GetUniformLocation (pgmId, "coef3");
			coefALocation = GL.GetUniformLocation (pgmId, "coefA");
		}
		protected override void BindVertexAttributes ()
		{
			base.BindVertexAttributes ();
			GL.BindAttribLocation(pgmId, 1, "in_tex");
		}
		protected override void BindSamplesSlots ()
		{
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"),0);
			GL.Uniform1(GL.GetUniformLocation (pgmId, "backTex"),1);
			//GL.Uniform1(GL.GetUniformLocation (pgmId, "stencil"),1);
		}
	}
}

