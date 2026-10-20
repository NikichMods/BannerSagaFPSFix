# Candidate tests

## 0.1.0-test.1 — first standalone installer candidate

Game behavior reused from accepted BannerSagaUI test.8; no new gameplay modification. New product code is a .NET Framework Windows Forms patcher with original-file backup, atomic replacement and verified restore. No host files in the distribution.

Local tests completed on Windows with .NET Framework release533325: all19 scenarios passed, including actual GUI Check/Install/Restore button activation on an isolated host fixture. Patched compressed SHA equals accepted test.8 ec709c7ecc93c1bf288694b616245eae1b94742b5f0fb543e574c5a7de298166; restored original hash matches7d24a5910b9a0b3dfdae21443e3f0b7edd2510d6269d8407b6d0d31a5178a317. Actual Windows Forms rendering in Russian/English was inspected; package UI acceptance by the user remains pending. Temporary files, images and raw host fixtures stay in ignored local/.

No installation into the user's Steam/DEV copies was performed by the new installer. They retain the earlier manually installed test.8/test.7. A known manual fix without this patcher's backup is recognized as already fixed and cannot be restored by this tool.

Frozen source: 7fcc6174f0014a2073592647f75efc89c41e4828. Executable BannerSagaFPSFix.exe,28672bytes,SHA-25620de509bb65a9b6b0321a36e1596bf2d03b617d023961232cbf000fa916cc910. Archive BannerSagaFPSFix-0.1.0-test.1.zip,22763bytes,SHA-256c6870f9bd2b4e54156a906acf52122c932fe503db0344cfe36c3152f644e9650. VERSION and metadata agree, source was clean. Archive inspected: exactly executable, two READMEs, LICENSE, LICENSING.md and manifest.json; nohostpayload. The frozen executable itself passed status/install/status/restore on the isolated host, producing the accepted patched hash and byte-exact original restore. Actual personal/DEV installed hashes were rechecked unchanged. Main remains the project foundation, implementation stays on research/first-release. No stable release or Nexus upload yet.
