# Third-Party Notices

This file separates the project's original MIT-licensed code from third-party components, references, and assets.

## Valve / Steam

- Component: Steam Controller name, Steam name, controller layout captures, and `src/SteamControllerGamepadViewer/wwwroot/assets/controller_config_controller_triton.svg`.
- Source: Steam client Controller Layout / Steam Controller tester UI.
- Notice: Steam, Steam Controller, Valve names, trademarks, UI assets, and controller artwork are property of Valve Corporation. They are not licensed under this repository's MIT license. This project is unofficial and is not endorsed by Valve.
- Reference: https://store.steampowered.com/subscriber_agreement/

## SDL3

- Component: SDL3 dynamic library and gamepad APIs.
- Source: https://github.com/libsdl-org/SDL
- License: zlib license, https://github.com/libsdl-org/SDL/blob/main/LICENSE.txt
- Use in this project: the code dynamically loads `SDL3.dll` from the app folder or an installed Steam folder. The repository does not vendor SDL3 source code.

SDL3 license notice:

```text
Copyright (C) 1997-2026 Sam Lantinga <slouken@libsdl.org>

This software is provided 'as-is', without any express or implied
warranty. In no event will the authors be held liable for any damages
arising from the use of this software.

Permission is granted to anyone to use this software for any purpose,
including commercial applications, and to alter it and redistribute it
freely, subject to the following restrictions:

1. The origin of this software must not be misrepresented; you must not
   claim that you wrote the original software. If you use this software
   in a product, an acknowledgment in the product documentation would be
   appreciated but is not required.
2. Altered source versions must be plainly marked as such, and must not be
   misrepresented as being the original software.
3. This notice may not be removed or altered from any source distribution.
```

## Phetri Input Overlays

- Component: `Phetri-A/Phetri-input-overlays`.
- Source: https://github.com/Phetri-A/Phetri-input-overlays
- License: MIT License, https://github.com/Phetri-A/Phetri-input-overlays/blob/main/LICENSE
- Use in this project: design/workflow reference only. No source files or image assets from this repository are vendored here.

## univrsal input-overlay

- Component: `univrsal/input-overlay`.
- Source: https://github.com/univrsal/input-overlay
- License: GNU GPL v2.0, https://github.com/univrsal/input-overlay/blob/master/LICENSE
- Use in this project: OBS overlay workflow reference only. This project does not link against, copy, or vendor `univrsal/input-overlay` source code or assets.

## Microsoft .NET

- Component: .NET runtime and ASP.NET Core framework libraries.
- Source: https://github.com/dotnet/runtime
- License: MIT License, https://github.com/dotnet/runtime/blob/main/LICENSE.TXT
- Use in this project: framework/runtime used to build and run the app. No NuGet package dependencies are currently declared by this repository.

## AI Assistance

OpenAI Codex assisted with implementation. Codex did not provide a separate third-party code library; the project includes an AI-assistance disclosure in the README so users can make an informed choice.
