# LanSchool Monitor (DLL version)



## How to compile
``` bat
csc /target:library /out:LSM.dll Program.cs

"C:\Program Files (x86)\Microsoft SDKs\Windows\v10.0A\bin\NETFX 4.6.1 Tools\ildasm.exe" /out:LSM.il LSM.dll

REM add 'export [1]' to the beginning of the .il file

"C:\Windows\Microsoft.NET\Framework\v4.0.30319\ilasm.exe" LSM.il /DLL /output=LSM.dll

```

## How to use the DLL
``` bat
rundll32 LSM.dll,Main
```
### Or with python (Does not require the last two commands to be run)
``` bat
py LSM.py
```