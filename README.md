MonoTools.Debugger
============

MonoTools.Debugger enables linux remote debugging using Visual Studio 2015.

[![Build status](https://ci.appveyor.com/api/projects/status/y25a6ymkwrt268s1?svg=true)](https://ci.appveyor.com/project/techcap/monoremotedebugger)

Usage
---
Download MonoRemoteDebugger.Server on the linux machine.
> wget https://github.com/techl/MonoRemoteDebugger/releases/download/v1.0.9/MonoRemoteDebugger.Server.zip

Extract MonoTools.Debugger.Server
> unzip -d MonoTools.Debugger.Server MonoTools.Debugger.Server.zip

Run MonoTools.Debugger.Server on the linux machine.
> cd MonoTools.Debugger.Server

> mono MonoTools.Debugger.Server.exe

<br>


Install MonoTools.Debugger extension. You can find also in the Visual Studio Gallery.

Run Visual Studio 2015.

Toolbar -> MonoTools.Debugger -> Debug with Mono (remote)

Type remote IP Address .

Click Connect button.

Then the program will run and hit the breakpoint which you set on Visual Studio.

Enjoy you debugging.

<br />

### Version History
<https://github.com/techl/MonoTools.Debugger/blob/master/CHANGELOG.md>

