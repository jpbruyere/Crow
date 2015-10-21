using System;
using OpenTK.Graphics.OpenGL;

namespace go.GLBackend
{
	public class ClipShader : Shader
	{
		public ClipShader ()
		{
			vertSource = @"
				#version 130

				precision highp float;

				uniform mat4 projection_matrix;
				uniform mat4 modelview_matrix;

				in vec2 in_position;

				void main(void)
				{
					gl_Position = projection_matrix * modelview_matrix * vec4(in_position,0, 1);
				}";

			fragSource = @"
				#version 130
				precision highp float;

				out vec4 out_frag_color;

				void main(void)
				{
					out_frag_color = vec4(1.0,1.0,1.0,1.0);
				}";

			Compile ();
		}
	}
}

