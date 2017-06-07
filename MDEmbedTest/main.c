#include <mono/jit/jit.h>
#include <mono/metadata/environment.h>
#include <stdlib.h>
#include <GL/gl.h>
#include <GL/glext.h>
#include "../../libcrow/libcrow.h"

/*
 * Very simple mono embedding example.
 * Compile with: 
 * 	gcc -o teste teste.c `pkg-config --cflags --libs mono-2` -lm
 * 	mcs test.cs
 * Run with:
 * 	./teste test.exe
 */

static MonoString*
gimme () {
	return mono_string_new (mono_domain_get (), "All your monos are belong to us!");
}

static void main_function (MonoDomain *domain, const char *file, int argc, char** argv)
{
	MonoAssembly *assembly;
	MonoAssembly *a2;

	assembly = mono_domain_assembly_open (domain, file);
	if (!assembly)
		exit (2);
	//a2 = mono_domain_assembly_open (domain, "../build/Debug/testDrm.exe");
	//if (!a2)
	//	exit (2);
	/*
	 * mono_jit_exec() will run the Main() method in the assembly.
	 * The return value needs to be looked up from
	 * System.Environment.ExitCode.
	 */
	mono_jit_exec (domain, assembly, argc, argv);
	//mono_jit_exec (domain, a2, argc, argv);
}

int 
main(int argc, char* argv[]) {
	MonoDomain *domain;
	const char *file;
	int retval;
	
	if (argc < 2){
		fprintf (stderr, "Please provide an assembly to load\n");
		return 1;
	}
	file = argv [1];

	/*
	 * Load the default Mono configuration file, this is needed
	 * if you are planning on using the dllmaps defined on the
	 * system configuration
	 */
	mono_config_parse (NULL);
	/*
	 * mono_jit_init() creates a domain: each assembly is
	 * loaded and run in a MonoDomain.
	 */
	domain = mono_jit_init (file);
	/*
	 * We add our special internal call, so that C# code
	 * can call us back.
	 */
	mono_add_internal_call ("Crow.Native.LibCrow::gimme", gimme);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_context_create", crow_context_create);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_context_destroy", crow_context_destroy);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_context_set_root", crow_context_set_root);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_context_process_clipping", crow_context_process_clipping);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_context_process_layouting", crow_context_process_layouting);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_context_process_drawing", crow_context_process_drawing);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_context_resize", crow_context_resize);

	mono_add_internal_call ("Crow.Native.LibCrow::crow_object_create", crow_object_create);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_object_destroy", crow_object_destroy);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_object_set_type", crow_object_set_type);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_object_do_layout", crow_object_do_layout);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_object_register_layouting", crow_object_register_layouting);

	mono_add_internal_call ("Crow.Native.LibCrow::crow_object_child_add", crow_object_child_add);
	mono_add_internal_call ("Crow.Native.LibCrow::crow_object_child_remove", crow_object_child_remove);

	main_function (domain, file, argc - 1, argv + 1);
	
	retval = mono_environment_exitcode_get ();
	
	mono_jit_cleanup (domain);
	return retval;
}


