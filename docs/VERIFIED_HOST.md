# Verified host data

TBS1 Steam Windows application 2.58.17 / build 3268685. No sequel/other-version compatibility is inferred.

- Original compressed SWF SHA-256: 7d24a5910b9a0b3dfdae21443e3f0b7edd2510d6269d8407b6d0d31a5178a317.
- CWS version byte: 36; decompressed body length: 43644613 bytes.
- Original decompressed body SHA-256: 8bcb8396f5bcfde810adcc08855feca3e6749fb8b28f4bda4932a734caffb2d9.
- Patch: ten NOP bytes at decompressed-body offset 42694795 (does not include the eight-byte SWF header).
- Patched decompressed body SHA-256: cbc79b96e1e18a84619280d60a7d03a4be4b12cf65025513ca60f9b14709426b.
- Accepted legacy standalone compressed result SHA-256: ec709c7ecc93c1bf288694b616245eae1b94742b5f0fb543e574c5a7de298166.
- Native .NET Framework 4.8 implementation on the development machine produces that exact compressed hash too. The full decompressed-content check remains authoritative if a platform compression implementation differs.

Owner: game.gui.pages.GuiPgDetails.update(int), ABC tag3671/body18301/method20989. The removed expression was only portrait.update(delta). All instruction offsets, signatures, metadata and other method bodies were compared in BannerSagaUI test.7/test.8. This repository contains no game instructions or asset payload.

Runtime evidence: BannerSagaUI source947eb13f737d5e677eb303e7dece8552b768ea4a/test.8, user-confirmed FPS recovery in the personal Steam installation on 21 October 2026, static-portrait compromise accepted. Source references: NikichMods/BannerSagaUI docs/RESEARCH.md and docs/TEST_BUILD_LOG.md. Known hardware from that case i5-9600KF/RTX4060; this is not a benchmark promise.

Installer tests: 17 synthetic scenarios plus one real GUI sequence on an isolated host fixture and one native original->patch->exact-restore roundtrip (19 total). Covered read-only inspection, unknown version, corrupted/missing backup, repeated install/restore, foreign edits, unmanaged fixed file, running-game refusal, concurrent transaction, locked target, interrupted precommit state, untrusted metadata, checksum errors, wrong profile and reparse rejection. GUI rendering was inspected in Russian and English. These checks prove tested paths, not immunity to all filesystem races, power failures or future game changes.
