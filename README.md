MonoRemoteDebugger
============

MonoRemoteDebugger enables linux remote debugging using Visual Studio 2015.

[![Build status](https://ci.appveyor.com/api/projects/status/y25a6ymkwrt268s1?svg=true)](https://ci.appveyor.com/project/techcap/monoremotedebugger)

Usage
---
Download MonoRemoteDebugger.Server on the linux machine.
> wget https://github.com/techl/MonoRemoteDebugger/releases/download/v1.3.0/MonoRemoteDebugger.Server.zip

Extract MonoRemoteDebugger.Server
> unzip -d MonoRemoteDebugger.Server MonoRemoteDebugger.Server.zip

Run MonoRemoteDebugger.Server on the linux machine.
> cd MonoRemoteDebugger.Server

> mono MonoRemoteDebugger.Server.exe

<br>


Install MonoRemoteDebugger extension. You can find also in the Visual Studio Gallery.

Run Visual Studio 2015.

Toolbar -> MonoRemoteDebugger -> Debug with Mono (remote)

Type remote IP Address .

Click Connect button.

Then the program will run and hit the breakpoint which you set on Visual Studio.

Enjoy you debugging.

<br />

### Known Issue
Not supported breakpoint on user thread.

Not supported Visual Basic.

### Version History
<https://github.com/techl/MonoRemoteDebugger/blob/master/CHANGELOG.md>

<br />

This project is based on [MonoDebugger](https://github.com/giessweinapps/MonoDebugger). Thanks to Christian Giesswein.
