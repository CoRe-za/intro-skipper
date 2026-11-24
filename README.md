# Intro Skipper custom plugin repository

Installable custom Intro Skipper build with an optional intro start at zero per series, for Jellyfin 12.4.0. The rule preserves normal end adjustments and manual edits.

Add this repository URL in Jellyfin Dashboard → Plugins → Repositories:

```text
https://raw.githubusercontent.com/CoRe-za/intro-skipper/plugin-repository/manifest.json
```

Refresh the catalogue, install/update Intro Skipper 12.0.8.1, then restart Jellyfin. Back up your persistent Jellyfin configuration and plugin databases first. The custom build uses the upstream plugin id; upstream updates may replace the custom feature.

See INSTALL.txt for configuration and manual installation. See VALIDATION.txt for test results. Source: https://github.com/CoRe-za/intro-skipper/tree/228516812f9be02c6cc52124a780a3ac62852415

The ZIP is the same locally built, unsigned binary supplied with this release. License: GPL; see LICENSE and the linked matching source.
