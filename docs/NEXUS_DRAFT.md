# Nexus page draft — not published

Title: Banner Saga Hero Screen FPS Fix

Summary: Stops barely visible portrait animation to remove severe FPS drops on the hero details/upgrade screen. Includes a small Windows installer with a verified original backup and restore.

Tested game: The Banner Saga 1, Steam Windows application 2.58.17 / build 3268685, Russian. Tested symptom: approximately 100 -> 20 FPS when opening any hero's details, recovering on exit. The standalone workaround removed that drop in the user's normal Steam play. This is a reported result on one machine, not a guaranteed benchmark.

Tradeoff: portraits on this screen remain static. This is deliberate and accepted for the first release. Combat, character stats, progression and localization are not changed by the patch. FPS elsewhere is outside the stated scope.

Download: a patcher executable plus instructions and license, no game SWF/assets. Select the folder containing win32, check, install. To uninstall, close the game and restore from the same tool. Preserve .BannerSagaFPSFix/original.swf. No Python/decompiler needed; Windows .NET Framework 4.8 prerequisite. English/Russian interface.

Other versions and mods replacing the same SWF are not supported. Refusal to modify an unknown file is intentional. Existing manually installed fixes without a backup cannot be rolled back by this tool. No automatic game launch, termination or save edits.

Permissions must remain consistent with MPL-2.0 and the public source repository. Credits: original patcher by NikichMods, game by Stoic. Exact accepted package/source/hash and installer acceptance must be added before a publication task. Do not upload a generated game file.
