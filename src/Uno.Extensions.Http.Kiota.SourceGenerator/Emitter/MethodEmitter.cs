#nullable disable

using System;
using System.Collections.Generic;
using System.Text;
using Uno.Extensions.Http.Kiota.SourceGenerator.CodeDom;
using Uno.Extensions.Http.Kiota.SourceGenerator.Configuration;
using Uno.Extensions.Http.Kiota.SourceGenerator.Refinement;

namespace Uno.Extensions.Http.Kiota.SourceGenerator.Emitter;

/// <summary>
/// Emits C# method declarations from the CodeDOM tree for request-builder
/// classes. Handles four distinct method shapes:
/// <list type="bullet">
///   <item><b>Indexer accessor</b> — a C# indexer (<c>this[string]</c>)
///   that creates a child item request builder with a path parameter
///   added to the URL template parameters dictionary.</item>
///   <item><b>HTTP executor</b> — async methods (<c>GetAsync</c>,
///   <c>PostAsync</c>, etc.) that build request info, set up error
///   mappings, and call <c>RequestAdapter.SendAsync</c>.</item>
///   <item><b>Request-info builder</b> — methods
///   (<c>ToGetRequestInformation</c>, etc.) that construct a
///   <c>RequestInformation</c> with the HTTP method, URL template,
///   path parameters, accept headers, and optional request body.</item>
///   <item><b>WithUrl</b> — a method that constructs a new request builder
///   instance from a raw URL string, bypassing URL template expansion.</item>
/// </list>
/// <para>
/// Called from <see cref="CSharpEmitter"/> delegation points for each
/// method in the canonical member ordering.
/// </para>
/// </summary>
internal sealed class MethodEmitter
{
	private readonly KiotaGeneratorConfig _config;

	/// <summary>
	/// Initializes a new <see cref="MethodEmitter"/> with the given
	/// generator configuration.
	/// </summary>
	/// <param name="config">
	/// The generator configuration controlling client name, namespace,
	/// and other options.
	/// </param>
	public MethodEmitter(KiotaGeneratorConfig config)
	{
		_config = config;
	}

	// ==================================================================
	// Indexer emission
	// ==================================================================

	/// <summary>
	/// Emits a C# indexer accessor for a parameterized path segment.
	/// <para>
	/// Pattern:
	/// <code>
	/// public global::Ns.ItemRequestBuilder this[string position]
	/// {
	///     get
	///     {
	///         var urlTplParams = new Dictionary&lt;string, object&gt;(PathParameters);
	///         urlTplParams.Add("{segment}", position);
	///         return new global::Ns.ItemRequestBuilder(urlTplParams, RequestAdapter);
	///     }
	/// }
	/// </code>
	/// </para>
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="indexer">The indexer definition from the CodeDOM.</param>
	/// <param name="cls">The owning class (used for context).</param>
	public void EmitIndexer(CodeWriter writer, CodeIndexer indexer, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (indexer is null)
		{
			throw new ArgumentNullException(nameof(indexer));
		}

		var returnTypeString = CSharpConventionService.GetTypeString(indexer.ReturnType);
		var paramType = GetIndexerParameterType(indexer);
		var paramName = "position";

		// XML doc summary (single-line, using the indexer's description if set).
		var summaryText = !string.IsNullOrEmpty(indexer.Description)
			? indexer.Description
			: "Gets an item from the collection";
		CSharpEmitter.WriteXmlDocSummarySingleLine(writer, summaryText);

		// XML doc param (use OpenAPI description if available, else default).
		var paramDesc = !string.IsNullOrEmpty(indexer.ParameterDescription)
			? indexer.ParameterDescription
			: "Unique identifier of the item";
		writer.WriteLine(
			"/// <param name=\"" + paramName + "\">"
			+ paramDesc
			+ "</param>");

		// XML doc returns.
		writer.WriteLine(
			"/// <returns>A <see cref=\""
			+ CSharpConventionService.GetTypeReference(indexer.ReturnType)
			+ "\"/></returns>");

		// Indexer declaration.
		writer.WriteLine(
			"public "
			+ returnTypeString
			+ " this["
			+ paramType + " " + paramName
			+ "]");
		writer.OpenBlock();

		// Getter.
		writer.WriteLine("get");
		writer.OpenBlock();

		writer.WriteLine(
			"var urlTplParams = new Dictionary<string, object>(PathParameters);");

		var pathSegment = indexer.PathSegment ?? indexer.IndexParameterName ?? "id";
		writer.WriteLine(
			"urlTplParams.Add(\""
			+ EscapeStringLiteral(pathSegment)
			+ "\", " + paramName + ");");

		writer.WriteLine(
			"return new "
			+ returnTypeString
			+ "(urlTplParams, RequestAdapter);");

		writer.CloseBlock(); // get
		writer.CloseBlock(); // indexer
	}

	// ==================================================================
	// HTTP executor method emission
	// ==================================================================

	/// <summary>
	/// Emits an HTTP executor method (<c>GetAsync</c>, <c>PostAsync</c>,
	/// etc.) with nullable conditional compilation guards, error mappings,
	/// and <c>RequestAdapter.SendAsync</c> dispatch.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="method">The executor method from the CodeDOM.</param>
	/// <param name="cls">The owning class (used for context).</param>
	public void EmitExecutorMethod(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (method is null)
		{
			throw new ArgumentNullException(nameof(method));
		}

		var returnType = method.ReturnType;
		var returnTypeString = returnType != null
			? CSharpConventionService.GetTypeString(returnType)
			: null;
		var returnTypeRef = returnType != null
			? CSharpConventionService.GetTypeReference(returnType)
			: null;
		var hasReturnType = returnType != null && !string.Equals(returnType.Name, "void", StringComparison.OrdinalIgnoreCase);
		var hasBody = HasParameterOfKind(method, CodeParameterKind.Body);
		var bodyParam = hasBody ? FindParameterOfKind(method, CodeParameterKind.Body) : null;
		var configParam = FindParameterOfKind(method, CodeParameterKind.RequestConfiguration);
		var requestGeneratorName = CSharpConventionService.GetRequestGeneratorMethodName(method.HttpMethod);

		// Determine query parameters type for request configuration.
		var queryParamsType = GetQueryParametersType(cls, method);

		// XML doc.
		WriteExecutorXmlDoc(writer, method, returnTypeRef, hasReturnType, returnType);

		// Nullable conditional compilation — two method signatures.
		// Always needed because requestConfiguration parameter is a reference type.
		var needsNullableGuard = true;
		if (needsNullableGuard)
		{
			// #if NETSTANDARD2_1_OR_GREATER ... nullable enabled signature.
			writer.WriteLineRaw("#if " + CSharpConventionService.NullableEnableCondition);
			writer.WriteLineRaw("#nullable enable");

			WriteExecutorSignature(writer, method, returnTypeString, hasReturnType, hasBody, bodyParam, queryParamsType, nullable: true);
			writer.OpenBlock();

			writer.WriteLineRaw("#nullable restore");
			writer.WriteLineRaw("#else");

			// Restore indent to the same level as the first signature.
			writer.DecreaseIndent();
			WriteExecutorSignature(writer, method, returnTypeString, hasReturnType, hasBody, bodyParam, queryParamsType, nullable: false);
			writer.OpenBlock();

			writer.WriteLineRaw("#endif");
		}
		else
		{
			WriteExecutorSignature(writer, method, returnTypeString, hasReturnType, hasBody, bodyParam, queryParamsType, nullable: false);
			writer.OpenBlock();
		}

		// Method body.
		EmitExecutorBody(writer, method, hasReturnType, returnTypeRef, returnType, hasBody, bodyParam, requestGeneratorName);

		writer.CloseBlock();
	}

	// ==================================================================
	// Request generator method emission
	// ==================================================================

	/// <summary>
	/// Emits a request-information builder method
	/// (<c>ToGetRequestInformation</c>, <c>ToPostRequestInformation</c>, etc.)
	/// that constructs a <c>RequestInformation</c> instance.
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="method">The request generator method from the CodeDOM.</param>
	/// <param name="cls">The owning class (used for context).</param>
	public void EmitRequestGeneratorMethod(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (method is null)
		{
			throw new ArgumentNullException(nameof(method));
		}

		var hasBody = HasParameterOfKind(method, CodeParameterKind.Body);
		var bodyParam = hasBody ? FindParameterOfKind(method, CodeParameterKind.Body) : null;
		var queryParamsType = GetQueryParametersType(cls, method);

		// XML doc.
		if (!string.IsNullOrEmpty(method.Description))
		{
			CSharpEmitter.WriteXmlDocSummary(writer, method.Description);
		}

		writer.WriteLine("/// <returns>A <see cref=\"RequestInformation\"/></returns>");

		if (hasBody && bodyParam != null)
		{
			var bodyDescription = !string.IsNullOrEmpty(bodyParam.Description)
				? bodyParam.Description
				: "The request body";
			writer.WriteLine(
				"/// <param name=\"" + bodyParam.Name + "\">"
				+ bodyDescription + "</param>");
		}

		writer.WriteLine(
			"/// <param name=\"requestConfiguration\">"
			+ "Configuration for the request such as headers, query parameters, and middleware options."
			+ "</param>");

		// Nullable conditional compilation — two signatures.
		var needsNullableGuard = true; // requestConfiguration is always a reference type
		if (needsNullableGuard)
		{
			writer.WriteLineRaw("#if " + CSharpConventionService.NullableEnableCondition);
			writer.WriteLineRaw("#nullable enable");

			WriteRequestGeneratorSignature(writer, method, hasBody, bodyParam, queryParamsType, nullable: true);
			writer.OpenBlock();

			writer.WriteLineRaw("#nullable restore");
			writer.WriteLineRaw("#else");

			writer.DecreaseIndent();
			WriteRequestGeneratorSignature(writer, method, hasBody, bodyParam, queryParamsType, nullable: false);
			writer.OpenBlock();

			writer.WriteLineRaw("#endif");
		}

		// Method body.
		EmitRequestGeneratorBody(writer, method, hasBody, bodyParam);

		writer.CloseBlock();
	}

	// ==================================================================
	// WithUrl method emission
	// ==================================================================

	/// <summary>
	/// Emits a <c>WithUrl</c> method that creates a new request builder from
	/// a raw URL string.
	/// <para>
	/// Pattern:
	/// <code>
	/// public global::Ns.XxxRequestBuilder WithUrl(string rawUrl)
	/// {
	///     return new global::Ns.XxxRequestBuilder(rawUrl, RequestAdapter);
	/// }
	/// </code>
	/// </para>
	/// </summary>
	/// <param name="writer">The code writer to emit into.</param>
	/// <param name="method">The WithUrl method from the CodeDOM.</param>
	/// <param name="cls">The owning class.</param>
	public void EmitWithUrlMethod(CodeWriter writer, CodeMethod method, CodeClass cls)
	{
		if (writer is null)
		{
			throw new ArgumentNullException(nameof(writer));
		}

		if (cls is null)
		{
			throw new ArgumentNullException(nameof(cls));
		}

		var classRef = CSharpConventionService.GetGloballyQualifiedName(cls);

		// XML doc.
		writer.WriteLine("/// <summary>");
		writer.WriteLine(
			"/// Returns a request builder with the provided arbitrary URL. "
			+ "Using this method means any other path or query parameters are ignored.");
		writer.WriteLine("/// </summary>");
		writer.WriteLine(
			"/// <returns>A <see cref=\"" + classRef + "\"/></returns>");
		writer.WriteLine(
			"/// <param name=\"rawUrl\">"
			+ "The raw URL to use for the request builder."
			+ "</param>");

		// Method declaration.
		writer.WriteLine(
			CSharpConventionService.GetAccessModifier(AccessModifier.Public)
			+ " "
			+ classRef
			+ " WithUrl(string rawUrl)");
		writer.OpenBlock();

		writer.WriteLine(
			"return new " + classRef + "(rawUrl, RequestAdapter);");

		writer.CloseBlock();
	}

	// ==================================================================
	// Private helpers — executor method
	// ==================================================================

	/// <summary>
	/// Writes XML doc comments for an executor method.
	/// </summary>
	private static void WriteExecutorXmlDoc(
		CodeWriter writer,
		CodeMethod method,
		string returnTypeRef,
		bool hasReturnType,
		CodeTypeBase returnType)
	{
		// Summary.
		if (!string.IsNullOrEmpty(method.Description))
		{
			CSharpEmitter.WriteXmlDocSummary(writer, method.Description);
		}

		// Returns.
		if (hasReturnType && returnTypeRef != null)
		{
			if (returnType != null && returnType.IsCollection)
			{
				// Collection returns: A List&lt;type&gt;
				writer.WriteLine(
					"/// <returns>A List&lt;" + returnTypeRef + "&gt;</returns>");
			}
			else
			{
				writer.WriteLine(
					"/// <returns>A <see cref=\"" + returnTypeRef + "\"/></returns>");
			}
		}

		// Body param.
		var bodyParam = FindParameterOfKind(method, CodeParameterKind.Body);
		if (bodyParam != null)
		{
			var bodyDescription = !string.IsNullOrEmpty(bodyParam.Description)
				? bodyParam.Description
				: "The request body";
			writer.WriteLine(
				"/// <param name=\"" + bodyParam.Name + "\">"
				+ bodyDescription + "</param>");
		}

		// cancellationToken param.
		writer.WriteLine(
			"/// <param name=\"cancellationToken\">"
			+ "Cancellation token to use when cancelling requests"
			+ "</param>");

		// requestConfiguration param.
		writer.WriteLine(
			"/// <param name=\"requestConfiguration\">"
			+ "Configuration for the request such as headers, query parameters, and middleware options."
			+ "</param>");

		// Exception docs for error mappings.
		// Combine status codes that map to the same error type.
		var errorGroups = new Dictionary<string, List<string>>();
		var errorGroupOrder = new List<string>();
		foreach (var mapping in method.ErrorMappings)
		{
			var errorTypeRef = CSharpConventionService.GetTypeReference(mapping.Value);
			if (!errorGroups.TryGetValue(errorTypeRef, out var codes))
			{
				codes = new List<string>();
				errorGroups[errorTypeRef] = codes;
				errorGroupOrder.Add(errorTypeRef);
			}

			codes.Add(mapping.Key);
		}

		foreach (var errorTypeRef in errorGroupOrder)
		{
			var codes = errorGroups[errorTypeRef];
			var codeString = string.Join(" or ", codes);
			writer.WriteLine(
				"/// <exception cref=\"" + errorTypeRef + "\">"
				+ "When receiving a " + codeString + " status code"
				+ "</exception>");
		}
	}

	/// <summary>
	/// Writes the executor method signature.
	/// </summary>
	private static void WriteExecutorSignature(
		CodeWriter writer,
		CodeMethod method,
		string returnTypeString,
		bool hasReturnType,
		bool hasBody,
		CodeParameter bodyParam,
		string queryParamsType,
		bool nullable)
	{
		var sb = new StringBuilder();
		sb.Append(CSharpConventionService.GetAccessModifier(method.Access));
		sb.Append(" async ");

		// Return type: Task<T?> or Task<T> or Task.
		if (hasReturnType)
		{
			if (nullable)
			{
				sb.Append("Task<");
				sb.Append(returnTypeString);
				sb.Append("?> ");
			}
			else
			{
				sb.Append("Task<");
				sb.Append(returnTypeString);
				sb.Append("> ");
			}
		}
		else
		{
			sb.Append("Task ");
		}

		sb.Append(method.Name);
		sb.Append("(");

		// Build parameter list.
		var paramParts = new List<string>();

		// Body parameter (if present).
		if (hasBody && bodyParam != null)
		{
			paramParts.Add(
				CSharpConventionService.GetTypeString(bodyParam.Type)
				+ " " + bodyParam.Name);
		}

		// Request configuration parameter.
		var configType = "Action<RequestConfiguration<" + queryParamsType + ">>";
		if (nullable)
		{
			configType += "?";
		}

		paramParts.Add(configType + " requestConfiguration = default");

		// Cancellation token parameter.
		paramParts.Add("CancellationToken cancellationToken = default");

		sb.Append(string.Join(", ", paramParts));
		sb.Append(")");

		writer.WriteLine(sb.ToString());
	}

	/// <summary>
	/// Emits the body of an executor method.
	/// </summary>
	private static void EmitExecutorBody(
		CodeWriter writer,
		CodeMethod method,
		bool hasReturnType,
		string returnTypeRef,
		CodeTypeBase returnType,
		bool hasBody,
		CodeParameter bodyParam,
		string requestGeneratorName)
	{
		// Body parameter null guard.
		if (hasBody && bodyParam != null)
		{
			writer.WriteLine("_ = " + bodyParam.Name + " ?? throw new ArgumentNullException(nameof(" + bodyParam.Name + "));");
		}

		// Build the ToXxxRequestInformation call arguments.
		var infoCallArgs = new StringBuilder();
		if (hasBody && bodyParam != null)
		{
			infoCallArgs.Append(bodyParam.Name);
			infoCallArgs.Append(", ");
		}

		infoCallArgs.Append("requestConfiguration");

		writer.WriteLine(
			"var requestInfo = " + requestGeneratorName
			+ "(" + infoCallArgs.ToString() + ");");

		// Error mappings.
		if (method.ErrorMappings.Count > 0)
		{
			writer.WriteLine(
				"var errorMapping = new Dictionary<string, ParsableFactory<IParsable>>");
			writer.OpenBlock();

			// Combine 4XX and 5XX into "XXX" when they map to the same type.
			var combinedMappings = CombineErrorMappings(method.ErrorMappings);
			foreach (var entry in combinedMappings)
			{
				writer.WriteLine(
					"{ \"" + entry.Key + "\", "
					+ entry.Value + ".CreateFromDiscriminatorValue },");
			}

			// Close the dictionary initializer with }; (not just }).
			writer.DecreaseIndent();
			writer.WriteLine("};");
		}

		// Dispatch to RequestAdapter.
		if (hasReturnType)
		{
			if (returnType.IsCollection)
			{
				// Collection returns use a two-step pattern:
				// var collectionResult = await ...SendCollectionAsync<T>(...);
				// return collectionResult?.AsList();
				var sendCall = BuildSendCall(returnType, returnTypeRef, method.ErrorMappings.Count > 0);
				writer.WriteLine(sendCall);
				writer.WriteLine("return collectionResult?.AsList();");
			}
			else
			{
				var sendCall = BuildSendCall(returnType, returnTypeRef, method.ErrorMappings.Count > 0);
				writer.WriteLine(sendCall);
			}
		}
		else
		{
			var sendCall = BuildSendNoContentCall(method.ErrorMappings.Count > 0);
			writer.WriteLine(sendCall);
		}
	}

	/// <summary>
	/// Builds the <c>return await RequestAdapter.SendAsync</c> or
	/// <c>SendPrimitiveAsync</c> call string.
	/// </summary>
	private static string BuildSendCall(
		CodeTypeBase returnType,
		string returnTypeRef,
		bool hasErrorMappings)
	{
		var errorMappingArg = hasErrorMappings ? "errorMapping" : "default";
		var sb = new StringBuilder();

		// Collection types use a temp variable; non-collection returns directly.
		if (returnType.IsCollection)
		{
			sb.Append("var collectionResult = await RequestAdapter.");
		}
		else
		{
			sb.Append("return await RequestAdapter.");
		}

		// Determine which Send method to use based on return type.
		if (IsModelType(returnType))
		{
			// Object type → SendAsync<T>.
			if (returnType.IsCollection)
			{
				sb.Append("SendCollectionAsync<");
				sb.Append(returnTypeRef);
				sb.Append(">(requestInfo, ");
				sb.Append(returnTypeRef);
				sb.Append(".CreateFromDiscriminatorValue, ");
				sb.Append(errorMappingArg);
				sb.Append(", cancellationToken).ConfigureAwait(false);");
			}
			else
			{
				sb.Append("SendAsync<");
				sb.Append(returnTypeRef);
				sb.Append(">(requestInfo, ");
				sb.Append(returnTypeRef);
				sb.Append(".CreateFromDiscriminatorValue, ");
				sb.Append(errorMappingArg);
				sb.Append(", cancellationToken).ConfigureAwait(false);");
			}
		}
		else if (IsEnumType(returnType))
		{
			// Enum type → SendPrimitiveAsync<T?>.
			if (returnType.IsCollection)
			{
				sb.Append("SendCollectionAsync<");
				sb.Append(returnTypeRef);
				sb.Append("?>(requestInfo, ");
				sb.Append(errorMappingArg);
				sb.Append(", cancellationToken).ConfigureAwait(false);");
			}
			else
			{
				sb.Append("SendPrimitiveAsync<");
				sb.Append(returnTypeRef);
				sb.Append("?>(requestInfo, ");
				sb.Append(errorMappingArg);
				sb.Append(", cancellationToken).ConfigureAwait(false);");
			}
		}
		else if (string.Equals(returnType.Name, "Stream", StringComparison.Ordinal))
		{
			// Stream → SendPrimitiveAsync<Stream>.
			sb.Append("SendPrimitiveAsync<Stream>(requestInfo, ");
			sb.Append(errorMappingArg);
			sb.Append(", cancellationToken).ConfigureAwait(false);");
		}
		else if (CSharpConventionService.IsPrimitiveType(returnType.Name))
		{
			// Primitive types → SendPrimitiveAsync<T?>.
			if (returnType.IsCollection)
			{
				sb.Append("SendPrimitiveCollectionAsync<");
				sb.Append(returnTypeRef);
				sb.Append(">(requestInfo, ");
				sb.Append(errorMappingArg);
				sb.Append(", cancellationToken).ConfigureAwait(false);");
			}
			else
			{
				sb.Append("SendPrimitiveAsync<");
				sb.Append(returnTypeRef);
				sb.Append("?>(requestInfo, ");
				sb.Append(errorMappingArg);
				sb.Append(", cancellationToken).ConfigureAwait(false);");
			}
		}
		else
		{
			// Fallback → SendAsync<T>.
			sb.Append("SendAsync<");
			sb.Append(returnTypeRef);
			sb.Append(">(requestInfo, ");
			sb.Append(returnTypeRef);
			sb.Append(".CreateFromDiscriminatorValue, ");
			sb.Append(errorMappingArg);
			sb.Append(", cancellationToken).ConfigureAwait(false);");
		}

		return sb.ToString();
	}

	/// <summary>
	/// Builds the <c>await RequestAdapter.SendNoContentAsync</c> call for
	/// void-returning executor methods.
	/// </summary>
	private static string BuildSendNoContentCall(bool hasErrorMappings)
	{
		var errorMappingArg = hasErrorMappings ? "errorMapping" : "default";
		return "await RequestAdapter.SendNoContentAsync(requestInfo, "
			   + errorMappingArg
			   + ", cancellationToken).ConfigureAwait(false);";
	}

	/// <summary>
	/// Combines error mappings by type reference. When both "4XX" and "5XX"
	/// map to the same type, they are merged into a single "XXX" entry.
	/// </summary>
	private static List<KeyValuePair<string, string>> CombineErrorMappings(
		IReadOnlyDictionary<string, CodeType> errorMappings)
	{
		// Group status codes by their resolved type reference.
		var groups = new Dictionary<string, List<string>>();
		var groupOrder = new List<string>();

		foreach (var mapping in errorMappings)
		{
			var typeRef = CSharpConventionService.GetTypeReference(mapping.Value);
			if (!groups.TryGetValue(typeRef, out var codes))
			{
				codes = new List<string>();
				groups[typeRef] = codes;
				groupOrder.Add(typeRef);
			}

			codes.Add(mapping.Key);
		}

		var result = new List<KeyValuePair<string, string>>();
		foreach (var typeRef in groupOrder)
		{
			var codes = groups[typeRef];
			bool has4XX = false;
			bool has5XX = false;
			foreach (var code in codes)
			{
				if (code == "4XX") has4XX = true;
				if (code == "5XX") has5XX = true;
			}

			if (has4XX && has5XX)
			{
				result.Add(new KeyValuePair<string, string>("XXX", typeRef));
			}
			else
			{
				foreach (var code in codes)
				{
					result.Add(new KeyValuePair<string, string>(code, typeRef));
				}
			}
		}

		return result;
	}

	// ==================================================================
	// Private helpers — request generator method
	// ==================================================================

	/// <summary>
	/// Writes the request generator method signature.
	/// </summary>
	private static void WriteRequestGeneratorSignature(
		CodeWriter writer,
		CodeMethod method,
		bool hasBody,
		CodeParameter bodyParam,
		string queryParamsType,
		bool nullable)
	{
		var sb = new StringBuilder();
		sb.Append(CSharpConventionService.GetAccessModifier(method.Access));
		sb.Append(" RequestInformation ");
		sb.Append(method.Name);
		sb.Append("(");

		var paramParts = new List<string>();

		// Body parameter (if present).
		if (hasBody && bodyParam != null)
		{
			paramParts.Add(
				CSharpConventionService.GetTypeString(bodyParam.Type)
				+ " " + bodyParam.Name);
		}

		// Request configuration parameter.
		var configType = "Action<RequestConfiguration<" + queryParamsType + ">>";
		if (nullable)
		{
			configType += "?";
		}

		paramParts.Add(configType + " requestConfiguration = default");

		sb.Append(string.Join(", ", paramParts));
		sb.Append(")");

		writer.WriteLine(sb.ToString());
	}

	/// <summary>
	/// Emits the body of a request generator method.
	/// </summary>
	private static void EmitRequestGeneratorBody(
		CodeWriter writer,
		CodeMethod method,
		bool hasBody,
		CodeParameter bodyParam)
	{
		// Body parameter null guard.
		if (hasBody && bodyParam != null)
		{
			writer.WriteLine("_ = " + bodyParam.Name + " ?? throw new ArgumentNullException(nameof(" + bodyParam.Name + "));");
		}

		var httpMethodConstant = CSharpConventionService.GetHttpMethodConstant(method.HttpMethod);

		// Create RequestInformation.
		writer.WriteLine(
			"var requestInfo = new RequestInformation("
			+ httpMethodConstant
			+ ", UrlTemplate, PathParameters);");

		// Apply request configuration.
		writer.WriteLine("requestInfo.Configure(requestConfiguration);");

		// Accept headers.
		if (method.AcceptedResponseTypes.Count > 0)
		{
			var acceptHeader = string.Join(", ", method.AcceptedResponseTypes);
			writer.WriteLine(
				"requestInfo.Headers.TryAdd(\"Accept\", \""
				+ EscapeStringLiteral(acceptHeader) + "\");");
		}

		// Set request body content.
		if (hasBody && bodyParam != null)
		{
			var contentType = method.RequestBodyContentType ?? "application/json";
			var bodyType = bodyParam.Type;

			if (IsModelType(bodyType))
			{
				if (bodyType.IsCollection)
				{
					writer.WriteLine(
						"requestInfo.SetContentFromParsableCollection(RequestAdapter, \""
						+ EscapeStringLiteral(contentType) + "\", "
						+ bodyParam.Name + ");");
				}
				else
				{
					writer.WriteLine(
						"requestInfo.SetContentFromParsable(RequestAdapter, \""
						+ EscapeStringLiteral(contentType) + "\", "
						+ bodyParam.Name + ");");
				}
			}
			else if (CSharpConventionService.IsPrimitiveType(bodyType.Name))
			{
				writer.WriteLine(
					"requestInfo.SetContentFromScalar(RequestAdapter, \""
					+ EscapeStringLiteral(contentType) + "\", "
					+ bodyParam.Name + ");");
			}
			else
			{
				// Fallback: treat as parsable.
				writer.WriteLine(
					"requestInfo.SetContentFromParsable(RequestAdapter, \""
					+ EscapeStringLiteral(contentType) + "\", "
					+ bodyParam.Name + ");");
			}
		}

		writer.WriteLine("return requestInfo;");
	}

	// ==================================================================
	// Utility helpers
	// ==================================================================

	/// <summary>
	/// Returns the C# type string for the index parameter. Defaults to
	/// <c>string</c> when no explicit type is provided.
	/// </summary>
	private static string GetIndexerParameterType(CodeIndexer indexer)
	{
		if (indexer.IndexParameterType != null)
		{
			return CSharpConventionService.GetTypeString(indexer.IndexParameterType, includeNullable: false);
		}

		return "string";
	}

	/// <summary>
	/// Finds the query parameters type name for a request configuration.
	/// Falls back to <c>DefaultQueryParameters</c> when no inner
	/// <see cref="CodeClassKind.QueryParameters"/> class is found.
	/// </summary>
	private static string GetQueryParametersType(CodeClass cls, CodeMethod method)
	{
		// Look for an inner QueryParameters class associated with this HTTP method.
		for (int i = 0; i < cls.InnerClasses.Count; i++)
		{
			var inner = cls.InnerClasses[i];
			if (inner.Kind == CodeClassKind.QueryParameters)
			{
				// Convention: inner class name contains the HTTP method name.
				// e.g., "GetQueryParameters" for GET.
				var methodName = method.HttpMethod != null
					? CSharpConventionService.ToPascalCase(method.HttpMethod.ToLowerInvariant())
					: null;

				if (methodName != null && inner.Name.Contains(methodName))
				{
					return CSharpConventionService.GetGloballyQualifiedName(inner);
				}
			}
		}

		return "DefaultQueryParameters";
	}

	/// <summary>
	/// Returns <see langword="true"/> if the method has a parameter of the
	/// specified kind.
	/// </summary>
	private static bool HasParameterOfKind(CodeMethod method, CodeParameterKind kind)
	{
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			if (method.Parameters[i].Kind == kind)
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Finds the first parameter of the specified kind, or returns
	/// <see langword="null"/>.
	/// </summary>
	private static CodeParameter FindParameterOfKind(CodeMethod method, CodeParameterKind kind)
	{
		for (int i = 0; i < method.Parameters.Count; i++)
		{
			if (method.Parameters[i].Kind == kind)
			{
				return method.Parameters[i];
			}
		}

		return null;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the type resolves to a model
	/// class (i.e., requires <c>SendAsync</c> with factory method rather
	/// than <c>SendPrimitiveAsync</c>).
	/// </summary>
	private static bool IsModelType(CodeTypeBase type)
	{
		if (type is CodeType codeType)
		{
			if (codeType.TypeDefinition is CodeClass)
			{
				return true;
			}

			// If unresolved but not primitive and not external, treat as model.
			if (!codeType.IsExternal && !CSharpConventionService.IsPrimitiveType(codeType.Name))
			{
				return true;
			}
		}

		return false;
	}

	/// <summary>
	/// Returns <see langword="true"/> when the type resolves to an enum.
	/// </summary>
	private static bool IsEnumType(CodeTypeBase type)
	{
		if (type is CodeType codeType)
		{
			return codeType.TypeDefinition is CodeEnum;
		}

		return false;
	}

	/// <summary>
	/// Escapes a string for use inside a C# string literal.
	/// </summary>
	private static string EscapeStringLiteral(string value)
	{
		if (string.IsNullOrEmpty(value))
		{
			return value;
		}

		return value
			.Replace("\\", "\\\\")
			.Replace("\"", "\\\"");
	}
}
