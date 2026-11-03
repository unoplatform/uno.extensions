using System;
using System.Linq;
using Uno.Extensions.Generators;
using Uno.RoslynHelpers;

namespace Uno.Extensions.Reactive.Generator;

internal partial record CommandFromMethod
{
	private delegate string DeconstructParameters(string parameterName, string ctName);

	private record class CommandConfigGenerator(CommandFromMethod Owner)
	{
		public string? ExternalParameter { get; init; }

		public string? ParametersCoercer { get; init; }

		public string? ParameterType { get; init; }

		public Func<string, string>? CanExecute { get; init; }

		public DeconstructParameters? DeconstructParameters { get; init; }

		/// <inheritdoc />
		public override string ToString()
		{
			var sb = new IndentedStringBuilder();
			using (sb.BlockInvariant(@$"new {NS.Commands}.CommandConfig"))
			{
				if (ExternalParameter is not null)
				{
					sb.AppendLine($"Parameter = {ExternalParameter},");
					sb.AppendLine();
				}
				if (ParametersCoercer is not null)
				{
					sb.AppendLine($"ParametersCoercing = {ParametersCoercer},");
					sb.AppendLine();
				}
				if (ParameterType is not null || CanExecute is not null)
				{
					using (sb.BlockInvariant($@"CanExecute = reactive_commandParameter =>"))
					{
						if (ParameterType is not null && CanExecute is not null)
						{
							sb.AppendLine($@"if (!(reactive_commandParameter is {ParameterType}))
							{{
								return false;
							}}

							var reactive_arguments = ({ParameterType}) reactive_commandParameter;
							if (!({CanExecute("reactive_arguments")}))
							{{
								return false;
							}}".Align(0));
							sb.AppendLine();
						}
						else if (ParameterType is not null)
						{
							sb.AppendLine($@"if (!(reactive_commandParameter is {ParameterType}))
							{{
								return false;
							}}".Align(0));
							sb.AppendLine();
						}
						else if (CanExecute is not null)
						{
							sb.AppendLine($@"if (!({CanExecute("reactive_commandParameter")}))
							{{
								return false;
							}}".Align(0));
							sb.AppendLine();
						}

						sb.AppendLine("return true;");
						sb.AppendLine();
					}
					sb.AppendLine(",");
					sb.AppendLine();
				}
				using (sb.BlockInvariant($@"Execute = async (reactive_commandParameter, reactive_ct) =>"))
				{
					if (ParameterType is not null)
					{
						sb.AppendLine(
							$@"var reactive_arguments = ({ParameterType}) reactive_commandParameter!;

							{DeconstructParameters!("reactive_arguments", "reactive_ct").Align(7)}

							{GetInvoke(Owner.Method.Parameters.Select(p => p.Name).JoinBy(", "))};".Align(0));
						sb.AppendLine();
					}
					else
					{
						sb.AppendLine($@"{GetInvoke(Owner.Method.Parameters.Select(_ => "reactive_ct").JoinBy(", "))};");
						sb.AppendLine();
					}
				}
			}

			return sb.ToString();
		}

		private string GetInvoke(string arguments)
		{
			var invoke = $"{N.Ctor.Model}.{Owner.Method.Name}({arguments})";
			if (!Owner.Context.IsAwaitable(Owner.Method))
			{
				return invoke;
			}

			// On the updated-model (Hot Reload) path the model is `dynamic`: cast the call to its declared type so we don't await
			// a dynamic value, which crashes Roslyn EnC on later edits (https://github.com/dotnet/roslyn/issues/86045).
			// Harmless where the model is statically typed. Remove once Roslyn is fixed.
			return Owner.Method.ReturnsVoid // `async void`, no type to cast to (kept as is)
				? $"await {invoke}"
				: $"await (({Owner.Method.ReturnType.ToFullString()}){invoke})";
		}
	}
}
