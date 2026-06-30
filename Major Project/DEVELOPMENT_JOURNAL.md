# Development Journal

Project: **AR Card Deck Builder**  
Developer: **Peter**  
Development period: **December 2025 - May 2026**  
Project type: **Interactive AR card game / runtime deck builder**

## Journal Purpose

This journal records the development process for the AR Card Deck Builder project. It includes dated entries, observations, technical decisions, problems encountered, and evidence prompts for screenshots or photos. The entries follow the project timeline shown through the Git commit history and are intended to show how the project developed over time.

The project started as an AR image tracking experiment and gradually became a runtime deck builder where users can scan custom card images, spawn 3D models, display stats, trigger animations, and define card-based interaction rules.

## Image Evidence Notes

The journal includes image evidence placeholders. These should be replaced with dated screenshots or photos from the project, such as:

- Unity scene screenshots.
- Inspector screenshots.
- Printed card photos.
- Phone camera / iVCam testing photos.
- Vuforia image target setup screenshots.
- Gameplay screenshots showing spawned models, stats, fire tornado particles, and spell/rule interactions.

Suggested image folder:

```text
Documentation/Journal Images/
```

Suggested file naming style:

```text
YYYY-MM-DD-short-description.png
```

Example:

```text
Documentation/Journal Images/2026-01-09-summon-sequence.png
```

## Peer Testing Notes

Two friends helped with informal testing during development. One tester had Unity experience and focused mainly on the editor workflow, project setup, and how easy the deck builder was to understand. The second tester was a trading card game player and focused more on the card game idea, the feeling of scanning cards, the summon effects, audio, and possible gameplay improvements.

The project was tested with them more than once during the later development stages. Their feedback helped guide several quality-of-life changes, especially around making the runtime deck controls easier to find, improving the visual and audio feedback when a model appears, and deciding which ideas should be kept as future work rather than added before the deadline.

---

## Entry 1 - 15 December 2025

### Project Setup

The project was created and committed to version control. The first stage focused on setting up the Unity project structure, adding the initial project files, and making sure the repository could track the work over time.

At this point, the project was still very broad. The main aim was to create an AR-based card experience, but the exact system for scanning cards, spawning models, and building decks still needed to be explored.

### Work Produced

- Created the Unity project.
- Added the default Unity project structure.
- Added `.gitignore` and basic repository setup.
- Confirmed that the project could be opened and managed through version control.

### Observations

Using Git from the start was useful because the project involved many experiments. This meant that if a feature broke, it would be possible to compare changes or revert to an earlier working state.

### Image Evidence To Add

Add a screenshot of the initial Unity project opened in Unity Hub or the Unity Editor.

```text
Documentation/Journal Images/2025-12-15-project-created.png
```

---

## Entry 2 - 22 December 2025

### AR Test And Vuforia Setup

The first AR test was added. This stage introduced Vuforia into the project and began testing whether Unity could recognize image targets through the camera. This was an important foundation because the entire project depends on card images being detected reliably.

### Work Produced

- Added Vuforia-related project files.
- Created an AR test scene.
- Added Vuforia image target resources.
- Began testing camera-based image recognition.
- Prepared the project for Android/mobile-style AR testing.

### Observations

The first major challenge was understanding what Vuforia actually does. The card image is not just a normal texture; Vuforia analyzes the image for feature points and then uses those feature points to recognize the card through the camera.

This stage made it clear that the quality of the card image matters. Images with strong detail and contrast are easier to track than plain or blurry images.

### Image Evidence To Add

Add a screenshot of the AR test scene and the Vuforia image target setup.

```text
Documentation/Journal Images/2025-12-22-vuforia-ar-test.png
```

---

## Entry 3 - 8 January 2026

### First Creature Summoning Prototype

The project moved from basic image tracking into spawning a creature when a card was detected. A fire dragon model was added, along with a basic summon system that placed the model in the AR scene.

This was one of the first points where the project started to feel like an AR card game rather than only an image tracking test.

### Work Produced

- Added a fire dragon 3D model.
- Added summon rune artwork.
- Created early summoning scripts.
- Connected image target detection to creature appearance.
- Started building a visual summon sequence.

### Observations

The biggest issue at this stage was scale and positioning. AR content needs to appear correctly relative to the printed card, so the model had to be placed and scaled carefully. If the object is too large, too small, or offset badly, the illusion breaks.

### Image Evidence To Add

Add a screenshot or photo showing the first dragon model appearing from a scanned card.

```text
Documentation/Journal Images/2026-01-08-first-creature-spawn.png
```

---

## Entry 4 - 9 January 2026

### Working Summoning Sequence

The summoning sequence was improved so the creature did not simply appear instantly. A rune, fire particles, timed delays, and creature movement were added to make the spawn feel more dramatic.

### Work Produced

- Added fire particle material.
- Improved `PortalSummon`.
- Added timing to the summon sequence.
- Added a rune opening effect.
- Added creature movement during spawn.

### Observations

This stage showed that feedback and timing are important. Even a simple delay before the creature appears makes the interaction feel more intentional. The rune and fire particles helped communicate that the card was summoning something into the scene.

### Image Evidence To Add

Add a screenshot showing the summon rune and fire particles before the creature appears.

```text
Documentation/Journal Images/2026-01-09-summon-rune-fire.png
```

---

## Entry 5 - 9 January 2026

### Tap Interaction

Tap interaction was implemented so AR creatures could respond to user input. Android input compatibility was also adjusted, which was important because the final direction of the project could become a mobile app.

### Work Produced

- Added tap interaction support.
- Connected touch input to creature interaction.
- Fixed Android input system compatibility.
- Began testing user interaction beyond simply scanning a card.

### Observations

This was the first step toward making the creatures feel interactive. Spawning a model is interesting, but interaction makes the project feel more like a game system. This also showed that input needed to work both in the Unity Editor and on a mobile device.

### Image Evidence To Add

Add a screenshot or short sequence showing a creature being tapped/clicked and reacting.

```text
Documentation/Journal Images/2026-01-09-creature-interaction.png
```

---

## Entry 6 - 10 March 2026

### Card Detection System

The project returned to card detection and began building more specific logic around scanned cards. This was an important transition from testing one target to handling card identity more deliberately.

### Work Produced

- Added card detection scripts.
- Logged detected card names.
- Began separating card detection from summon behavior.
- Prepared the system for multiple cards instead of one hard-coded target.

### Observations

At this stage, the project started needing better structure. A single image target could be handled manually, but multiple cards required scripts that could identify which card was detected and respond accordingly.

### Image Evidence To Add

Add a Unity Console screenshot showing detected card logs.

```text
Documentation/Journal Images/2026-03-10-card-detection-log.png
```

---

## Entry 7 - 10 March 2026

### 3D Models Added And Spawned

More 3D models were added and connected to card scanning. This helped test whether the project could support different creatures instead of only the fire dragon.

### Work Produced

- Added additional creature models.
- Tested spawning models from scanned cards.
- Continued improving AR model placement.
- Began moving toward a system where each card could have its own model.

### Observations

Adding multiple models showed that the system needed a cleaner way to assign models to cards. Hard-coding model references would not scale well, especially if the goal was to make a deck builder.

### Image Evidence To Add

Add screenshots showing at least two different models being spawned from different cards.

```text
Documentation/Journal Images/2026-03-10-multiple-models.png
```

---

## Entry 8 - 5 April 2026

### Database Manager And Deck Builder

This was a major development point. A database manager and deck builder were introduced so that card data could be stored and managed rather than manually configured in the scene.

The project direction became clearer here: it was no longer only an AR scanning demo, but a runtime deck builder that could support user-defined cards.

### Work Produced

- Created `DeckDatabase`.
- Created `DeckBuilderManager`.
- Added card data fields such as name, quantity, card type, image, stats, and model path.
- Added saving and loading deck data.
- Began building a workflow for custom decks.

### Observations

This shifted the project from a fixed AR experience to a flexible tool. The main challenge became data flow: card image, model, stats, and later sounds all needed to travel from the deck builder into the runtime AR scene.

### Peer Testing / Feedback

At this stage, the Unity-focused tester tried the deck builder workflow and understood the project setup more easily than a non-Unity user. His main feedback was that the deck builder idea worked, but some actions were hidden in the Inspector and were not obvious enough when testing the AR scene. This feedback was noted for later because the runtime loading controls still needed to become easier to find and use.

### Image Evidence To Add

Add an Inspector screenshot of the Deck Builder Manager with card fields visible.

```text
Documentation/Journal Images/2026-04-05-deck-builder-manager.png
```

---

## Entry 9 - 22 April 2026

### Image Targets For Cards

The project focused on using card images as Vuforia image targets. This was needed so that user cards could become scan targets rather than relying only on pre-made Vuforia targets.

### Work Produced

- Added image target handling for cards.
- Connected card images to the scanning workflow.
- Continued testing printed/visual card recognition.
- Prepared the system for runtime-created image targets.

### Observations

This stage confirmed that card images can be flexible, but tracking quality depends heavily on the image. Detailed, high-contrast images work better than simple designs. This observation is important for user guidance in the final project.

### Image Evidence To Add

Add a screenshot showing card images used as scan targets.

```text
Documentation/Journal Images/2026-04-22-card-image-targets.png
```

---

## Entry 10 - 28 April 2026

### Linking Scanned Cards To Deck Models

The scanned card started using deck data to choose the correct model. This removed the need for every model assignment to be manually set in the scene.

### Work Produced

- Connected scanned cards to the deck database.
- Improved model assignment through deck data.
- Added serialized model fields for easier configuration.
- Tested card-to-model matching.

### Observations

This was a key step toward the deck builder idea. The user should be able to define a card in the deck manager and then see that card spawn the correct model when scanned.

This also revealed the importance of naming consistency. Card names, image target names, and model paths need to match or be mapped clearly.

### Image Evidence To Add

Add a screenshot showing a deck entry and the matching 3D model spawning from that card.

```text
Documentation/Journal Images/2026-04-28-card-to-model-link.png
```

---

## Entry 11 - 29 April 2026

### Reverting And Recovering Work

Some work was reverted after changes caused problems or moved the project away from a stable point. This was a useful part of the process because it showed the importance of version control.

### Work Produced

- Attempted to revert to a previous commit.
- Restored a more stable project state.
- Continued rebuilding the card/model system afterward.

### Observations

Not every change improved the project. Some experiments created issues or made the setup harder to manage. Git made it possible to recover instead of restarting. This entry is important because it shows problem-solving and project management, not only successful feature work.

### Image Evidence To Add

Add a screenshot of the Git history or a note showing the revert point in the timeline.

```text
Documentation/Journal Images/2026-04-29-git-revert-history.png
```

---

## Entry 12 - 29 April 2026

### Scanned Card Models And Stats UI

The project added stronger support for scanned card models and introduced a stats UI. Creature cards could now show values such as health, mana, and damage above the model.

### Work Produced

- Improved scanned card model spawning.
- Added stats display above 3D models.
- Added health, mana, and damage icons.
- Connected stats display to deck card data.

### Observations

The stats UI made the project feel closer to a card game. It also introduced readability issues because white text could be difficult to see in AR. This later led to adding black text strokes/outlines so numbers would remain visible.

### Image Evidence To Add

Add a screenshot showing a spawned creature with health, mana, and damage stats visible.

```text
Documentation/Journal Images/2026-04-29-stats-ui.png
```

---

## Entry 13 - 5 May 2026

### Cleanup Of Scenes And Scripts

Extra scenes and scripts were removed or cleaned up. This helped reduce confusion and made the project easier to navigate.

### Work Produced

- Removed unused scenes and scripts.
- Reduced project clutter.
- Focused the project around the main card detection test scene.
- Improved maintainability.

### Observations

As the project grew, it became harder to know which scenes and scripts were still relevant. Cleanup was necessary so the project could be tested more reliably and handed over more clearly.

### Image Evidence To Add

Add a screenshot of the cleaned Unity project hierarchy or Scenes folder.

```text
Documentation/Journal Images/2026-05-05-project-cleanup.png
```

---

## Entry 14 - 13 May 2026

### Animation Work

The focus moved back to animation and presentation. The project needed stronger visual feedback when a model spawned, especially because the summon effect is one of the main experiences of the app.

### Work Produced

- Continued working on creature animation.
- Improved summon presentation.
- Prepared the project for better particle effects and sound feedback.

### Observations

Animation and visual effects are important for user experience. Without them, the model spawn feels technical rather than magical. The goal became to make the model feel like it is being summoned from the card.

### Image Evidence To Add

Add a screenshot or sequence showing the animation test in the Unity Game view.

```text
Documentation/Journal Images/2026-05-13-animation-work.png
```

---

## Entry 15 - 20 May 2026

### Animations And Sound Effects

Sound effects and improved particle animations were added. The summon effect was developed into a fire tornado / fire swirl style effect, and interaction effects were improved so the creature could react with particles and sound.

### Work Produced

- Added per-card sound effect support.
- Improved fire tornado summon particles.
- Replaced square-looking particles with generated soft/flame textures.
- Added runtime audio loading.
- Added procedural fallback sounds.
- Improved interaction feedback.

### Observations

This stage improved polish significantly. Particles that look like plain squares break immersion, so the generated flame and soft-disc textures made the effects look more intentional. Sound also made the interactions easier to understand.

### Peer Testing / Feedback

The trading card game tester liked the idea of physical cards triggering digital creatures, but felt the summon needed stronger impact to make it feel more like a card ability being activated. Based on this feedback, the particles and sound effects were improved so the model spawn felt more dramatic. This led to focusing on a fire swirl / fire tornado style summon effect, better-looking particles, and stronger audio feedback.

### Image Evidence To Add

Add screenshots showing the fire tornado summon effect and sound effect fields in the deck builder.

```text
Documentation/Journal Images/2026-05-20-fire-tornado-sound.png
```

---

## Entry 16 - 20 May 2026

### Spell Rule Card Prototype

A spell rule card prototype was implemented to explore how cards could do more than spawn models. Instead of limiting the project to creature cards, spell cards can now describe gameplay effects such as attacks, buffs, healing, mana gain, draw effects, and shields.

### Work Produced

- Added spell/rule card behavior data.
- Added fields for effect type, target, amount, duration, mana cost, and effect sound.
- Added a structure that can support attack, buff, healing, mana, draw, and shield effects.
- Kept creature cards focused on spawning and model interaction feedback.
- Made the model stay visible after looking away from the creature card.
- Prepared the project for a future turn-based battle system.

### Observations

This made the card system more game-like. Cards can now do more than spawn models; they can also enable actions. This opens the project to future ability cards such as healing, shields, elemental attacks, or buffs.

One issue discovered during testing was that if the creature disappeared when looking away, card interactions became difficult to follow. This led to changing the behavior so creatures remain visible after being scanned.

### Peer Testing / Feedback

The TCG-focused tester also suggested that the project would feel more complete with a proper app-style UI and full match tracking, such as tracking the state of a game rather than only showing individual cards and effects. These ideas were not fully implemented because of time constraints and because they would have required a much larger gameplay system. Instead, the project was kept focused as an expandable AR deck builder, with spell/rule cards used as a proof of concept for future ability cards.

### Image Evidence To Add

Add three screenshots or photos:

1. Creature visible after scanning.
2. Spell/rule card settings in the deck builder.
3. Creature interaction feedback after clicking the model.

```text
Documentation/Journal Images/2026-05-20-spell-rule-settings.png
Documentation/Journal Images/2026-05-20-creature-interaction-feedback.png
```

---

## Entry 17 - 28 May 2026

### README And Final Documentation

A README file was created to explain the project, requirements, installation process, Vuforia setup, iVCam setup, card printing, and troubleshooting. This was important because the project now had several systems that needed explanation for another user or assessor.

### Work Produced

- Created `README.md`.
- Explained the project purpose.
- Documented requirements.
- Added iVCam setup instructions.
- Added Vuforia notes.
- Added install steps.
- Added scanning and deck builder instructions.
- Added troubleshooting notes.

### Observations

Documentation became necessary because the project is no longer self-explanatory. A user needs to know that iVCam must be installed on both PC and phone, Vuforia must be available, and printed cards are needed to test the AR experience properly.

The README also explains that users can add their own card images, but high-contrast detailed images will scan better.

### Peer Testing / Feedback

The Unity-focused tester suggested turning the runtime deck actions into clear buttons so they were easier to find during testing. This feedback was implemented by making the Deck Runtime Image Target Loader inspector simpler and adding visible buttons for loading, reloading, clearing, printing, and clearing cached runtime deck data.

This also affected the documentation. The README was updated to explain that the user needs to press **Load** on the Deck Runtime Image Target Loader when testing the deck, because the runtime image targets are created when the deck is loaded in Play Mode.

### Image Evidence To Add

Add a screenshot of the README file or repository page showing the documentation.

```text
Documentation/Journal Images/2026-05-28-readme-documentation.png
```

---

## Overall Development Reflection

The project developed from a simple Unity AR test into a runtime AR card deck builder. The first stage focused on setting up Vuforia image tracking and making a 3D creature appear. Later stages added multiple models, deck data, runtime image targets, stats, animations, sound effects, and ability-card interactions.

The most important technical challenge was connecting user-created deck data to runtime AR behavior. The card image, model path, stats, and sounds all needed to be saved in the deck and then loaded into the Vuforia runtime target system. This made the project more flexible because users are not limited to one fixed set of cards.

Another major challenge was interaction flow. Testing showed that if creatures disappeared when the original card was no longer visible, interactions were harder to understand. The solution was to keep the creature visible after scanning, then use spell/rule data as the foundation for future card effects.

Peer testing also helped define the final scope of the project. The feedback showed that a full mobile app UI and match-tracking system would be useful in the future, but the most achievable final version was an AR deck builder that demonstrates custom cards, runtime scanning, creature spawning, stats, sound, particles, and ability-card interaction.

The project now demonstrates:

- AR image tracking.
- Runtime card/deck creation.
- Custom card images.
- 3D model spawning.
- Particle and sound feedback.
- Card-based interactions.
- A foundation for a future mobile AR card app.

## Suggested Final Evidence Checklist

Before submitting, collect or add these dated images:

- `2025-12-15-project-created.png` - initial Unity project.
- `2025-12-22-vuforia-ar-test.png` - Vuforia/image target setup.
- `2026-01-08-first-creature-spawn.png` - first model spawn.
- `2026-01-09-summon-rune-fire.png` - summon rune/fire effect.
- `2026-03-10-card-detection-log.png` - card detection logs.
- `2026-04-05-deck-builder-manager.png` - deck builder inspector.
- `2026-04-28-card-to-model-link.png` - card data linked to model.
- `2026-04-29-stats-ui.png` - health/mana/damage display.
- `2026-05-20-fire-tornado-sound.png` - improved VFX/audio setup.
- `2026-05-20-spell-rule-settings.png` - spell/rule card settings.
- `2026-05-20-creature-interaction-feedback.png` - creature interaction feedback.
- `2026-05-28-readme-documentation.png` - README/documentation.

These images should be placed in a documentation folder and referenced from this journal if required for submission.
