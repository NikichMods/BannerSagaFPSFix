# BannerSagaFPSFix

Follow NikichMods/DevRules, beginning with WORKING_RULES.md and its action map; local work follows CODEX_WORKFLOW.md.

## Scope and evidence
Standalone Windows patcher for The Banner Saga 1 Steam application 2.58.17 / build 3268685. Disable only GuiPgDetails.update's portrait.update call. Static hero portraits are the accepted first-release compromise; do not change combat, progression, localization, Steam integration or saves. Do not promise general FPS gains or trilogy compatibility.

Start with docs/PROJECT.md. Reusable host/tooling entry point: NikichMods/BannerSagaUI docs/ENGINEERING_GUIDE.md. Retrieve applicable findings there before fresh host research. Prior exact runtime evidence: BannerSagaUI docs/TEST_BUILD_LOG.md test.7/test.8, source 947eb13f737d5e677eb303e7dece8552b768ea4a, standalone payload ec709c7ecc93c1bf288694b616245eae1b94742b5f0fb543e574c5a7de298166. Keep new product decisions, installer tests and releases here; do not copy the full research history.

## Safety and distribution
Ship only original tooling, license and documentation. Host SWFs, extracted code/assets, private logs and saves stay outside Git and distribution, including private repositories. Accept only the verified original hash for installation. Never overwrite an unknown modified target, a damaged backup or files reached through reparse points. Require a closed game before install/restore; never terminate or launch it automatically. Preserve a verified original backup, use an atomic replacement and verify the result. Restore only a recognized patch with the verified original. Keep existing patched/manual files and their provenance distinct from managed installations.

Use disposable local fixtures for installer tests; default to no writes to the user's Steam or DEV copies. Any real installation needs its own user authorization. Keep temporary fixtures in ignored local/; no game payload in CI.

## Workflow
main is stable; research/* contains unaccepted work. VERSION is the only semantic-version owner. Numbered artifacts are immutable, tied to exact committed source and SHA-256. A tested game modification does not accept a new installer automatically. Build and test locally with Windows .NET Framework csc and tools/build.ps1 / tools/test.ps1; host tests supplement synthetic tests. Accept GUI/install/restore behavior separately before stable promotion. GitHub Releases is the eventual stable download owner; Nexus upload is a separate explicit action.

Original software is MPL-2.0; see LICENSING.md. No third-party runtime is bundled.
