# Qi's Fading Elevator

![Qi's Fading Elevator](docs/qis-fading-elevator.png)

Qi's Fading Elevator adds a diegetic, repairable elevator to Skull Cavern. The shaft remembers the depths you reach, but that memory fades with time and damage, turning the elevator into a temporary foothold rather than permanent progression.

## Features

- A broken elevator appears at the Skull Cavern entrance on a fresh save.
- Repair it with five iridium bars and one battery pack through an in-world restoration scene.
- The repaired shaft adapts its placement and palette to the entrance and generated cavern floors.
- Reaching a deeper floor renews the shaft's remembered foothold.
- The foothold fades every in-game hour, including time spent sleeping.
- Damage in Skull Cavern causes an immediate source- and severity-based loss.
- A compact gauge inside Skull Cavern shows the live foothold against your personal record.
- Elevator destinations use configurable intervals and always include the exact remembered floor.
- Generic Mod Config Menu support is optional.

The default hourly fade is 5% of the current foothold, adjusted by daily luck. At neutral luck, floor 10 loses one floor in roughly two hours, while floor 100 loses about five floors per hour.

## Requirements

- Stardew Valley 1.6
- [SMAPI](https://smapi.io/) 4.0 or later
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (optional)

## Installation

1. Install SMAPI.
2. Download the latest release from [Nexus Mods](https://www.nexusmods.com/stardewvalley/mods/49041).
3. Extract the `QisFadingElevator` folder into `Stardew Valley/Mods`.
4. Start the game through SMAPI.

Once you have the Skull Key, visit the Skull Cavern entrance and inspect the broken shaft from nearby.

## Configuration

The optional Generic Mod Config Menu integration exposes:

- Master enable switch
- Destination floor interval
- Hourly fade percentage
- Minimum permanent foothold
- Daily-luck influence
- Story notices
- Depth gauge visibility

## Testing commands

These SMAPI console commands are intended for development and troubleshooting:

```text
qfe_status
qfe_foothold <floor>
qfe_decay [hours]
qfe_damage [health lost] [monster|blast|other]
qfe_repair <on|off>
```

## Building from source

Install the .NET 6 SDK, Stardew Valley, and SMAPI, then run:

```powershell
dotnet build -c Release
```

The project uses `Pathoschild.Stardew.ModBuildConfig`. If Stardew Valley isn't installed in the default Windows Steam location, pass its path explicitly:

```powershell
dotnet build -c Release -p:GamePath="D:\Games\Stardew Valley"
```

## Contributing

Bug reports and focused pull requests are welcome. Please include the SMAPI log, game/mod versions, and clear reproduction steps for gameplay issues.

## Support

Qi's Fading Elevator is free. If you enjoy the mod or the other open-source work from Linh's Workshop, you can support ongoing development through [GitHub Sponsors](https://github.com/sponsors/nganlinh4).

## License

The source code is available under the [MIT License](LICENSE). Original visual assets in `assets/` and `docs/` use the separate terms in [ASSETS-LICENSE.md](ASSETS-LICENSE.md).

Stardew Valley is copyright ConcernedApe. This project is an unofficial fan-made mod and is not affiliated with or endorsed by ConcernedApe.
