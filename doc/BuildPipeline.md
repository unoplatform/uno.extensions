# CI/CD Pipeline
_[TBD - Review and update this guidance]_

The template for our mobile applications has been tweaked and refined over a long time. The latest version includes a fully functional YAML pipeline and uses [multiple stages](https://devblogs.microsoft.com/premier-developer/azure-devops-pipelines-multi-stage-pipelines-and-yaml-for-continuous-delivery/) in order to properly separate the different steps and reuse the code for similar ones. Here's a little rundown of the different files:

# [.azure-pipelines.yml](../.azure-pipelines.yml)
This is the point of entry for our builds. The sequence is as follows:
  1. Build the application for the staging environment
  2. Release the staging application to **AppCenter** _AND_ simultaneously build the application for the production environment
  3. Release the production application to **AppCenter**, the **Google Play Store** and the **Apple AppStore** (the last two are usually configured to be run manually using [approvals](https://docs.microsoft.com/en-us/azure/devops/pipelines/process/approvals?view=azure-devops&tabs=check-pass#approvals))

## Pull request builds
Due to the length of Xamarin build, this template is configured to behave a little differently when building a pull request (PR). We developed [a set of tools](https://github.com/nventive/MSBuild.UnifiedExtensions/blob/develop/src/Application.Building.Light/Readme.md) to reduce the build time at the cost of some optimizations. This requires a specific variable called `IsLightBuild` to be set, hence why it is appearing in the pipeline. 

Also, being a PR build, we do not find it necessary to release it, so all the relevant stages are disabled.

## Canary
This template also includes build and release stages dedicated for Canary testing our packages. For the build side, the same templates as the other builds are used and we simply pass the required steps through variables. The release process is identical: we only release in AppCenter.

This process is only recommended for some projects at nventive. Feel free to remove it from the pipeline if you don't need it. Leaving it present will not impact the regular build process. 

# Build
## [stage-build.yml](../build/stage-build.yml)
This file is the template used for our different build stages. Due to the constraints of some of the technologies in our stack - namely, UAP and iOS- this stage is split in two jobs: 
- One building Android and UAP and running on Windows
- One building iOS running on macOS

Those 2 jobs rely on the same template for the actual build steps.

**Note**: The WASM portion of this template is not fully functional yet, but it would be running on Linux.

## [steps-build.yml](../build/steps-build.yml)
This is where the exact build steps are defined. The sequence is fairly classic for a .Net application:
1. Install and run [GitVersion](https://gitversion.net/) to calculate the semantic version based on the Git history
1. Install and run NuGet to install the project's dependencies
1. Depending on the platform, install the proper signing certificates
1. Run the build with MSBuild
1. Run Unit Test and publish both the test results and the code coverage results 
1. Push the built artifacts

Because this is built on our internal machines, a cleanup step is also included to prevent the build process from leaving too much clutter on the disk.

One thing to note here is that the build is also taking care of the signature of the application.

# Release

The release stages are even more straigtforward than the build ones. One thing to note is that, for the same reason as it is done at the end of the build steps, a clean-up step is included in every stage.

## [stage-release-appcenter.yml](../build/stage-release-appcenter.yml)
This stage is in charge of pushing the 3 versions of the application to AppCenter. Correspondingly, 3 jobs are run, one for each platform.
 iOS and UAP are very straightforward (download the artifact and push it) but Android is a little more complex: the new AAB format is currently not supported by AppCenter and an APK must be produced; the task [AabConvertToUniversalApk](https://marketplace.visualstudio.com/items?itemName=DamienAicheh.bundletool-tasks) is used to do so.

## [stage-release-appstore.yml](../build/stage-release-appstore.yml)
This stage is in charge of pushing the iOS version to the Apple AppStore. Given that the build stage signs the application, this is as simple as using the proper task and pushing the IPA file. This should only be run for configurations that properly sign the application (in the case of the template, the staging stage signs with a certificate that is not supported by the App Store).

## [stage-release-googleplay.yml](../build/stage-release-googleplay.yml)
Similar to the App Store stage, this stage pushes the AAB produced by the build to the Google Play Store. This is also meant for a properly signed APK.
