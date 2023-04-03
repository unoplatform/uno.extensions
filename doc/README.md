# Docs

## Development - how to compile and run the website locally

Here's a set of steps that will allow you to generate and run the Uno.Extensions documentation locally:
- Clone Uno repository locally - this has the main set of documentation.
The satellite docs (ie the docs from repositories for themes, toolkit, extensions etc) are integrated with the set of docs from the uno repository and merged into a single set of docs.
- Open the file _doc\import_external_docs.ps1_ and update the commit id for the uno.extensions repository to be either the latest commit on main,
or if you want to review docs you've been working on, use the latest commit on your branch.  
Alternatively, for testing purposes, just use the branch name to use the latest commit of it.
- Run "powershell .\import_external_docs.ps1" from the doc folder in the Uno repository.
  > If you get an error stating that "_doc\articles\external already exists_", remove the _doc\articles\external_ folder and try again
- Install docfx by running "dotnet tool install -g docfx".
- Run docfx from the doc folder.
  > If you get any errors, try removing the troublesome references from the _toc.yml_ file (inspect the error and search for that file).
- Install dotnet serve by running "dotnet tool install -g dotnet-serve".
- Run dotnet serve from doc folder eg "dotnet-serve".
- Browse to the URL at the listening URL with the _site folder (eg Navigate to http://localhost:54361/_site/).  
  >One thing to note is that if you click on the Docs link you'll get a 404 because the url includes "docs" in the url
  (eg `http://localhost:63064/`**`docs`**`/articles/intro.html`).
  Replace `docs` in the url with `_site` and you should be able to access docs (eg `http://localhost:63064/`**`_site`**`/articles/intro.html`</pre>).
- Once all changes to docs are complete, do a PR to uno repository updating the doc\import_external_docs.ps1 with the commit that represents the version of docs to be published.
