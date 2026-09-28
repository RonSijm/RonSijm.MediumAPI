using System.Reflection;
using System.Runtime.CompilerServices;
using Microsoft.AspNetCore.Routing;

namespace RonSijm.MediumAPI;

/// <summary>
/// Holds the endpoint-mapping actions that the compile-time source generator registers for every project that
/// declares <see cref="IEndpoint"/> implementations. Each project's generator emits a module initializer
/// that calls <see cref="Register"/> as soon as its assembly is loaded, so a single call to
/// <see cref="MediumApiEndpointExtensions.MapMediumApiEndpoints"/> anywhere in the application maps every endpoint
/// from every referenced project - no per-project registration call, no manual wiring, no assembly scanning
/// configuration to maintain.
/// </summary>
public static class EndpointRegistry
{
	private static readonly List<Action<IEndpointRouteBuilder>> mappers = [];
	private static readonly object gate = new();

	/// <summary>
	/// Called by generated module initializers. Not intended to be called directly from application code.
	/// </summary>
	public static void Register(Action<IEndpointRouteBuilder> mapEndpoints)
	{
		lock (gate)
		{
			mappers.Add(mapEndpoints);
		}
	}

	/// <summary>
	/// The endpoint-mapping actions registered so far. Exposed mainly for diagnostics - most applications
	/// only ever need <see cref="MediumApiEndpointExtensions.MapMediumApiEndpoints"/>.
	/// </summary>
	public static IReadOnlyList<Action<IEndpointRouteBuilder>> Mappers
	{
		get
		{
			lock (gate)
			{
				return mappers.ToArray();
			}
		}
	}

	/// <summary>
	/// Module initializers only run once the CLR actually executes a module's global static constructor - and
	/// merely loading an assembly via reflection does <b>not</b> trigger that on its own. A referenced project
	/// that nothing in the host ever touches directly (which describes almost every <c>*.ASP</c> project - the
	/// host only calls <see cref="MediumApiEndpointExtensions.MapMediumApiEndpoints"/>, it never references an
	/// endpoint class by name) isn't even in the host assembly's metadata reference table, so walking references
	/// wouldn't find it either. Instead, load every assembly sitting next to the entry assembly and explicitly
	/// run its module constructor, giving every module initializer (if any) a chance to self-register.
	/// </summary>
	internal static void EnsureReferencedAssembliesAreLoaded()
	{
		var baseDirectory = AppContext.BaseDirectory;
		if (string.IsNullOrEmpty(baseDirectory) || !Directory.Exists(baseDirectory))
		{
			return;
		}

		var loadedNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
		foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
		{
			if (assembly.GetName().Name is { } name)
			{
				loadedNames.Add(name);
			}

			RunModuleConstructor(assembly);
		}

		foreach (var dllPath in Directory.EnumerateFiles(baseDirectory, "*.dll", SearchOption.TopDirectoryOnly))
		{
			string assemblyName;
			try
			{
				assemblyName = AssemblyName.GetAssemblyName(dllPath).Name ?? Path.GetFileNameWithoutExtension(dllPath);
			}
			catch
			{
				// Not a managed assembly (native dependency, etc.) - nothing for us to load.
				continue;
			}

			if (!loadedNames.Add(assemblyName))
			{
				continue;
			}

			try
			{
				RunModuleConstructor(Assembly.LoadFrom(dllPath));
			}
			catch
			{
				// Assemblies that can't load here (wrong architecture, missing dependencies, etc.) can't
				// contain endpoint adapters we could have mapped anyway.
			}
		}
	}

	private static void RunModuleConstructor(Assembly assembly)
	{
		try
		{
			RuntimeHelpers.RunModuleConstructor(assembly.ManifestModule.ModuleHandle);
		}
		catch
		{
			// Dynamic modules and a handful of framework assemblies don't support this - nothing we can (or
			// need to) do about it.
		}
	}
}
