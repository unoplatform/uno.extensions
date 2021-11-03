﻿using System;
using System.Threading.Tasks;

namespace Uno.Extensions.Navigation
{
    public class ResponseNavigator : INavigator
    {
        private INavigator Navigation { get; }

        private Type ResultType { get; }

        public TaskCompletionSource<Options.Option> ResultCompletion { get; }

        public Route Route => Navigation?.Route;

        public ResponseNavigator(INavigator internalNavigation, Type resultType, TaskCompletionSource<Options.Option> completion)
        {
            Navigation = internalNavigation;
            ResultType = resultType;
            ResultCompletion = completion;

            // Replace the navigator
            Navigation.Get<IServiceProvider>().AddInstance<INavigator>(this);
        }

        public Task<NavigationResponse> NavigateAsync(NavigationRequest request)
        {
            if (request.Route.FrameIsBackNavigation())
            {
                var responseData = request.Route.ResponseData() as Options.Option;
                if (responseData?.GetValueType() == ResultType)
                {
                    ResultCompletion.TrySetResult(responseData);
                }

                // Restore the navigator
                Navigation.Get<IServiceProvider>().AddInstance<INavigator>(this.Navigation);
            }

            return Navigation.NavigateAsync(request);
        }
    }
}
