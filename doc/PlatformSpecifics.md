# Platform specifics
_[TBD - Review and update this guidance]_

## Android

#### AndroidManifest
- Information usually found in AndroidManifest is split into three files.
    - `AndroidManifest.xml` for various options, intent filters providers etc.
    - `AssemblyInfo.cs` for the permissions.
    - `Main.cs` for the `<Application>` properties. 

### ApplicationName
- `Label` is set in Main.cs to a resource. For Android this ApplicationName resource is located in `Resources/values/Strings.xml`. Resources.resw in the Shared project is not used for the label on Android.

### Profiled AOT

In order to get better startup performance on Android, this application is bootstrapped using profiled AOT.

This is a special type of compilation that uses a generated file (`custom.aprof`) to optimize the AOT compilation.

To generate this file, following the following steps:

1. Open a command prompt or terminal against your Android project’s directory that contains the .csproj.
1. Ensure only one Android device is attached.
1. Execute the following command: `msbuild /t:BuildAndStartAotProfiling`
1. Let your application run until it’s loaded.
1. Execute the following command: `msbuild /t:FinishAotProfiling`.
1. Use the file in your `.csproj` with LLVM enabled.
```xml
<EnableLLVM>True</EnableLLVM>
<AndroidEnableProfiledAot>True</AndroidEnableProfiledAot>
<AndroidUseDefaultAotProfile>False</AndroidUseDefaultAotProfile>
```

[For more information, follow this article.](https://devblogs.microsoft.com/xamarin/faster-android-startup-times-with-startup-tracing/)

### iOS



### UWP



### WASM


