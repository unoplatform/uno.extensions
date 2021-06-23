# Template.config

## Platform pragmas

- If you want pragma directives (e.g. `__ANDROID__`) to be in the output without being interpreted as a symbol, you need to use the following comment.

    ```csharp
    //-:cnd:noEmit
    #if __ANDROID__
    ...
    #endif
    //+:cnd:noEmit
    ```

- You can run `template-scripts\escape-if-directives.linq` in linqpad to automatically escape #if directives.
- To remove the escape syntax, you use the search-and-replace feature of vscode:
  ```
  Search: //[-+]:cnd.+
  ^ with regex enabled
  Replace: **LEAVE_THIS_EMPTY**
  files to include: ./src/library/, *.cs
  ^ limits to cs files from src/library folder
  ```
  > note: you can use `collapse all` to confirm the affected files, before doing `replace all`.

## GUIDs

- You can generate/replace guids as described in this [github sample](https://github.com/dotnet/dotnet-template-samples/tree/master/14-guid).
  - For example, it is important for manifests to make sure generated apps have different identifiers.


## References

- [Creating custom templates](https://docs.microsoft.com/en-us/dotnet/core/tools/custom-templates)
- [Properties of template.json](https://github.com/dotnet/templating/wiki/Reference-for-template.json)
- [Comment syntax](https://github.com/dotnet/templating/wiki/Reference-for-comment-syntax)
- [Supported files](https://github.com/dotnet/templating/blob/5b5eb6278bd745149a57d0882d655b29d02c70f4/src/Microsoft.TemplateEngine.Orchestrator.RunnableProjects/SimpleConfigModel.cs#L387)
