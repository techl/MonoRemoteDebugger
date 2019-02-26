# Roadmap

- [ ] Watch complex objects.
- [ ] Support breakpoints on worker threads.

Features that have a checkmark are complete and available for
download in the
[CI build](http://vsixgallery.com/extension/27D183E9-5D2B-44D6-9EC8-2DB329096DF7/).

# Changelog

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

## 1.4.0

- [x] Support VS2019
- [x] Support big file(#49) 

## 1.3.1-beta2

- [x] Fix problem of GTK application(#48)
- [x] nuget update

## 1.3.0
**2017-07-04**

- [x] [Added support for Windows 10 with Mono 64Bit](https://github.com/techl/MonoRemoteDebugger/pull/39/commits/912b5c4f9fac23d21ae2b1313ec08cf68522c57b)
- [x] [New Feature: Debug last content if AppHash is equal. No content transfer needed (reason: faster debugging for embedded devices if content hasn't changed)](https://github.com/techl/MonoRemoteDebugger/pull/39/commits/f4d256c806278ec9bf86c7f799ebe08a2ab90de6)
- [x] [Support for complex objects (arrays, struct, class, local, parameters) and callstack added](https://github.com/techl/MonoRemoteDebugger/pull/39/commits/60215b17fc7667a96d24d1dec091fe3d2f841fbb)
- [x] [Better printing support for string, struct, object, jagged array and multidimensional array](https://github.com/techl/MonoRemoteDebugger/pull/39/commits/83f84c1fd7c38e9fa9ac2a6a8dade60427a7e171)

## 1.2.1
**2017-05-26**

- [x] Support VS2013 in vsix

## 1.2.0
**2017-03-30**

- [x] Support changing MonoRemoteDebugger Server port

## 1.1.0
**2017-03-28**

- [x] Support Visual Studio 2017

## 1.0.11
**2016-08-02**

- [x] Fixed the bug "the parameter is incorrect" which is occurred if the project is inside solution folders.

## 1.0.10
**2016-06-29**

- [x] Fixed the bug running without Xamarin extension.
- [x] Cleared potential mono dll conflicts.
- [x] Support VS15

## 1.0.9
**2016-06-07**

- [x] Fixed the bug that if project name has spaces, it doesn't work.

## 1.0.8
**2016-06-06**

- [x] Changed Mono.Debugger.Soft.dll name for avoiding conflict.

## 1.0.7

**2016-05-14**

- [x] Fixed VS hang bug.

## 1.0.6

**2016-05-14**

- [x] Fixed a bug which cannot find mono path on windows 10 x64

## 1.0.5

- [x] Bug fixed

## 1.0.4

- [x] Fixed conflict with Xamarin VS Extension.

## 1.0.3

- [x] Bug fixed


## 1.0.2

- [x] Support Visual Studio Output Window

- [x] Support deleting Break Point

## 1.0.1

- [x] Support Continue operation

## 1.0.0

- [x] Initial Release
