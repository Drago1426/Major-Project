# AR Card Deck Builder

This project is an interactive AR card deck builder made in Unity. It lets users create or edit a deck, assign card images, stats, 3D models, sound effects, and then scan printed cards to spawn and interact with 3D creatures.

The project uses Vuforia for image tracking. In other words, the card image becomes the marker that Vuforia recognizes through the camera. Once a card is detected, the Unity scripts handle the creature summon, fire tornado particles, stats display, sound effects, and card-based interactions such as the Fireball card.

## Main Features

- Runtime deck creation and editing.
- Custom card images for scanning.
- Runtime Vuforia image targets.
- 3D creature spawning from scanned cards.
- Fire tornado summon effect.
- Creature stats displayed above models.
- Per-card sound effects.
- Fireball ability card support.
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
3. Press Play.
4. Point the phone camera at a printed card.
5. When Vuforia detects the card, the assigned 3D model should appear.
6. Scan the Fireball card to arm the visible model.
7. Click the model in the Unity Game view to fire the fireball effect.

The 3D model should stay visible after looking away from the card, so you can scan an ability card and then interact with the creature.

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
- Fireball sound effect.

The runtime image target loader can load, reload, clear, and print runtime deck targets from the inspector.

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

If the Fireball card does not work:

- Make sure a creature card has already been scanned.
- Make sure the creature is visible.
- Scan the Fireball card.
- The creature should glow yellow.
- Click the creature in the Game view to fire.

## Current Status

This is a prototype for an AR card game / runtime deck builder. It is currently focused on Unity Editor testing with iVCam, but the structure can later be expanded into a mobile app where users create decks, scan physical cards, and interact with AR creatures directly on the phone.
