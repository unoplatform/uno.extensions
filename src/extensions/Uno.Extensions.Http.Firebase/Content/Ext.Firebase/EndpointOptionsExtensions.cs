namespace Uno.Extensions.Http.Firebase
{
    public static class EndpointOptionsExtensions
    {
        public static bool UseFirebaseHandler(this EndpointOptions options)
        {
            return options.FeatureEnabled(nameof(UseFirebaseHandler));
        }
    }
}
