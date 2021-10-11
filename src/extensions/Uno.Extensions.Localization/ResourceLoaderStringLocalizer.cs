using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Microsoft.Extensions.Localization;
using Windows.ApplicationModel.Resources;

namespace Uno.Extensions.Localization
{
    /// <summary>
    /// This implementation of <see cref="IStringLocalizer"/> uses <see cref="ResourceLoader"/>
    /// to get the string resources.
    /// </summary>
    public class ResourceLoaderStringLocalizer : IStringLocalizer
    {
        private const string SearchLocation = "Resources";
        private readonly ResourceLoader _resourceLoader;
        private readonly bool _treatEmptyAsNotFound;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResourceLoaderStringLocalizer"/> class.
        /// </summary>
        /// <param name="treatEmptyAsNotFound">If empty strings should be treated as not found.</param>
        public ResourceLoaderStringLocalizer(bool treatEmptyAsNotFound = true)
        {
            _treatEmptyAsNotFound = treatEmptyAsNotFound;
            _resourceLoader = ResourceLoader.GetForViewIndependentUse();
        }

        /// <inheritdoc/>
        public LocalizedString this[string name] => GetLocalizedString(name);

        /// <inheritdoc/>
        public LocalizedString this[string name, params object[] arguments] => GetLocalizedString(name, arguments);

        /// <inheritdoc/>
        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
            => throw new NotSupportedException("ResourceLoader doesn't support listing all strings.");

        private LocalizedString GetLocalizedString(string name, params object[] arguments)
        {
            if (name is null)
            {
                throw new ArgumentNullException(nameof(name));
            }

            var resource = _resourceLoader.GetString(name);

            if (_treatEmptyAsNotFound && string.IsNullOrEmpty(resource))
            {
                resource = null;
            }

            var notFound = resource == null;

            resource ??= name;

            var value = arguments.Any()
                ? string.Format(CultureInfo.CurrentCulture, resource, arguments)
                : resource;

            return new LocalizedString(name, value, resourceNotFound: notFound, searchedLocation: SearchLocation);
        }
    }
}
