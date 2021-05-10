# Error handling
_[TBD - Review and update this guidance]_

## Command error handling

If an exception is thrown during the execution of an `ICommand`, the exception is propagated to [HandleCommandException](../src/app/ApplicationTemplate.Shared/Configuration/ErrorConfiguration.cs).

- This handler will log the exception and potentially log it to an analytics provider.

- It will also show a message to the user based on the command / exception.
  - If it's an exception due to the cancellation of the command, nothing is displayed.
  - If it's an exception due to a network connectivity error, a network error message is displayed.
  - If it's any other type of exception you will get a command-specific message.
    - Assuming your command is named `MyCommand`, it will check if there is a matching resource with the name `MyCommand_Error_DialogTitle` / `MyCommand_Error_DialogBody`.
    - If one or both of those resources are missing, it will use the default error resources namely `Default_Error_DialogTitle` and `Default_Error_DialogBody`.

  **Important: Don't catch exceptions in your view model commands to _show a message_, simply add the right resoures!**

## API error handling

### Exception Hub

If an exception is thrown during the execution of an API request, the exception is propagated to an exception hub. You can observe those exceptions if you want. This is configured in `AddExceptionHubHandler` of the [ApiConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/ApiConfiguration.cs) file.

[For more information on the ExceptionHubHandler](https://github.com/nventive/MallardMessageHandlers#exceptionhubhandler).

### Exception Interpreter

You can also convert an API response into an exception by using an `ExceptionInterpreter`.

This is configured in the [ApiConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/ApiConfiguration.cs) file.

[For more information on the ExceptionInterpreterHandler](https://github.com/nventive/MallardMessageHandlers#exceptioninterpreterhandler).

### Network errors

If an exception is thrown during the execution of an API request, a network connectivity check is executed to validate if the user has a network connectivity. If it's not the case, a specific type of exception (`NoNetworkException`) is thrown.

This is configured in the [ApiConfiguration.cs](../src/app/ApplicationTemplate.Shared/Configuration/ApiConfiguration.cs) file.

[For more information on the NetworkExceptionHandler](https://github.com/nventive/MallardMessageHandlers#networkexceptionhandler).

[For more information on the ExceptionInterpreter](https://github.com/nventive/MallardMessageHandlers#exceptioninterpreterhandler).