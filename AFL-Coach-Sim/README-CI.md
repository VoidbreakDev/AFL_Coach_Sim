# CI Setup (Login-based) for AFL Coach Sim

This bundle configures GitHub Actions to authenticate with Unity using **UNITY_EMAIL / UNITY_PASSWORD** (no .ulf needed),
which is compatible with the latest Personal license rules.

## Files
- `.github/workflows/unity-ci.yml` — Login-based Unity EditMode tests on Unity 6000.0.54f1
- `.github/workflows/static-scan.yml` — Heuristic static scan for common Unity/C# pitfalls
- `Tools/ci/static_scan.py` — Scanner script
- `.editorconfig` — Optional IDE analyzer settings

## Required Secrets
Create these in **Settings → Secrets and variables → Actions**:
- `UNITY_EMAIL` — your Unity account email
- `UNITY_PASSWORD` — your Unity account password
- `UNITY_SERIAL` — *(optional)* leave empty for Personal; set if you have a Pro/Plus serial

## Notes
- The workflow logs in to Unity Hub on the runner, runs tests, and logs out automatically.
- Restrict repo write access to protect your secrets.
