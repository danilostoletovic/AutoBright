# AutoBright privacy

AutoBright processes one camera image locally when you request a sample or when enabled automation runs. It requests video only, not audio. Images remain in memory and are discarded after processing; they are not saved or uploaded. The camera indicator may illuminate during capture.

The app has no application network calls, telemetry, advertising, or accounts. It stores configuration, the selected camera's Windows device identifier, original display brightness, latest numeric light estimate/brightness/time, and latest error in `%LOCALAPPDATA%\AutoBright\preferences.json`. No image or sample history is retained. Windows itself controls camera permissions and may retain its own access records.

Turn OFF before uninstalling or deleting AutoBright to remove its per-user scheduled task and restore saved brightness. Preferences remain until you delete the AutoBright local-data folder. A manual sample while OFF changes brightness once without creating automation.

The EXE installer's uninstaller attempts OFF cleanup before removing files and stops if cleanup fails. The Store MSIX has no equivalent uninstall cleanup hook: turn OFF in the app before uninstalling it.
