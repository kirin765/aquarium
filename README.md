# Pocket Reef

A playable Unity 6 portrait aquarium prototype inspired by Insaniquarium.
Open `Assets/Scenes/SampleScene.unity` and press Play. The game starts automatically;
no Inspector setup or external art assets are required. Use a 360 × 640 Game view
(or any 9:16 resolution). Mouse clicks in the editor map to screen taps on devices.

## Rules

- Start with $40 and two level 1 goldfish. Buy more for $20 each.
- Tap the tank to drop food for $1. The tank starts with room for one pellet;
  the FEED button drops food and permanently adds one more space. The first
  upgrade costs $10, then each costs 1.75× the previous price. Three meals
  grow a goldfish to level 2.
- Level 1 produces nothing; healthy level 2 fish drop $5 every eight seconds.
- Tap coins to collect them before they expire after 22 seconds.
- Hunters cost $70, eat level 2 goldfish, and drop $15 every eight seconds.
  They hunt every 24 seconds, or sooner when hungry. They do not eat pellets.
- Sharks cost $200, eat level 2 goldfish, and drop $40 every eight seconds. Both
  hunters and sharks lose hunger more slowly than goldfish.
- A tank occasionally drives across the sand, firing missiles at fish. Tap a
  missile to destroy it; a fish hit by one dies.
- The first epidemic starts after 45 seconds; subsequent outbreaks occur every
  40–58 seconds. Sick fish can infect neighbors within 48 game pixels.
- Disease has three ten-second stages, followed by death. Sick fish move slowly
  and stop producing coins. CURE costs $15 and loads the next pellet as medicine:
  a sick fish that eats it is cured, but a healthy fish that eats it dies.
- Select Remove, then tap the sick fish. Removal is free and prevents further
  transmission from that fish. Feed mode restores ordinary feeding.
- Dead fish roll over and sink slowly to the sand.
- Fish only chase ordinary food when hungry; a full fish ignores pellets until
  its hunger drops again. Medicine is always eaten. Hunters need adult prey.
  Hungry fish display a warning.
- Reach current savings of $100, $1,000, $3,000, then $10,000. Advancing keeps
  the existing aquarium and savings. Pause and help stop the simulation.
- If no fish, collectible coins, or money for a new fish remain, restart is offered.

## Implementation and scope

`Assets/Scripts/AquariumGame.cs` contains the simulation, procedural pixel sprites,
and resolution-independent immediate-mode UI. Balance values are near the top of
that file and in `Update`. This prototype uses no remote services or paid assets.
Progress exists only for the current session. Audio, persistence, production art,
accessibility, store integrations, and device testing are still to be added.

## Distribution

Portrait player and web dimensions are configured. Android and WebGL support are
installed locally. Use Unity Build Profiles to produce Android or WebGL builds;
platform builds have not yet been validated. For itch.io, package a WebGL build
with its `index.html` at the archive root and enable mobile/portrait embedding.
Google Play release signing and store submission remain separate release work.
App-in-Toss requires a dedicated host/SDK integration and platform verification;
this repository does not yet provide that integration.

## Validation

The game source was compiled using Unity's bundled C# compiler and Unity reference
assemblies. A full batch editor run could not open the project because it was
already open in another Unity instance. Play-mode, touch, and platform build
checks remain to be performed in that editor.
