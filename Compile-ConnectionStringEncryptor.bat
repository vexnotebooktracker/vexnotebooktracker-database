@echo off
REM Compiles the tool together with the shared KeyMaterial.cs so it uses the
REM same AES key as the library. Adjust the path to KeyMaterial.cs if the tool
REM lives outside the database project folder.
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc /out:ConnectionStringEncryptor.exe ConnectionStringEncryptor.cs KeyMaterial.cs
