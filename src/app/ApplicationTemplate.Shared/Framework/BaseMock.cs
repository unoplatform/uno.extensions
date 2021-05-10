using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using GeneratedSerializers;

namespace ApplicationTemplate
{
	public class BaseMock
	{
		private readonly IObjectSerializer _serializer;

		public BaseMock(IObjectSerializer serializer)
		{
			_serializer = serializer ?? throw new ArgumentNullException(nameof(serializer));
		}

		/// <summary>
		/// Gets the deserialized value of the specified embedded resource.
		/// </summary>
		/// <typeparam name="T">Type of value</typeparam>
		/// <param name="resourceName">Name of the resource</param>
		/// <param name="serializer">Object serializer</param>
		/// <param name="callerMemberName">Caller member name</param>
		/// <returns>Deserialized value</returns>
		/// <remarks>
		/// If left empty, the <paramref name="resourceName" /> will implicitly be treated as "{callerTypeName}.{callerMemberName}.json".
		/// If <paramref name="serializer" /> is left empty, the serializer defined in ctor. will be used".
		/// Note that this will deserialize the first embedded resource whose name ends with the specified <paramref name="resourceName" />.
		/// </remarks>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Globalization", "CA1307:Specify StringComparison", Justification = "Not available for Desktop")]
		protected T GetFromEmbeddedResource<T>(
			string resourceName = null,
			IObjectSerializer serializer = null,
			[CallerMemberName] string callerMemberName = null)
		{
			var assembly = GetType().GetTypeInfo().Assembly;

			var desiredResourceName = resourceName != null
				? resourceName.Replace("/", ".")
				: $"{GetType().Name}.{callerMemberName}.json";

			var actualResourceName = assembly
				.GetManifestResourceNames()
				.FirstOrDefault(name => name.EndsWith(desiredResourceName, StringComparison.OrdinalIgnoreCase));

			if (actualResourceName == null)
			{
				throw new FileNotFoundException($"Couldn't find an embedded resource ending with '{desiredResourceName}'.", desiredResourceName);
			}

			using (var stream = assembly.GetManifestResourceStream(actualResourceName))
			{
				return (T)(serializer ?? _serializer).FromStream(stream, typeof(T));
			}
		}

		/// <summary>
		/// Creates a task that's completed successfully with the deserialized value of the specified embedded resource.
		/// </summary>
		/// <remarks>
		/// If left empty, the <paramref name="resourceName" /> will implicitly be treated as "{callerTypeName}.{callerMemberName}.json".
		/// If <paramref name="serializer" /> is left empty, the serializer defined in ctor. will be used".
		/// Note that this will deserialize the first embedded resource whose name ends with the specified <paramref name="resourceName" />.
		/// </remarks>
		/// <typeparam name="T">Type of object</typeparam>
		/// <param name="resourceName">Name of the resource</param>
		/// <param name="serializer">Deserializer</param>
		/// <param name="callerMemberName">Name of the caller (used if no resource name provided)</param>
		/// <returns>Deserialized object</returns>
		protected Task<T> GetTaskFromEmbeddedResource<T>(
			string resourceName = null,
			IObjectSerializer serializer = null,
			[CallerMemberName] string callerMemberName = null
		) => Task.FromResult(GetFromEmbeddedResource<T>(resourceName, serializer, callerMemberName));
	}
}
