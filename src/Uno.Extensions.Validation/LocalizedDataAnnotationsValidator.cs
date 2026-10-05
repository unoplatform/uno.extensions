namespace Uno.Extensions.Validation;

/// <summary>
/// DataAnnotations validation which localizes error messages and display names using an <see cref="IStringLocalizer"/>.
/// </summary>
/// <remarks>
/// This mirrors <see cref="System.ComponentModel.DataAnnotations.Validator.TryValidateObject(object, ValidationContext, ICollection{ValidationResult}?, bool)"/>
/// (with validateAllProperties), so results are the same as the BCL ones when no resource key is found:
/// 1. property attributes (Required first, short-circuiting the other attributes of the property);
/// 2. class-level attributes, only if there is no property error;
/// 3. <see cref="IValidatableObject"/>, only if there is still no error.
/// Attribute instances are shared (cached by the TypeDescriptor), so they are never mutated.
/// </remarks>
internal sealed class LocalizedDataAnnotationsValidator
{
	private const string AttributeSuffix = "Attribute";

	private readonly ConcurrentDictionary<Type, TypeMetadata> _metadata = new();
	private readonly IServiceProvider _services;
	private readonly DataAnnotationsLocalizationOptions _options;

	public LocalizedDataAnnotationsValidator(IServiceProvider services, IOptions<DataAnnotationsLocalizationOptions> options)
	{
		_services = services;
		_options = options.Value;
	}

	public bool TryValidateObject(object instance, ValidationContext context, ICollection<ValidationResult> results)
	{
		if (_options.LocalizerProvider(instance.GetType(), _services) is not { } localizer)
		{
			return System.ComponentModel.DataAnnotations.Validator.TryValidateObject(instance, context, results, validateAllProperties: true);
		}

		if (!ReferenceEquals(instance, context.ObjectInstance))
		{
			throw new ArgumentException("The instance provided must match the ObjectInstance on the ValidationContext supplied.", nameof(instance));
		}

		var metadata = _metadata.GetOrAdd(instance.GetType(), static type => new TypeMetadata(type));
		var count = results.Count;

		// 1. Property attributes
		foreach (var property in metadata.Properties)
		{
			var propertyContext = new ValidationContext(instance, context, context.Items) { MemberName = property.Descriptor.Name };
			if (LocalizeDisplayName(property.Display, localizer) is { } displayName)
			{
				propertyContext.DisplayName = displayName;
			}

			Validate(property.Descriptor.GetValue(instance), propertyContext, property.Attributes, property.Required, metadata, localizer, results);
		}

		if (results.Count > count)
		{
			return false;
		}

		// 2. Class-level attributes
		if (metadata.TypeAttributes.Length > 0)
		{
			var typeContext = context;
			if (LocalizeDisplayName(metadata.TypeDisplay, localizer) is { } typeDisplayName)
			{
				// Do not alter the context of the caller.
				typeContext = new ValidationContext(instance, context, context.Items) { MemberName = context.MemberName, DisplayName = typeDisplayName };
			}

			Validate(instance, typeContext, metadata.TypeAttributes, metadata.TypeRequired, metadata, localizer, results);
			if (results.Count > count)
			{
				return false;
			}
		}

		// 3. IValidatableObject (messages are produced by the object itself, which can get the localizer from the context services).
		if (instance is IValidatableObject validatable
			&& validatable.Validate(context) is { } validatableResults)
		{
			foreach (var result in validatableResults)
			{
				if (result != ValidationResult.Success)
				{
					results.Add(result);
				}
			}
		}

		return results.Count == count;
	}

	private void Validate(
		object? value,
		ValidationContext context,
		ValidationAttribute[] attributes,
		RequiredAttribute? required,
		TypeMetadata metadata,
		IStringLocalizer localizer,
		ICollection<ValidationResult> results)
	{
		if (required is not null
			&& required.GetValidationResult(value, context) is { } requiredError)
		{
			results.Add(Localize(required, requiredError, context, metadata, localizer));
			return;
		}

		foreach (var attribute in attributes)
		{
			if (attribute != required
				&& attribute.GetValidationResult(value, context) is { } error)
			{
				results.Add(Localize(attribute, error, context, metadata, localizer));
			}
		}
	}

	private ValidationResult Localize(ValidationAttribute attribute, ValidationResult result, ValidationContext context, TypeMetadata metadata, IStringLocalizer localizer)
	{
		if (attribute.ErrorMessageResourceType is not null || !string.IsNullOrEmpty(attribute.ErrorMessageResourceName))
		{
			return result; // Explicit resources win.
		}

		var key = attribute.ErrorMessage;
		if (string.IsNullOrEmpty(key))
		{
			if (string.IsNullOrEmpty(_options.DefaultMessageKeyFormat))
			{
				return result;
			}

			key = string.Format(CultureInfo.InvariantCulture, _options.DefaultMessageKeyFormat, GetAttributeName(attribute.GetType()));
		}

		var displayName = context.DisplayName;
		if (!string.Equals(result.ErrorMessage, attribute.FormatErrorMessage(displayName), StringComparison.Ordinal))
		{
			return result; // Custom message built by an overridden IsValid, we cannot know its arguments.
		}

		var format = localizer[key];
		if (format.ResourceNotFound)
		{
			return result;
		}

		try
		{
			// Formatted here instead of using localizer[key, args], as the ResourceLoaderStringLocalizer drops the arguments when it falls back from '.' to '/' in keys.
			var message = string.Format(CultureInfo.CurrentCulture, format.Value, GetArguments(attribute, displayName, metadata, localizer));
			return new ValidationResult(message, result.MemberNames);
		}
		catch (FormatException)
		{
			return result; // Invalid translation, keep the non-localized message rather than losing the error.
		}
	}

	private static object?[] GetArguments(ValidationAttribute attribute, string displayName, TypeMetadata metadata, IStringLocalizer localizer)
		=> attribute switch
		{
			StringLengthAttribute stringLength => [displayName, stringLength.MaximumLength, stringLength.MinimumLength],
			RangeAttribute range => [displayName, range.Minimum, range.Maximum],
			LengthAttribute length => [displayName, length.MinimumLength, length.MaximumLength],
			MinLengthAttribute minLength => [displayName, minLength.Length],
			MaxLengthAttribute maxLength => [displayName, maxLength.Length],
			CompareAttribute compare => [displayName, GetOtherPropertyDisplayName(compare, metadata, localizer)],
			RegularExpressionAttribute regex => [displayName, regex.Pattern],
			_ => [displayName],
		};

	private static string GetOtherPropertyDisplayName(CompareAttribute compare, TypeMetadata metadata, IStringLocalizer localizer)
		=> (metadata.Displays.TryGetValue(compare.OtherProperty, out var display) ? LocalizeDisplayName(display, localizer) : null)
			?? compare.OtherPropertyDisplayName
			?? compare.OtherProperty;

	private static string? LocalizeDisplayName(DisplayAttribute? display, IStringLocalizer localizer)
		=> display is { ResourceType: null, Name: { Length: > 0 } key }
			&& localizer[key] is { ResourceNotFound: false } name
			? name.Value
			: null;

	private static string GetAttributeName(Type attributeType)
		=> attributeType.Name.EndsWith(AttributeSuffix, StringComparison.Ordinal)
			? attributeType.Name[..^AttributeSuffix.Length]
			: attributeType.Name;

	private sealed class TypeMetadata
	{
		public TypeMetadata(Type type)
		{
			var properties = new List<PropertyMetadata>();
			var displays = new Dictionary<string, DisplayAttribute>(StringComparer.Ordinal);
			foreach (PropertyDescriptor descriptor in TypeDescriptor.GetProperties(type))
			{
				var attributes = GetExplicitAttributes(descriptor);
				var display = attributes.OfType<DisplayAttribute>().SingleOrDefault();
				if (display is not null)
				{
					displays[descriptor.Name] = display;
				}

				var validationAttributes = attributes.OfType<ValidationAttribute>().ToArray();
				if (validationAttributes.Length > 0)
				{
					properties.Add(new PropertyMetadata(descriptor, validationAttributes, validationAttributes.OfType<RequiredAttribute>().FirstOrDefault(), display));
				}
			}

			var typeAttributes = TypeDescriptor.GetAttributes(type).Cast<Attribute>().ToArray();

			Properties = properties.ToArray();
			Displays = displays;
			TypeAttributes = typeAttributes.OfType<ValidationAttribute>().ToArray();
			TypeRequired = TypeAttributes.OfType<RequiredAttribute>().FirstOrDefault();
			TypeDisplay = typeAttributes.OfType<DisplayAttribute>().SingleOrDefault();
		}

		/// <summary>
		/// Gets the attributes declared on the property, excluding those which the TypeDescriptor merges from the type of the property (same as the BCL ValidationAttributeStore).
		/// </summary>
		private static Attribute[] GetExplicitAttributes(PropertyDescriptor descriptor)
		{
			var typeAttributes = TypeDescriptor.GetAttributes(descriptor.PropertyType).Cast<Attribute>().ToArray();

			return descriptor
				.Attributes
				.Cast<Attribute>()
				.Where(attribute => !typeAttributes.Any(typeAttribute => ReferenceEquals(typeAttribute, attribute)))
				.ToArray();
		}

		public PropertyMetadata[] Properties { get; }

		public IReadOnlyDictionary<string, DisplayAttribute> Displays { get; }

		public ValidationAttribute[] TypeAttributes { get; }

		public RequiredAttribute? TypeRequired { get; }

		public DisplayAttribute? TypeDisplay { get; }
	}

	private sealed record PropertyMetadata(PropertyDescriptor Descriptor, ValidationAttribute[] Attributes, RequiredAttribute? Required, DisplayAttribute? Display);
}
