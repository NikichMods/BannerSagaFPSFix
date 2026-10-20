# Candidate tests

## 0.1.0-test.1 — first standalone installer candidate

Game behavior reused from accepted BannerSagaUI test.8; no new gameplay modification. New product code is a .NET Framework Windows Forms patcher with original-file backup, atomic replacement and verified restore. No host files in the distribution.

Local tests completed on Windows with .NET Framework release533325: all19 scenarios passed, including actual GUI Check/Install/Restore button activation on an isolated host fixture. Patched compressed SHA equals accepted test.8 ec709c7ecc93c1bf288694b616245eae1b94742b5f0fb543e574c5a7de298166; restored original hash matches7d24a5910b9a0b3dfdae21443e3f0b7edd2510d6269d8407b6d0d31a5178a317. Actual Windows Forms rendering in Russian/English was inspected; package UI acceptance by the user remains pending. Temporary files, images and raw host fixtures stay in ignored local/.

No installation into the user's Steam/DEV copies was performed by the new installer. They retain the earlier manually installed test.8/test.7. A known manual fix without this patcher's backup is recognized as already fixed and cannot be restored by this tool.

Exact executable source/hash and archive identity will be recorded after the first immutable build. Main remains the project foundation, implementation stays on research/first-release. No stable release or Nexus upload yet.
