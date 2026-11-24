# Per-series introduction starts at zero

This custom Intro Skipper build adds a series-level option. It is based on upstream commit `721697ae88f961200e5afee31204c35296957102`, matching the source revision of Intro Skipper 12.0.8.0. Custom build version: **12.0.8.1**. It targets **Jellyfin 12.4.0**.

## Use

1. Open the Intro Skipper dashboard and its **Timestamps** browser.
2. Select a TV library and the series you want to change.
3. Enable **Start detected intros at zero for this series**.
4. Use the dashboard's **Save** button, then **Scan Series**.
5. Review a few episode timestamps before relying on automatic skipping.

The choice applies to all seasons of that series, including future seasons. Repeat for any other series you want to enable. It is disabled by default.

An automatic intro at `00:43:00.325–00:44:00.440` becomes `00:00:00.000–00:44:00.440`. The end retains the result of the normal detection and boundary adjustments. To avoid global snapping moving that end to the episode runtime, set **Snap to episode start/end threshold** to **0**. Use an **Intro End Offset** of **0** if you do not want an additional end offset.

This option does not improve detection or invent missing intros. Check the detected end first. Manual edits remain manual and are not rewritten. Deleted automatic segments retain their tombstones. Other segment types and unselected series keep their normal behavior. To restore detected starts, disable the checkbox, save and scan the series again.

## Install on Unraid

This is a replacement build of Intro Skipper, with the same plugin id. Do not install two copies alongside one another.

1. Stop the Jellyfin container.
2. Back up Jellyfin's persistent configuration, including the Intro Skipper configuration and databases, outside the active plugin directories.
3. Find the existing `IntroSkipper.dll` under the container's persistent `/config/plugins` mapping. The host path depends on your Unraid appdata mapping.
4. Replace that DLL with `IntroSkipper.dll` from the binary ZIP, retaining its permissions. Keep the backup outside the active plugin directories.
5. Start Jellyfin. Check its logs and dashboard for Intro Skipper **12.0.8.1**, then configure the series checkbox.

The upstream automatic update feed does not distribute this custom feature. An upstream plugin update can replace the custom DLL; preserve this package and review updates before applying them. To roll back, stop Jellyfin and restore your backed-up plugin and configuration before restarting.

## Build and source

Build from the repository root with .NET 10, Node.js and pnpm:

```sh
dotnet build IntroSkipper.sln -c Release
dotnet test IntroSkipper.sln -c Release --no-build
```

The installable assembly is `IntroSkipper/bin/Release/net10.0/IntroSkipper.dll`. The binary ZIP needs no dependency DLLs: Jellyfin supplies its own compatible dependencies. The matching source archive includes the upstream GPL license and the modifications.
