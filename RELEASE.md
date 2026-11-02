# Release procedure

1. Start from a clean committed `master` checkout. Run `pwsh -File tools/Verify.ps1` and require all tests, package checks and isolated consumer assertions to pass.
2. Require the Windows and Ubuntu CI jobs to pass for the exact release commit. Download and retain a specific verified artifact; do not substitute an unchecked rebuild.
3. Review `release-manifest.json`, the nupkg/snupkg inventories, hashes and repository commit. Resolve `packages/BenScr.DelimitedText/1.0.0` to that exact commit.
4. Publish only the explicitly verified `BenScr.DelimitedText.1.0.0.nupkg` and matching `.snupkg`. Never overwrite or silently skip a different existing version.
5. After NuGet validation, verify the public package in a new empty-cache consumer:

```powershell
pwsh -File tools/TestPackage.ps1 -Source https://api.nuget.org/v3/index.json `
    -ExpectedCommit <release-commit> -EvidencePath artifacts/public-consumer.json
```

6. Compare public package payloads with the verified artifact, check the NuGet repository signature, fetch matching public PDBs and verify Source Link against the immutable commit.
7. Check the indexed gallery page and original icon, then record precise URLs and evidence. Create the GitHub release with that same tag and the exact package files.

Until the upload and public checks finish, report the artifact as verified locally or ready to publish, not published. CI performs verification only; it contains no publication secret or implicit upload.
