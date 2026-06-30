# AR Card Deck Builder

This project is an interactive AR card deck builder made in Unity. It lets users create or edit a deck, assign card images, stats, 3D models, sound effects, and then scan printed cards to spawn and interact with 3D creatures.

The project uses Vuforia for image tracking. In other words, the card image becomes the marker that Vuforia recognizes through the camera. Once a card is detected, the Unity scripts handle the creature summon, fire tornado particles, stats display, sound effects, and card-based rule data for future interactions.

## Main Features

- Runtime deck creation and editing.
- Custom card images for scanning.
- Runtime Vuforia image targets.
- 3D creature spawning from scanned cards.
- Fire tornado summon effect.
- Creature stats displayed above models.
- Per-card sound effects.
- Spell/rule card support.
- Unity Game view mouse interaction for testing.
- Phone-as-camera testing through iVCam.

## Requirements

- Unity `6000.3.1f1`.
- A Windows PC capable of running Unity.
- A phone to use as the camera.
- iVCam installed on both the PC and the phone.
- Vuforia Engine.
- Printed card images for physical scanning.

## Vuforia Notes

The project currently references Vuforia as a local Unity package:

```text
com.ptc.vuforia.engine: file:com.ptc.vuforia.engine-11.4.4.tgz
```

If the Vuforia package file is included with the project, Unity should load it automatically through the Package Manager. If Unity reports that Vuforia is missing, install Vuforia Engine manually through Unity's Package Manager or from Vuforia's Unity package.

If Vuforia asks for a license key, create or use a Vuforia developer license key and add it to the Vuforia configuration in Unity.

## iVCam Setup

iVCam is used so your phone can act as the camera inside Unity while testing.

1. Install the iVCam PC client on your computer.
2. Install the iVCam mobile app on your phone.
3. Connect the phone and PC using the same Wi-Fi network or USB.
4. Open iVCam on both the phone and the PC.
5. Make sure the phone camera feed appears in the iVCam PC client.
6. Open the Unity project.
7. If Unity/Vuforia does not automatically use the iVCam camera, select or configure the iVCam webcam device in the Vuforia/AR camera settings.

Once iVCam is connected, pressing Play in Unity should use the phone camera feed as the AR camera input.

## Installing The Project

1. Download or clone the project.
2. Open Unity Hub.
3. Select **Add project from disk**.
4. Choose the project folder:

```text
Major Project
```

5. Open it with Unity `6000.3.1f1`.
6. Wait for Unity to import assets and restore packages.
7. Open the main test scene:

```text
Assets/Scenes/Card Detection Test.unity
```

8. Check that Vuforia loads correctly.
9. Connect iVCam.
10. Press Play.

## How To Use

1. Print the cards you want to scan.
2. Open `Card Detection Test`.
3. Connect iVCam and make sure the phone camera feed is working.
4. Press Play in Unity.
5. Select the object that has the **Deck Runtime Image Target Loader** component.
6. In the inspector, find **Runtime Deck Actions**.
7. Press **Load** to load the deck as runtime Vuforia image targets.
8. Point the phone camera at a printed card.
9. When Vuforia detects the card, the assigned 3D model should appear.
10. Click the model in the Unity Game view to test its configured interaction feedback.
11. Use spell cards in the deck builder to define future gameplay rules such as attacks, buffs, healing, mana gain, draw effects, and shields.

The 3D model should stay visible after looking away from the card, so you can continue inspecting and interacting with the spawned creature.

If the deck was edited while the game is running, press **Reload** on the Deck Runtime Image Target Loader so Vuforia clears the old runtime targets and loads the updated deck.

## Creating Custom Cards

This project is designed as a deck builder, so users can add their own card images and use the system however they want.

Custom card images can be used, but tracking quality depends on the image. The best card images usually have:

- Clear details.
- Good contrast.
- Sharp edges.
- Unique shapes or artwork.
- Minimal blank space.

Images that are mostly plain colors, blurry, low contrast, or repeated patterns may not scan reliably.

After adding a custom card image, print the card and scan it with the phone camera. The physical printed card should match the image used in the deck builder as closely as possible.

## Project Scene

The main scene currently used for testing is:

```text
Assets/Scenes/Card Detection Test.unity
```

Other scenes may exist in the project, but `Card Detection Test` is the active card scanning and deck testing scene.

## Deck Builder Notes

The deck builder lets you configure cards with:

- Card name.
- Card type.
- Quantity.
- Card image.
- Target width.
- Creature stats.
- 3D model prefab or resource path.
- Summon sound effect.
- Optional effect sound data for spell/rule cards.

The runtime image target loader can load, reload, clear, and print runtime deck targets from the inspector.

## Building A Deck

The deck is created through the **Deck Builder Manager** in the Unity Inspector.

1. Open `Assets/Scenes/Card Detection Test.unity`.
2. Select the GameObject that has the **Deck Builder Manager** component.
3. In the **Deck** section, enter the deck name and choose the game type.
4. In the **Card** section, enter the card name and quantity.
5. Choose the card type. For example, use `Creature` for a creature card or `Spell` for a spell/ability card.
6. Assign the card image. This is the image that must be printed and scanned.
7. If the card is a creature, fill in its health, damage, and mana values.
8. In **Model And Target**, assign a model prefab or enter a Resources path for the model.
9. In **Sounds**, assign optional summon and effect sound data.
10. Press **Add Card** to add the card to the working deck.
11. Repeat the process for every card you want in the deck.
12. Press **Save Deck**.

After saving, enter Play Mode and use the **Deck Runtime Image Target Loader** to load the deck into Vuforia.

## Loading The Runtime Deck

The **Deck Runtime Image Target Loader** is the component that turns the saved deck cards into runtime Vuforia image targets. The deck may exist in the deck builder, but Vuforia will not scan those cards until the runtime targets are loaded.

1. Press Play.
2. Select the GameObject with the **Deck Runtime Image Target Loader** component.
3. Check that **Deck Id To Load** matches the deck you want to test.
4. Press **Load** under **Runtime Deck Actions**.
5. Check the Unity Console. It should say how many runtime targets were created, skipped, or failed.
6. Start scanning the printed cards.

Use these buttons while testing:

- **Load**: loads the selected deck into Vuforia.
- **Reload**: clears the current runtime targets and loads the deck again. Use this after editing and saving a deck.
- **Clear Targets**: removes the runtime targets currently loaded from the deck.
- **Print Loaded**: prints the loaded runtime target names to the Console.
- **Clear Loaded Cache**: clears the loader's internal list of loaded target names.

The **Load** and **Reload** buttons should be used in Play Mode because Vuforia needs to be initialized before runtime image targets can be created.

## Creature Cards And Spell Rules

Creature cards are cards that spawn 3D models. These should have a card image and a model assigned.

Spell cards are used to define rule data. The current rule fields support effects such as attack, damage buff, health buff, healing, mana gain, card draw, and shield. Each spell can also store a target type, amount, duration, mana cost, and optional effect sound path.

The current prototype saves this spell/rule data as part of the deck. A full turn-based battle system can be built on top of these fields later.

## Printing Cards

To test the AR scanning properly, cards need to be printed or displayed clearly on another screen. Printing is recommended because lighting, reflections, and screen glare can make tracking less stable.

When printing cards:

- Keep the image sharp.
- Avoid glossy reflections if possible.
- Do not crop or stretch the image differently from the deck image.
- Keep the physical card size consistent with the target width set in the deck builder.

## Troubleshooting

If the camera does not work:

- Make sure iVCam is running on the PC and phone.
- Check that the phone and PC are connected.
- Confirm that the iVCam feed appears in the iVCam PC app.
- Check the Vuforia/AR camera settings in Unity.

If cards are not detected:

- Make sure Vuforia is installed and configured.
- Make sure the card image is clear and high contrast.
- Try better lighting.
- Move the camera closer or farther from the printed card.
- Avoid glare on the printed card.

If the 3D model does not appear:

- Check that the card has a model assigned.
- Check that the model is inside a Resources folder if it is loaded through a resource path.
- Check the Unity Console for missing prefab, image, or Vuforia errors.

If a spell/rule card is not configured correctly:

- Make sure the card type is set to `Spell`.
- Make sure the effect type is not `None`.
- Make sure the effect target is set.
- Add an effect amount for rules that need a number, such as attack, buff, heal, mana, draw, or shield.
- Save the deck again before loading or reloading it at runtime.

## Current Status

This is a prototype for an AR card game / runtime deck builder. It is currently focused on Unity Editor testing with iVCam, but the structure can later be expanded into a mobile app where users create decks, scan physical cards, and interact with AR creatures directly on the phone.
