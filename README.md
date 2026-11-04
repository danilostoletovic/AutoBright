# AutoBright

<img src="Assets/AutoBright.png" width="96" height="96" alt="AutoBright sun and camera aperture icon">

By [Dan Delivers](https://dandelivers.dev).

A small Windows 11 laptop brightness app. **One purpose. One button. Zero bloat.**

Turn **ON** to estimate room brightness with your camera and adjust your laptop display every two minutes. Use **Sample now** for an immediate adjustment. Close the window: AutoBright exits, and Windows Task Scheduler launches brief sampling runs. Images stay in memory. There is no account, telemetry, application network API, tray icon, service, or idle worker.

Built with .NET 10 WinForms, native Windows camera APIs, WMI brightness control, and Task Scheduler. [AutoDark](https://github.com/danilostoletovic/AutoDark) is its sunrise/sunset theme-switching companion.

## Use

1. Build from source, or download `AutoBright-Setup-win-x64.exe` from [GitHub Releases](https://github.com/danilostoletovic/AutoBright/releases) once a version-tag build has published it. The installer adds a Start menu shortcut for your Windows account without administrator privileges. For portable use, extract `AutoBright-win-x64.zip` into a permanent folder.
2. Open AutoBright, choose a camera, allow Windows camera access, and press the **OFF** button to enable automatic adjustment. The button then reads **ON**.
3. Click **Sample now** whenever you want an immediate adjustment. Close the app when finished; enabled automation continues while the laptop is awake and you are logged on.
4. Reopen and press **ON** to turn it **OFF**, remove its task, and restore the brightness saved before enabling it. Turn OFF before uninstalling or moving a portable copy.

Run `AutoBright.exe`, choose a camera, and press **OFF** to turn automatic adjustment **ON**. The button shows the current state. AutoBright samples immediately and then approximately every two minutes while the computer is awake and this Windows user is logged on. Close the window: Task Scheduler launches short executions, with no AutoBright process left idle. Logon, unlock, and resume also trigger catch-up sampling. Windows scheduling can delay runs; the app does not wake the laptop.

Click **Sample now** to capture and adjust immediately, including while automatic adjustment is OFF. This is a one-time brightness change; OFF restores the brightness saved before automatic mode was enabled, not earlier manual samples. Select cameras while OFF. Turn OFF before moving/deleting the executable or uninstalling. OFF first saves the disabled state, removes the task, and restores each saved display's brightness. Cleanup failures remain visible and can be retried using **OFF · Clean up**. Reconnect an unavailable saved display to complete restoration.

Allow camera access and **Let desktop apps access your camera** under Windows Settings > Privacy & security > Camera. Open the hardware shutter. Windows may show a camera permission prompt or light. An occupied, disconnected, unsupported, or denied camera produces an error and leaves the display unchanged for that sample. If enabling fails after task registration, the app remains ON and reports the failure; turn OFF to stop retries.

## Brightness estimate

Each sample opens only the selected video device (no microphone), captures one JPEG in memory, downsamples to 64 × 48 BGRA pixels, and calculates weighted image luminance. Neither images nor video are written to disk or transmitted. Capture operations have a 30-second cancellation timeout and the camera is disposed after each attempt.

The target curve is `20 + 64 * sqrt(luminance)`, with a five-percentage-point dead band and a maximum change of 15 percentage points per sample. A scene that previously targeted 80% now targets 68%; the curve spans 20–84% to favor a dimmer display. Hysteresis can retain a nearby current value, and large changes converge over multiple samples. Windows drivers may quantize supported brightness levels. The app reads the resulting brightness back from Windows. On multiple supported internal panels, each receives a target based on its current brightness; the UI reports the first panel.

This estimates scene brightness, not lux. Webcam auto-exposure, gain, framing, faces, display light, and a covered lens affect the result. It cannot match a calibrated ambient light sensor or guarantee Android-equivalent accuracy. Camera selection supports Windows video-capture devices that implement photo capture; compatibility with every laptop camera is not guaranteed. A dark/covered lens can cause dimming down to the 20% floor.

Display adjustment uses Microsoft's [WmiSetBrightness API](https://learn.microsoft.com/en-us/windows/win32/wmicoreprov/wmisetbrightness-method-in-class-wmimonitorbrightnessmethods) through native WMI COM, without PowerShell, administrator access, or third-party packages. It requires a driver exposing active `WmiMonitorBrightness` and `WmiMonitorBrightnessMethods` instances. External monitors/DDC-CI are not implemented. Disable competing adaptive-brightness tools if they override the result.

Preferences, the selected camera device ID, original brightness, latest numeric sample and latest error live in `%LOCALAPPDATA%\AutoBright\preferences.json`. Writes replace the file atomically. A per-user session mutex serializes UI and scheduled actions. No image history or growing logs are retained.

## Build and verify

Install the .NET 10 SDK and Windows SDK, or Visual Studio 2026 with .NET desktop development. Open `AutoBright.slnx`.

```powershell
dotnet build AutoBright.csproj -c Release -warnaserror
./bin/Release/net10.0-windows10.0.19041.0/AutoBright.exe --self-test
./bin/Release/net10.0-windows10.0.19041.0/AutoBright.exe --validate-task
dotnet publish AutoBright.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/portable
```

`--self-test` checks luminance, mapping, hysteresis, limits, task XML, and STA dispatcher behavior without accessing the camera or changing brightness/preferences/tasks. `--test-camera` captures one image using the default camera and prints the light estimate without changing display brightness, preferences, or tasks. `--validate-task` asks Windows to validate the XML without registering a task. `--disable` performs real OFF cleanup. `--scheduled` exits without camera/display access while disabled. `--render-ui <directory>` saves a UI PNG without starting sampling.

Camera initialization and capture run on a dedicated STA thread with a short-lived hidden Windows dispatcher, including during scheduled execution. This satisfies MediaCapture's threading requirement without leaving an idle process. Errors identify the failed operation and Windows HRESULT.

Brightness writes bind WMI parameters by name through `ExecMethod_`, read instance properties explicitly, accept providers with no output object, and allow 1.5 seconds for the driver to settle before read-back. `--test-display` writes the existing brightness back unchanged and verifies it. `--test-sample` captures a camera image, applies and verifies its brightness target, then restores and verifies the original brightness in cleanup; it does not enable automation or change preferences. This hardware check briefly changes screen brightness.

Hardware acceptance: sample in dark and bright surroundings, verify camera indicator extinguishes, read resulting brightness in Windows, select another webcam, test shutter/denial/disconnection and a busy camera. Enable, close the app, observe multiple two-minute samples and no idle process; test battery, sleep/resume, unlock/logon, and OFF restoration/removal. Verify 100%, 150%, and 200% display scaling and keyboard navigation. Automated checks do not establish camera/driver compatibility or end-to-end hardware success.

## Packaging and GitHub releases

The GitHub Actions workflow in `.github/workflows/build.yml` builds and tests pushes to `main`/`master` and pull requests. Push a stable `vMAJOR.MINOR.PATCH` tag (for example `v1.0.0`) to build a self-contained x64 EXE, portable ZIP, and per-user Inno Setup installer. The workflow creates a draft release, uploads the installer and ZIP, then publishes it. Tag components determine executable, installer, and MSIX versions. Pushing source to `main` runs checks; publishing downloads requires a version tag.

`AutoBright-Setup-win-x64.exe` installs to `%LOCALAPPDATA%\Programs\AutoBright`, creates a Start menu shortcut, and registers an uninstaller in Windows Installed apps without administrator privileges. Close the app before installing an update. The uninstaller calls `--disable` to remove automation and restore saved brightness; cleanup failure stops uninstall and keeps the installation. Preferences are retained. Uninstall initialization disables automation even if you cancel the later uninstall confirmation. Portable users should turn OFF before moving the EXE or switching to the installer.

Local builds (use fresh output directories):

```powershell
./scripts/Test-ReleaseTools.ps1
./scripts/Build-Portable.ps1 -Tag v1.0.0 -Output artifacts/release
./scripts/Install-InnoCompiler.ps1
./scripts/Build-Installer.ps1 -Tag v1.0.0 -Executable artifacts/release/AutoBright.exe -CompilerPath artifacts/inno-tools/compiler/ISCC.exe
```

The Inno Setup download is pinned to a SHA-256 digest. The EXE/ZIP checks verify x64 GUI architecture, embedded icon, self-contained .NET 10 bundle, version, archive contents/hash, and application self-tests on the original and extracted executable. Downloaded installer/portable binaries are unsigned; Windows SmartScreen may warn. `-SkipAudit` on Build-Portable is only for offline local builds; GitHub CI retains the online NuGet audit.

## Microsoft Store MSIX

AutoBright's supplied Partner Center identity is committed in `packaging/AppxManifest.xml`, so Store packaging works without repository identity variables:

- Package Name: `DaniloStoletovic.AutoBright`
- Publisher: `CN=65C1819A-0B07-4365-9B17-61242ED51E2F`
- Publisher Display Name: `Danilo Stoletovic`
- Package Family Name: `DaniloStoletovic.AutoBright_f24tm8hnzqja0`
- Store ID: `9NK5LM17C8ZM` ([Store link](https://apps.microsoft.com/detail/9NK5LM17C8ZM); registration does not establish publication).

Optional overrides live in GitHub repository **Settings > Secrets and variables > Actions > Variables**:

| Repository variable | Required value |
| --- | --- |
| `STORE_IDENTITY_NAME` | Optional replacement Package/Identity Name |
| `STORE_PUBLISHER` | Optional exact replacement Publisher distinguished name, beginning `CN=` |
| `STORE_PUBLISHER_DISPLAY_NAME` | Optional exact replacement Publisher Display Name |
| `STORE_PACKAGING_ENABLED` | Optional: `false` disables Store packaging |

Version tags also build and validate `AutoBright-<version>.0-x64.msix`, upload a Store-submission Actions artifact, and attach the MSIX to the GitHub release. Store packaging runs by default; `STORE_PACKAGING_ENABLED=false` disables it. Invalid or placeholder identity overrides fail packaging.

The package uses the Windows 11 desktop `packagedClassicApp`/`mediumIL` architecture with `runFullTrust` and `webcam`, without microphone, location, `unvirtualizedResources`, or registry exclusions. Its `AutoBright.Store.exe` execution alias lets scheduled tasks survive Store updates; keep that alias enabled in Windows app execution alias settings. [Microsoft documents desktop packaging and Store identity](https://learn.microsoft.com/en-us/windows/msix/desktop/desktop-to-uwp-manual-conversion).

Local Store build (Windows SDK MakeAppx required; committed identity values can be overridden through environment variables or explicit parameters):

```powershell
./scripts/Build-Store.ps1 -Tag v1.0.0 -Executable artifacts/release/AutoBright.exe
# Optional: -IdentityName, -Publisher, -PublisherDisplayName, -MakeAppxPath
```

MakeAppx validates the manifest, packages the same verified EXE and generated icon sizes, then unpacks and checks identity, version, capabilities, activation alias, assets and payload hash. The output is an **unsigned Store submission candidate**, not a generally installable sideload package. Upload it manually in Partner Center; Store certification, capability review, Windows App Certification Kit, and packaged camera/brightness/scheduling tests are still required. No Store submission or approval is automated or implied.

Turn **OFF before uninstalling the Store app**. MSIX provides no cleanup hook here: uninstalling while ON can leave an inert scheduled task pointing to the removed alias and does not restore previous brightness. The EXE installer has its own cleanup hook described above.

The generated icon source is `Assets/AutoBright.png`; `Assets/AutoBright.ico` contains 16, 24, 32, 48, 64, 128 and 256 px frames. `./scripts/Build-Icon.ps1` regenerates the ICO from the source. Store logo sizes are produced during MSIX packaging. Generation details are recorded in `Assets/README.md`.

## Validation and limitations

Local validation passed a Release build with warnings as errors, 117 application checks, release-tool checks, portable EXE/ZIP validation, installer compilation, and MakeAppx packaging/unpacking with the supplied Store identity. A camera-to-display hardware sample changed brightness, verified the read-back, and restored the original brightness on the development laptop. This does not establish compatibility with every camera/display, packaged end-to-end behavior, Store certification, or all sleep/logon recovery paths; use the manual checklist above.

Dimmer targets favor a lower screen brightness, but battery savings have not been measured. Each sample briefly activates the camera and runs image processing. Auto-exposure makes image luminance an estimate rather than a calibrated ambient-light measurement.

[Privacy policy](PRIVACY.md) | [MIT license](LICENSE.txt)

Copyright (c) 2026 Dan Delivers. Released under the MIT license.
