using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;

namespace Uno.Extensions.Http.Firebase
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddFirebaseHandler(this IServiceCollection services)
        {
            return services.AddTransient<FirebasePerformanceHandler>();
        }
    }
}
