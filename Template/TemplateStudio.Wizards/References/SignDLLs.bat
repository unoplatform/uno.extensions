
#copy files to Unsigned folder, to by Sign Assemblies 
copy /Y ..\bin\Debug\net472\TemplateStudio.Wizards.dll DLLs\Unsigned\
copy /Y ..\bin\Debug\net472\UNO*.dll DLLs\Unsigned\
copy /Y C:\Windows\Microsoft.NET\Framework64\v4.0.30319\msc*.dll DLLs\Unsigned\
copy /Y C:\Windows\Microsoft.NET\Framework64\v4.0.30319\netstandard.dll DLLs\Unsigned\
copy /Y C:\Windows\Microsoft.NET\Framework64\v4.0.30319\System*.dll DLLs\Unsigned\

#Strong-name sign all assemblies DLL found in DLLs\Unsigned\ and copy the signed versions to DLLs\Signed with your own personal strong-name key.snk file
"C:\Program Files\BrutalDev\.NET Assembly Strong-Name Signer\StrongNameSigner.Console.exe" -in DLLs\Unsigned\ -out DLLs\Signed  -k ..\Keys\key.snk

#Copy the files from the signed directory into the "Libs" folder
copy /Y DLLs\Signed  ..\..\Libs
