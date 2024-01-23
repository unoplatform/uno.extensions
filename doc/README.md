# Docs

## Development - how to compile and run the website locally

Here's a set of steps that will allow you to generate and run the Uno.Extensions documentation locally:

- Clone Uno repository locally - this has the main set of documentation.
The satellite docs (ie the docs from repositories for themes, toolkit, extensions, etc) are integrated with the set of docs from the uno repository and merged into a single set of docs.
- Install docfx by running "dotnet tool install -g docfx".
- Install dotnet serve by running "dotnet tool install -g dotnet-serve".
- Open the file _doc\import_external_docs_test.ps1_ and use the branch name to use the latest commit of it, or specify a specific commit sha instead.
- Run "powershell .\import_external_docs_test.ps1" from the doc folder in the Uno repository.
- Browse to the URL at the listening URL with the _site folder (eg Navigate to `http://localhost:63064/`**`_site`**`/articles/intro.html`).
  > One thing to note is that if you click on the Docs link you'll get a 404 because the url includes "docs" in the url
  (eg `http://localhost:63064/`**`docs`**`/articles/intro.html`).
  Replace `docs` in the url with `_site` and you should be able to access docs (eg `http://localhost:63064/`**`_site`**`/articles/intro.html`</pre>).
- Once all changes to docs are complete, do a PR to uno repository updating the doc\import_external_docs.ps1 with the commit that represents the version of docs to be published.
