# Docs

## Development

Here's a set of steps that will allow you to generate and run the Uno.Extensions documentation locally:
- Clone Uno repository locally - this has the main set of documentation. The satellite docs (ie the docs from repositories for themes, toolkit, extensions etc) are integrated with the set of docs from the uno repository and merged into a single set of docs
- Open the file doc\import_extenal_docs.ps1 and update the commit id for the uno.extensions repository to be either the latest commit on main, or if you want to review docs you've been working on, use the latest commit on your branch
- Run "powershell .\import_extenal_docs.ps1" from the doc folder in the Uno repository
- Install docfx by running "dotnet tool install -g docfx"
- Run docfx from the doc folder
- Install dotnet serve by running "dotnet tool install -g dotnet-serve"
- Run dotnet serve from doc folder eg "dotnet-serve"
- Browse to the URL at the listening URL with the _site folder (eg Navigate to http://localhost:54361/_site/) 

One thing to note is that if you click on the Docs link you'll get a 404 because the url includes "Docs" in the url (eg http://localhost:63064/docs/articles/intro.html). Remove Docs from the url and you should be able to access docs (eg http://localhost:63064/articles/intro.html)