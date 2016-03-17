MonoRemoteDebugger
============

MonoRemoteDebugger enables linux remote debugging using Visual Studio 2015.

Usage
---
Download MonoRemoteDebugger.Server on the linux machine.
> wget https://github.com/techl/MonoRemoteDebugger/releases/download/v1.0.3/MonoRemoteDebugger.Server.zip

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

This project is inspired from MonoDebugger https://github.com/giessweinapps/MonoDebugger

Thanks to Christian Giesswein.
