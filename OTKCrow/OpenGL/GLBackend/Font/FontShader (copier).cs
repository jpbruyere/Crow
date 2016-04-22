using System;
using OpenTK.Graphics.OpenGL;

namespace Crow.GLBackend
{
	public class FontShader : Shader
	{
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
				uniform sampler2D stencil;
				uniform vec2 resolution;

				in vec2 texCoord;
				out vec4 out_frag_color;

				vec3
				energy_distribution( vec4 previous, vec4 current, vec4 next )
				{
					float primary = 1.0/3.0;
					float secondary = 1.0/3.0;
					float tertiary = 0.0;

					// Energy distribution as explained on:
					// http://www.grc.com/freeandclear.htm
					//
					// .. v..
					// RGB RGB RGB
					// previous.g + previous.b + current.r + current.g + current.b
					//
					// . .v. .
					// RGB RGB RGB
					// previous.b + current.r + current.g + current.b + next.r
					//
					// ..v ..
					// RGB RGB RGB
					// current.r + current.g + current.b + next.r + next.g

					float r =
					tertiary * previous.g +
					secondary * previous.b +
					primary * current.r +
					secondary * current.g +
					tertiary * current.b;

					float g =
					tertiary * previous.b +
					secondary * current.r +
					primary * current.g +
					secondary * current.b +
					tertiary * next.r;

					float b =
					tertiary * current.r +
					secondary * current.g +
					primary * current.b +
					secondary * next.r +
					tertiary * next.g;

					return vec3(r,g,b);
				}
				void main(void)
				{
				    float a = texture( tex, texCoord).r;

				    if (a==0.)
				        discard;

					float x = gl_FragCoord.x / resolution.x;
				    float s = mod(x,x);

					vec4 current = color * texture( tex, texCoord).r;
					vec4 previous = color * texture( tex, texCoord + vec2(-x,0)).r;
					vec4 next = color * texture( tex, texCoord + vec2(x,0)).r;

					float r = current.r;
					float g = current.g;
					float b = current.b;

					if( s <= 0.333 )
					{
						float z = s/0.333;
						r = mix(current.r, previous.b, z);
						g = mix(current.g, current.r, z);
						b = mix(current.b, current.g, z);
					}
					else if( s <= 0.666 )
					{
						float z = (s-0.33)/0.333;
						r = mix(previous.b, previous.g, z);
						g = mix(current.r, previous.b, z);
						b = mix(current.g, current.r, z);
					}
					else if( s < 1.0 )
					{
						float z = (s-0.66)/0.334;
						r = mix(previous.g, previous.r, z);
						g = mix(previous.b, previous.g, z);
						b = mix(current.r, previous.b, z);
					}


					out_frag_color = vec4(energy_distribution(previous, current, next),1);					
				}";

			Compile ();

		}

		protected override void BindVertexAttributes ()
		{
			base.BindVertexAttributes ();
			GL.BindAttribLocation(pgmId, 1, "in_tex");
		}
		protected override void BindSamplesSlots ()
		{
			GL.Uniform1(GL.GetUniformLocation (pgmId, "tex"),0);
			//GL.Uniform1(GL.GetUniformLocation (pgmId, "stencil"),1);
		}
	}
}

