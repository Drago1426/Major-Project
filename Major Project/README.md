# AR Card Deck Builder

This project is an interactive AR card deck builder and testing tool made in Unity. It lets users create and edit custom card decks, assign card images, 3D models, stats, sound effects, and spell rule data, then scan printed cards through Vuforia to spawn AR creatures.

The project is currently focused on Unity Editor testing rather than being a finished standalone mobile app. It works as a prototype tool for building AR card decks, testing runtime image targets, and preparing the structure for a future AR card game.

## Main Features

- Unity Editor deck builder tool.
- Saved deck creation and editing.
- Four card categories: `Summoner`, `Creature`, `Spell`, and `Land`.
- Custom card images for Vuforia scanning.
- Runtime Vuforia image target loading.
- Saved deck selection during Play Mode.
- 3D model assignment, scaling, and colour tinting.
- Fire tornado summon effect.
- Creature stats displayed above spawned models.
- Summon sound effects.
- Spell-only effect sound and effect path fields.
- Spell/rule data for attacks, buffs, healing, mana gain, card draw, and shields.
- Runtime loader buttons for loading, reloading, clearing, and printing loaded targets.
- Unity Game view interaction for testing.
- Phone-as-camera testing through iVCam.

## Requirements

- Unity `6000.3.1f1`.
- A Windows PC capable of running Unity.
- Vuforia Engine.
- A Vuforia license key if Vuforia asks for one.
- Printed card images for physical AR scanning.
- Optional: a phone and iVCam for phone-as-camera testing.

## Vuforia Notes

The project currently references Vuforia as a local Unity package:

```text
com.ptc.vuforia.engine: file:com.ptc.vuforia.engine-11.4.4.tgz
```

If the Vuforia package file is included with the project, Unity should load it automatically through the Package Manager. If Unity reports that Vuforia is missing, install Vuforia Engine manually through Unity's Package Manager or from Vuforia's Unity package.

If Vuforia asks for a license key, create or use a Vuforia developer license key and add it to the Vuforia configuration in Unity.

## iVCam Setup

iVCam is used so a phone can act as the camera inside Unity while testing.

1. Install the iVCam PC client on your computer.
2. Install the iVCam mobile app on your phone.
3. Connect the phone and PC using the same Wi-Fi network or USB.
4. Open iVCam on both the phone and the PC.
5. Make sure the phone camera feed appears in the iVCam PC client.
6. Open the Unity project.
7. If Unity or Vuforia does not automatically use the iVCam camera, select or configure the iVCam webcam device in the Vuforia/AR camera settings.

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
9. Connect iVCam if using a phone as the camera.
10. Press Play to test AR scanning.

## Main Scene

The main scene currently used for deck building and AR testing is:

```text
Assets/Scenes/Card Detection Test.unity
```

Other scenes may exist in the project, but `Card Detection Test` is the main card scanning and deck testing scene.

## Deck Builder Tool

The project can be used through the custom editor window:

```text
Tools > AR Deck Builder
```

It can also be used through the **Deck Builder Manager** component in the inspector.

The tool lets users configure:

- Deck name.
- Existing saved deck to edit.
- Card name.
- Card type.
- Quantity.
- Card image.
- Target width.
- Creature stats.
- 3D model prefab or Resources path.
- Runtime OBJ path.
- Model scale.
- Model tint.
- Summon sound.
- Spell effect type.
- Spell effect target.
- Spell effect amount.
- Spell effect duration.
- Spell mana cost.
- Spell effect sound.

The **Deck Builder Manager** now automatically tries to find the **Deck Runtime Image Target Loader** when the scene loads, when scripts reload, and when Play Mode stops. If the reference is still missing, use **Auto Fill Scene References** in the AR Deck Builder window or assign the loader manually.

## Card Types

The project uses four general card categories:

- `Summoner`: a flexible card type for custom deck ideas or future summoning rules.
- `Creature`: a card that can spawn a 3D creature model and display health, damage, and mana.
- `Spell`: a card that stores rule/effect data such as attack, buff, heal, mana gain, draw, or shield.
- `Land`: a flexible support card type for future resource or board rules.

The old MTG and Pokemon preset system has been removed. The project now uses these neutral card categories instead.

## Building A Deck

1. Open `Assets/Scenes/Card Detection Test.unity`.
2. Open **Tools > AR Deck Builder**.
3. Check that the **Deck Builder** field has found the scene's Deck Builder Manager.
4. In **Deck Setup**, enter the deck name.
5. In **Card Creator**, enter the card name and quantity.
6. Choose the card type: `Summoner`, `Creature`, `Spell`, or `Land`.
7. Assign the card image. This is the image that must be printed and scanned.
8. If the card is a creature, enter health, damage, mana, and assign a model.
9. If the card is a spell, enter the spell/rule effect data. Effect sound and effect path only appear for spell cards.
10. Assign optional model scale, model tint, and summon sound.
11. Press **Add Card To Working Deck**.
12. Repeat the process for every card in the deck.
13. Press **Save Deck**.

Saved decks are stored in the deck database/persistent save data and can be loaded again for editing.

## Editing An Existing Deck

1. Open **Tools > AR Deck Builder** or select the **Deck Builder Manager** component.
2. In the saved deck section, press **Refresh Saved Decks** if the list is empty or outdated.
3. Choose the deck from the saved deck dropdown.
4. Press **Load Selected For Editing**.
5. The deck's cards will appear in the working deck list.
6. Add, remove, or change cards in the working deck.
7. Press **Save Deck** to update the same deck.

Use **New Deck** when you want to clear the current editing state and start a separate deck.

## Loading A Runtime Deck

The **Deck Runtime Image Target Loader** is the component that turns saved deck cards into runtime Vuforia image targets. A deck can exist in the deck builder, but Vuforia will not scan those cards until the runtime targets are loaded.

1. Press Play.
2. Select the GameObject with the **Deck Runtime Image Target Loader** component.
3. In **Deck Source**, choose a deck from the **Saved Deck** dropdown.
4. Press **Refresh Saved Decks** if the deck list is empty or outdated.
5. Check that **Deck Id To Load** updates automatically.
6. Press **Load** under **Runtime Deck Actions**.
7. Check the Unity Console. It should say how many runtime targets were created, skipped, or failed.
8. Start scanning the printed cards.

Use these buttons while testing:

- **Load**: loads the selected deck into Vuforia.
- **Reload**: clears the current runtime targets and loads the selected deck again.
- **Clear Targets**: removes the runtime targets currently loaded from the deck.
- **Print Loaded**: prints the loaded runtime target names to the Console.
- **Clear Loaded Cache**: clears the loader's internal list of loaded target names.

The **Load** and **Reload** buttons should be used in Play Mode because Vuforia needs to be initialized before runtime image targets can be created.

## AR Scanning Workflow

1. Print the cards you want to scan.
2. Press Play in Unity.
3. Select the runtime deck from the Deck Runtime Image Target Loader.
4. Press **Load**.
5. Point the camera at a printed card.
6. When Vuforia detects the card, the assigned 3D model should appear.
7. The model can appear with summon VFX, sound, model tint, and stats.
8. Click the model in the Unity Game view to test its interaction feedback.

The 3D model should stay visible after looking away from the card, so it can continue to be inspected and tested.

## Models And Sounds

Creature cards should have a model assigned. The tool supports:

- A model prefab inside a Unity `Resources` folder.
- A Resources path typed manually.
- A selected model copied into the imported Resources model folder.
- A runtime OBJ path for imported models.

Summon sounds can be assigned to cards. Spell cards can also have effect sounds. The effect sound and effect path fields are only shown when the selected card type is `Spell`.

## Spell And Rule Data

Spell cards currently store rule data. The supported effect types are:

- `Attack`
- `BuffDamage`
- `BuffHealth`
- `Heal`
- `ManaGain`
- `DrawCard`
- `Shield`

Each spell can also store:

- Target type.
- Effect amount.
- Duration in turns.
- Mana cost.
- Effect sound path.

This data is saved as part of the deck. The project does not yet include a full turn-based battle system, but these fields provide the foundation for one.

## Generated Outputs

The AR Deck Builder window can generate helper files:

- **Export Deck JSON**: exports the current working deck as a JSON file.
- **Generate Print Sheet**: creates a printable HTML sheet for the current deck's card images.
- **Generate Deck Report**: creates a readable Markdown report of the deck.
- **Reveal Generated Files Folder**: opens the generated output folder.

These files are useful for testing, presenting, or printing cards.

## Creating Custom Card Images

This project is designed as a deck builder, so users can add their own card images and use the system however they want.

Custom card images can be used, but tracking quality depends on the image. The best card images usually have:

- Clear details.
- Good contrast.
- Sharp edges.
- Unique shapes or artwork.
- Minimal blank space.

Images that are mostly plain colours, blurry, low contrast, or repeated patterns may not scan reliably.

After adding a custom card image, print the card and scan it with the camera. The physical printed card should match the image used in the deck builder as closely as possible.

## Printing Cards

To test the AR scanning properly, cards need to be printed or displayed clearly on another screen. Printing is recommended because lighting, reflections, and screen glare can make tracking less stable.

When printing cards:

- Keep the image sharp.
- Avoid glossy reflections if possible.
- Do not crop or stretch the image differently from the deck image.
- Keep the physical card size consistent with the target width set in the deck builder.

## Troubleshooting

If the camera does not work:

- Make sure iVCam is running on the PC and phone if using iVCam.
- Check that the phone and PC are connected.
- Confirm that the iVCam feed appears in the iVCam PC app.
- Check the Vuforia/AR camera settings in Unity.

If saved decks do not appear:

- Make sure a deck has been saved.
- Press **Refresh Saved Decks**.
- Check that the Deck Database reference is assigned.
- Check the Unity Console for save/load errors.

If cards are not detected:

- Make sure Vuforia is installed and configured.
- Make sure the runtime deck was loaded in Play Mode.
- Make sure the card image is clear and high contrast.
- Try better lighting.
- Move the camera closer or farther from the printed card.
- Avoid glare on the printed card.

If the 3D model does not appear:

- Check that the card has a model assigned.
- Check that the card type is correct.
- Check that the model is inside a Resources folder if it is loaded through a Resources path.
- Check that the runtime OBJ path still exists if using a custom model path.
- Check the Unity Console for missing prefab, image, or Vuforia errors.

If a spell/rule card is not configured correctly:

- Make sure the card type is set to `Spell`.
- Make sure the effect type is not `None`.
- Make sure the effect target is set.
- Add an effect amount for rules that need a number, such as attack, buff, heal, mana, draw, or shield.
- Save the deck again before loading or reloading it at runtime.

## Current Status

This is a prototype for an AR card game deck builder and runtime testing tool. It is currently focused on Unity Editor testing with Vuforia and iVCam, not a finished standalone mobile app.

The project demonstrates custom deck creation, deck editing, runtime deck selection, AR card scanning, model spawning, summon effects, stats, sounds, and spell/rule data. It can later be expanded into a fuller mobile AR card game with turn logic, combat resolution, player state, and complete card-to-card interactions.
