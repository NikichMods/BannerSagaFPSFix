# Banner Saga FPS Fix

Fixes the large FPS drop when opening a hero's details/upgrade screen in **The Banner Saga 1** by stopping the portrait animation on that screen. Portraits remain visible and static. This does not promise higher FPS elsewhere in the game.

The workaround was tested on Steam Windows application **2.58.17 / build 3268685**, Russian locale. One player's affected screen recovered from about 20 FPS to about 100 FPS. Results depend on the system; other game versions and the sequels are not supported by this first profile.

## Install

1. Close The Banner Saga.
2. Extract the patcher archive to a folder outside the game. Run `BannerSagaFPSFix.exe` (Windows 10/11 with .NET Framework 4.8).
3. Select the game folder containing `win32`, then select **Check**.
4. If the original version is supported, select **Install fix**.
5. Start the game normally through Steam.

The language selector switches between English and Russian. The tool changes only `win32/app.game-saga1.air.swf`. It does not edit saves, localization, Steam settings, registry compatibility flags or any other game file. It never launches or terminates the game. No Python or decompiler is needed.

## Restore

Close the game, open the patcher, choose the same game folder and select **Restore original**. The original is kept at `.BannerSagaFPSFix/original.swf` inside the game folder. Do not remove that folder while the fix is installed. The backup is verified before restoration and is preserved afterwards. Repeating install/restore is safe.

If another mod or an update has changed the game file, restoration is refused to preserve those changes. If a manually installed copy of this fix is recognized without a managed backup, the patcher reports that state; use the original installation method to restore it. It does not invent or download an original file.

## Compatibility

Only the verified original SWF is accepted for installation. Unknown versions and other mods modifying the same SWF are refused. No compatibility with those mods is claimed. Steam verification or a game update can replace the modified file; check its status afterwards. Other languages are not edited, but runtime validation so far covers Russian only.

## Source and build

Original source is MPL-2.0. The archive contains no game binaries or assets. See `LICENSING.md`.

On Windows with Git and the .NET Framework C# compiler:

```powershell
./tools/test.ps1
./tools/build.ps1 -OutDirectory ./local/candidates/0.1.0-test.1
```

Commit source before freezing a numbered build. `VERSION` owns semantic versioning; the build derives executable metadata and package name from it. `--status`, `--install` and `--restore` accept a game-folder argument for scripted use. This is a test candidate until the installer interface is accepted; no Nexus or stable release has been published yet.
