# CLAUDE.md

## Deploy

When the user says "deploy", run this sequence from the repo root:

1. **Build** so the shipped DLL is fresh: `& scripts/Build-Mod.ps1` — compiles `src/`
   and copies the DLL into `release/put in game folder/.../Mods/WildfrostAccessibility/`,
   which is tracked in git and is what CI ships. The Release workflow does NOT build,
   so an uncommitted/stale DLL here ships broken.
2. **Commit** everything (including the rebuilt DLL under `release/`) and **push** to `main`.
3. **Tag** the next version and push the tag: check `git tag --sort=-creatordate`,
   bump the minor for features/fixes (`vX.Y.0`), then
   `git tag vX.Y.0 && git push origin vX.Y.0`.
4. **Verify the Release workflow runs**: it triggers on `v*.*.*` tags, zips `release/`
   plus a pandoc-generated readme.html, and publishes a GitHub Release.
   `gh run list --limit 1`, then `gh run watch <run-id>` (or re-check) until it
   reports success. If it fails, fix and re-tag.
