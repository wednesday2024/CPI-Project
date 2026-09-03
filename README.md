# Requirements to Open the Project

> [!IMPORTANT]
> ### Unity Version
> - **Unity 6000.3.23f1 LTS**

> [!CAUTION]
> - We do NOT recommend upgrading to Unity 6.5 or newer from this Unity 6.3 branch because it is known to break Fabric Audio completely and many core functions of Club Penguin Island. However, we do have an experimental Unity 6.5 branch that can be found [here](DevAssets/Backup/Unity-6.5+-Fixes).

 ## **Builds can be found here:**
 - [Builds](https://opencpisland.github.io/)

> [!CAUTION]
> ## **How do I clone the repo?**
> - I do NOT recommend using GitHub's built in ZIP download feature. Because it tends to forget to download files which breaks the project. I only recommend using the older HTTPS git clone or the new Git CLI gh repo clone options to download the repo on Linux. If you are on Windows or macOS, I recommend using git clone or using the official GitHub Desktop app. 

> HTTPS method:
> ```git clone https://github.com/OpenCPIsland/CPI-Project.git```

> Git CLI method:
> ```gh repo clone OpenCPIsland/CPI-Project```

> [The Github Desktop app method](https://github.com/apps/desktop)

 ## **Commonly asked questions:**

 Q: Why is everything pink or not loading when I load the game?

 A: You need to run the Unity Editor menu item:```Project -> AssetBundles -> Generated -> Generate client side AssetBundles```
 
 Q: I've upgraded to Unity 6.8 or above. I've regenerated the Asset Bundles but objects are still pink!
 
 A: Unity has made the decision to remove the original Built in Render Pipeline that Club Penguin Island used for shaders. You will need to recreate all of the shaders by hand in Shader Graph to Universal Render Pipeline.
 
 - For other commonly asked questions, you can find those in the ```#faq``` channel of our [Discord server](https://discord.gg/jkWbd3uqTS).

 ### Notes
 - **The Penguin data gets stored here in the Windows Registry:**  
  - **Built .exe client:** `HKEY_CURRENT_USER\SOFTWARE\OpenCPI\CP Island`  
  - **Unity Editor:** `HKEY_CURRENT_USER\SOFTWARE\Unity\UnityEditor\OpenCPI\CP Island`
 - **The Penguin data gets stored here on Linux for the built client but the Editor path is currently unknown because we had to rewrite the data paths to play more nicely with Linux:**
  - `/home/your_username/.config/unity3d/OpenCPI/CP Island/prefs`
 - To launch the game in the Unity editor:
   - Open `Assets/Game/Core/Scenes/Boot.unity`
  - Hit the Play button.
 - **Join our Discord for support, chatting, or for future updates:** [join here](https://discord.gg/jkWbd3uqTS)

> [!IMPORTANT]
> ### New and fixed features within the game

> - What's new/changed:
>     - Added the unreleased igloo furniture ```grand father clock``` from version 1.6.1 to Penguin Level 0.
>     - Added 5 custom party hat recolors (Halloween Party Hat, Holiday Party Hat, Anniversary 19 Party Hat, Anniversary 20 Party Hat, and Anniversary 21 Party Hat)
>     - Added 3 custom duck tube recolors (Blue, Green, and Purple)
>     - Added 1 custom recolor of the ```CakeCruiser``` Tube, the colors matches the 20th Anniversary cake and party hat.
>     - Refined the lightmap baking process.
>     - The Classic Arcade machine has been moved from near Franky's Pizza in Island Central to the sewer in Island Central
>     - Added and optimized support for native macOS Arm Silicon (Apple M1, M2, M3, M4, and newer chips)
>     - Changed the Waddle On login coins award from ```1000000``` to ```0```
>     - Added 2 new lighting options to the igloos. Those are ```Holiday``` and ```Rainbow Migration```. The ```Holiday``` lighting can be unlocked at Penguin level 20 and the ```Rainbow Migration``` lighting can be unlocked at Penguin level 8.
>     - Added an optional skybox in the project to allow a day/night cycle that will cycle every 15 minutes (unfinished)
>     - Added 30 new Penguin colors.
>     - Unlock the ```Valentine's Day chair``` at level 8. The ID for the chair is 278 and it will sell at the Igloo furniture shop for 40 coins.
>     - And most importantly, the game is no longer in the original 32-bit state! This recreation is in a 64-bit state.
>     - Version 1.13.5
>     - Changed the default 3 igloo save slots to 10 (10 is the max, higher than 10 causes data corruption and errors).
>     - Changed the default 130 max igloo furniture limit to 800.
>     - Support for DirectX 12 and Vulkan.
>     - Support for iL2CPP.
>     - Added an annual looping event controller 3000.
>     - Added a total of 60 new Igloo Music tracks. Now you have more different types of music to play in your igloo! 
>     - Added the April Fools Theme, Herbert Style, and Box Dimension to level 0 (before the tutorial level up).
>     - Added The Town (2014) to level 1. 
>     - Added Puffle Party, Puffle Ragtime, Me and My Puffle, Puffle Wild Theme, Come Out To Play, Shoot For The Sun, Coconut, The Best Beach Party, Backbeat Jammin, Forever Summer, Rockhopper Theme, Sunshine Holiday, and Summer Song to level 2. 
>     - Added Lucky One and Gotta Have A Wingman to level 3. 
>     - Added the Medieval Theme and The Royal Court to level 7. 
>     - Added Glam Jam and Rock The Boat Quartet Remix to level 8. 
>     - Added the Alien Lounge to level 9. 
>     - Added Dancing In The Sun, Rave Cave, Rock The Boat, Go Time, Go Time Remix, and Party In My Iggy to level 10.
>     - Added Checker Chuck, Downhill Hoedown, and Sunday Skool to Level 11. 
>     - Added Anchovy Jazz, Beat Them Keys, Maybe Baby, Puffle Dance Jazz Mix, and Cash Or Check to Level 12.
>     - Added Surf Monster, Haunted Disco, Discoween, Ghost just want to dance, Monster masquerade, Nightmare before Christmas's This is Halloween, Puffle Dance Rock Mix, Spooky Jazz, Night of The Living Sled, and What lurks in the night to level 14.
>     - Added Crossing Over to Level 16. 
>     - Added Steer The Funk and Dub Style Step to Level 17. 
>     - Added Holiday Lights, Tis the season, Snowy Holiday, Command Room, We Are The Penguins, and Catching snowflakes to level 20. 
>     - Added Sunny Side to Level 23. 
>     - Added Jazzy Pizza, Coffee Shop, and Pizza Parlor to level 24. 
>     - Added I've Been Delayed, Cumulonimbus, and the Ski Lodge to level 25.
>     - Support for .NET Standard 2.1
>     - Support for Unity's New Input System.
>     - Support for Unity WebGL.
>     - Added 2 new props, the 20th anniversary cake single and the 20th anniversary cake group. 
>     - Support for the unreleased Penguin sprinting and skidding locomotion.
>     - Added 1 new igloo furniture to level 14. It is a recolor of the level 13 ```Waterfall```, named the ```Slime Waterfall```.
>     - Added the Dubstep, Pop, and Rock genres for igloo music.
>     - Added Rainbow Migration and Holiday Party items to the Disney Shop.
>     - Added the ```RDMA 2017 Award``` to the Igloos. That unlocks at level 3.
>     - Added the ```Globe Bean Bag Chair``` from ```WorldPenguinDay2017``` to the Igloos. That unlocks at level 3.
>     - Added the ```Blizzard Beach Palm Tree``` and the ```Blizzard Beach Beach Chair``` from ```BlizzardBeach2017``` to the igloos. That unlocks at level 3.
>     - Added the ```Rainbow Migration``` ```Blender```, ```fruits```, ```Rainbow Smoothie```, and ```Color Post``` to the Igloos. Those unlocks at level 0, except for the ```Color Post``` which unlocks at level 8.
>	  - Added the unused PartySupplies ```Mint GlowStick Single``` to the igloo shop and the diving market which unlocks at Penguin Level 10.
> 	  - Brought back the older version of the effects particles for the ```Science Beaker``` Prop.
>     - Added a new igloo furniture item to the Igloos that is called the ```Chemistry Set``` that can be unlocked at Penguin Level 25 and can be bought at the ```Igloo Interiors``` shop for 75 coins.
>     - Added the ```Picnic Basket``` and ```Picnic Table``` from the Boardwalk to the igloos which can be unlocked at level 0.
>     - Added the ```Wish Squid``` from the Boardwalk to the igloos which can be unlocked at level 1.
>     - Added a whole bunch of Food Truck related igloo furniture to the igloos which can be unlocked at level 0.
>     - Added the Arcade Machine from the Town to the igloos which can be unlocked at level 10.
>     - Added the ```Rockhopper Picture Frame``` to the igloos which can be unlocked by completing the Chapter 1 Episode 1 of Rockhopper. This is a Quest Reward and can't be purchased from the Igloo & Interiors shop.
>     - Added a green variant of the Chemistry Beaker to level 25.
>     - Added a DJ Booth, DJ Pillar, and Purple Stage Curtain to the igloos. These furnitures will be unlocked at level 20.
>     - Added a new collectible named the ```Sea Crystals``` to the Sea Caves.
>     - Added a upcoming events cellphone widget. Which is says the upcoming annual parties/events.
>	  - Added support for Discord's Social SDK for Discord RPC. Note: This feature will only work on the Mono scripting backend and not iL2CPP.
>     - Added a new button to the Debug Menu which allows you to switch to the annual parties, regular mode, or any party for the annual parties controller.
>     - Added a new button to the Debug Menu -> Interactive Zones which allows you to adjust how many penguins that are jumping on the Trampoline 3000 in the Mt. Blizzard.
>     - Added controller support. Very basic at the moment. Left Trigger = Walk, Right Trigger = Sprinting, A = Jump and Select, B = Tube, X = Interaction, Y = Snowball.
>     - Added 4 new sizzle clips, Sleepy, Celebrate, AFK, and Tada.
>     - Added support to the Progression Unlock Service to allow unlocking of equipment instances.
>     - Added support to type in commands via the ingame chat. Currently, there are only 3 commands: !ae {Template.ID}, !at {Tube.ID}, and !ac {coins.amount}.
>     - Added the video trailer button to standalone from mobile. This will appear on the homescreen.
>     - Added the Dot's Clothing Catalog Daily Challenges to Offline Mode.
>     - Added the Daily challenges to Offline Mode.
>     - Added the daily fishing bait limit check to Offline Mode.
>     - Added the ability to exit the Tube Race Lobby in the Offline Room Runner.
>     - Added the ability to gain the 5 coins from the Puffle Treasure Chest in Offline Mode.
>     - Added a whole bunch of custom decals and fabrics.
>     - Added 5 Puffle statues to the igloos. They unlock at Player Level 2.
>     - Added a CPI staff inspired shirt in game.
>     - Added the Wet Suit to Penguin Level 23.
>     - Added the Corsage to Penguin Level 3.
>     - Added a whole bunch of custom emojis.
>     - Added the Box Chair from the Box Dimension to the igloos. That will unlock at Penguin Level 0.
>     - Increased the 30fps cap to 165fps to improve performance.
>     - Support for GPU rendering instead of the original CPU rendering.
>     - Added a few new Disney Store franchises, Incredibles 2, Monsters University, Inside Out, Coco, and Up.

> - What has been fixed:
>     - The spawn points have been moved so you will no longer spawn into the void and endlessly fall randomly like in the original
>     - The ```shoulder pack``` blueprint will correctly work now
>     - The ```modern coffee table``` and ```kitchen island ``` igloo furnitures can now be obtained instead of having it give you the ```teleporter``` igloo furniture
>     - Fixed missing scripts from the Mt. Blizzard Halloween 2018 decorations (this was causing errors in the original client)
>     - Fixed the Halloween 2018 Pumpkins flicker speed to match how they wanted it in the original (the editor and built client shows 2 different results. So the built client would make it too fast)
>     - Added missing colliders to certain world and quest objects
>     - Performance improvements
>     - Added the missing Summer Splashdown chat phases and Rookie sound effects to the Regular sewer in Island Central
>     - Fixed original errors within the ```Unlit Dynamic Object No FOG```, ```World Object```, and Igloo ```CubeMap``` shaders
>     - Fixed the Disney Store banners and for sale items, they originally stopped working on: ```January 1, 2020```. Now they will stop working on: ```December 31, 4065```
>     - Fixed the coins and collectibles that would spawn once a day (this broke when the servers went offline). They will now spawn once every 24 hours
>     - Fixed the microphone and guitar interactables collision in Island Central. The collisions were swapped in the original
>     - Fixed the collider on the ```boss computer chair``` igloo furniture
>     - Fixed the ```boss computer chair``` from spawning a little bit into the ground
>     - Fixed the ```cushion``` and ```stool``` igloo furniture being labeled as a "tube" when it should be labeled as a "ManipulatableObject"
>     - Fixed the ```diamond flower pot``` igloo item collision
>     - Fixed the ```modern coffee table``` and the ```kitchen island``` igloo furniture so that you can properly place items on top of them
>     - Fixed the ```Indoor wall light``` igloo item so that it can be placed properly on the walls of your igloo
>     - Fixed the collision on the ```CrystalCave``` igloo building
>     - Fixed the cosmic daily spin chest reward. It can now be obtainable
>     - Fixed the fishing squid reward (it was appearing far up out of the camera view (original issue))
>     - Fixed the Igloo Music Track ```Too Yule For Skool``` from not playing anything (original issue)
>     - Fixed a pumpkin in the Boardwalk of Halloween 2018 using the wrong material at the ```Sky Cafe``` (original issue), the original issue was making the pumpkin shell light up as well as the inner glow
>     - Some music had their genres switched to fit the music better.
>     - Fixed classification of indoor light fixture to be considered a wall item.
>     - Fixed the SunSet Arcade collision. It wasn't using the ```Terrain Barrier``` layer. You would be able to move through it if you were on your tube originally.
>     - Fixed the HalloweenParty2018 ```SpookyWindow``` Igloo furniture using the wrong material for the frame (original issue).
>     - Fixed the ClassicMiniGame ```Smoothie Smash``` order of fruit animation not containing the fruit which makes the fruit not appear (original issue).
>     - Fixed the ```Snowy Pine Tree``` igloo furniture using the decoration category rather than Landscaping.
>     - Fixed the original bug where the first trampoline on the Platforming wall in the Mt. Blizzard would give the wrong bounce direction.
>     - Fixed the original bug that would spam errors about static infs.
>     - Fixed an original bug with the YikesFace Emoji, it was missing a Reward sprite.
>     - Fixed an original bug with Offline Mode where it won't load your Igloo furniture inventory on data import.
>     - Fixed an original bug where the Disney Shop UI buttons wouldn't play an audio event.
>     - Fixed an original bug where the Cellphone Activity Notifications would start randomly spamming over and over.

> [!IMPORTANT]  
> ## System Requirements

> ### Windows
> - Latest version of Visual Studio Community (Starting with Visual Studio 2026).
> - [Git for Windows](https://gitforwindows.org/) installed. **Restart your PC after installing Git.**

> ### macOS
> - Latest version of Xcode for your version of macOS.

> ### Linux
> - Make sure your system is updated:
>   ```bash
>   sudo apt update && sudo apt upgrade
>   ```
> - Install Git:
>   ```bash
>   sudo apt install git gh
>   ```
>  - Make sure to install this component for X11 Window manager distros (this will be used within the editor and outside of the editor during runtime):
>    ```bash
>    sudo apt-get install libx11-dev
>    ```

> [!IMPORTANT]
> For further documentation, refer to the [OpenCPI Docs](DevAssets/Offline-Project-Instructions). If something is missing, feel free to create a fork and send a pull request.

> ## Special thanks to the following people who have made this restoration possible:

> [Galaxyrelic](https://github.com/Galaxyrelic)

> [ChavalSaturado](https://github.com/ChavalSaturado?tab=repositories)

> [PickleOnAString](https://github.com/PickleOnAString)

> [broimluna](https://github.com/broimluna)

> [shinonasada9](https://github.com/shinonasada9)

> [wednesday2024](https://github.com/wednesday2024)

> [AllinolCP](https://github.com/AllinolCP)

> [Minileandro](https://github.com/Minileandro)

> [Thunder](https://github.com/Ivorplayz)

> [Maksim](https://github.com/loozmax)

> miraculizado (Discord)

> approt (Discord)
