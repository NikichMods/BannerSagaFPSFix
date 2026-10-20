# Project map

## Adopted outcome
A separate Windows patcher for TBS1 hero-card FPS drops, without dependency on BannerSagaUI. User accepted static portraits for the first release on 21 October 2026; preserving animation is not a release prerequisite. Tested game: Steam Windows application 2.58.17 / build 3268685, Russian locale. Earlier user trials restored approximately 100 FPS from approximately 20 in both an isolated development copy and the personal Steam installation. This is one user's hardware/configuration, not a universal benchmark.

## Mechanism and readiness
READY for the narrow port: original game-owned GuiPgDetails.update(int), body18301, only portrait.update argument evaluation/call replaced by ten NOPs, no changed offsets. Only that card's animation update stops; GuiIcon and other animation consumers remain untouched. Existing BannerSagaUI test.8 compared every SWF tag, ABC signature and method body; no other method changed. Core patch profile guards the original compressed SHA-256, decompressed size/hash and patched decompressed hash. Recompression can differ without changing game content; runtime acceptance is tied to the normalized payload as well as installed-file hash.

READY for installation transaction implementation: fixed target win32/app.game-saga1.air.swf under user-selected root, exact original input, verified backup outside win32, same-directory temporary output and File.Replace commit, closed-game check immediately before replacement, exclusive per-target process lock, refuse reparse paths and unknown changes. Backup remains after restore for idempotency. Synthetic failure tests and an original->patched->restore roundtrip on an isolated game fixture are required before handoff. A running-game guard may conservatively block if another Banner Saga instance is open.

## Architecture choice
Use one Windows Forms executable on the Windows .NET Framework, with optional CLI status/install/restore for automation and testing. No Python/FFDec/SDK requirement for end users; builds use csc. A public fixed profile is preferable to shipping game-owned files or a generic binary patch format. UI exposes directory, install state, static-portrait tradeoff and install/restore. Unknown versions/mods fail closed; compatibility patching is deferred until separately justified.

## Current status
Implementation and local verification complete: 19 checks passed, including actual GUI install/restore on an isolated original. Native patched output exactly matches accepted test.8. Russian/English Windows Forms rendering inspected; user acceptance of the new installer remains pending. No Nexus page or stable release exists. VERSION starts independently of BannerSagaUI. This project owns future patcher behavior/releases; BannerSagaUI retains historical runtime evidence and reusable engine knowledge.

Next boundary: test.1 installer/archive is frozen from7fcc617 and passed actual-binary status/install/restore on the isolated host fixture; the user should now inspect its interface and install/restore experience there. Further ordinary gameplay is not required to re-prove the identical already accepted payload. After installer acceptance, prepare the exact candidate for a separately authorized release/Nexus publication task.
