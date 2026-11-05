# Requirements to Open the Project

> [!IMPORTANT]
> ### Unity Version
> - **Unity 6000.3.28f1 LTS**

 ## **Builds can be found here:**
 - [Builds](https://opencpisland.com/)

> [!CAUTION]
> ## **How do I clone the repo?**
> - I do NOT recommend using GitHub's built in ZIP download feature. Because it tends to forget to download files which breaks the project. I only recommend using the older HTTPS git clone or the new Git CLI gh repo clone options to download the repo on Linux. If you are on Windows or macOS, I recommend using git clone or using the official GitHub Desktop app. 

> HTTPS method:
> ```git clone https://github.com/wednesday2024/CPI-Project.git```

> Git CLI method:
> ```gh repo clone wednesday2024/CPI-Project```

> [The Github Desktop app method](https://github.com/apps/desktop)

 ## **Commonly asked questions:**

 Q: Why is everything pink or not loading when I load the game?

 A: You need to run the Unity Editor menu item:```Project -> AssetBundles -> Generated -> Generate client side AssetBundles```

 ### Notes
 - **The Penguin data gets stored here in the Windows Registry:**  
  - **Built .exe client:** `HKEY_CURRENT_USER\SOFTWARE\OpenCPI Interactive\CP Island`  
  - **Unity Editor:** `HKEY_CURRENT_USER\SOFTWARE\Unity\UnityEditor\OpenCPI Interactive\CP Island`
 - **The Penguin data gets stored here on Linux for the built client but the Editor path is currently unknown because we had to rewrite the data paths to play more nicely with Linux:**
  - `/home/your_username/.config/unity3d/OpenCPI Interactive/CP Island/prefs`
 - To launch the game in the Unity editor:
   - Open `Assets/Game/Core/Scenes/Boot.unity`
  - Hit the Play button.
  - Or click the "Disney Fast Play" menu item under "Project" from any scene.
 - **Join our Discord for support, chatting, or for future updates:** [join here](https://discord.gg/jkWbd3uqTS)

> [!IMPORTANT]  
> ## System Requirements

> ### Windows
> - Latest version of Visual Studio Community.
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

> [Koloway](https://github.com/Koloway)

> [Maksim](https://github.com/loozmax)

> miraculizado (Discord)

> approt (Discord)
