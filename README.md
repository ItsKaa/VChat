## VChat
A simple chat improvement mod for Valheim, a single dll that works for both servers and clients.

## Features
- Adds a new global chat channel that doesn't ping players, this can be a server-hosted instance and users without VChat will be able to access it, however, if the server isn't hosting it then only clients with VChat installed can see the global chat.
- Colours! colours for every chat channel (whisper, local, shout, global) and also colours the input field, even when typing channel commands like `/shout [text]`.
- Sent message history that can be called using arrow up and down when the chat is focused.
- Easily configurable using ingame commands that can be also be adjusted in the configuration file.
- Removes the annoying auto caps and lowercases for the shout and whisper channels.
- Various other settings to improve the ingame chat.

## Commands:
```
/s /l /say /local  
/y /sh /yell /shout  
/w /whisper  
/g /global [text]
```
Switches to the provided chat channel, and if text is entered, also send a message to that channel.
- Examples:
  - "/g Hello VChat" will send a message to the global chat channel and set it as the active channel for your next message.
  - "/sh" will switch the currently active chat channel to shout.

```
/setlocalcolor /setshoutcolor /setwhispercolor /setglobalcolor [color]
```
Changes the chat colour for the chat channel visible in the command name, this accepts either a html string like #ff0000 or a name, like 'red'.

```
/showchat
```
Toggles the chat to always show or hide.

```
/showchatonmessage
```
Toggles the chat to show when a message is sent, this has no effect if /showchat is enabled.

```
/chatclickthrough
```
Toggles if the chat should be click-through, for example when the map is in front of the chat window.

```
/maxplayerchathistory [number]
```
Sets the amount of messages that should be recorded so that the history can be called up with the up and down arrow.
This is disabled if it's set to 0.
Note that these are not saved to a file, restarting Valheim will clear this list.

```
/setdefaultchannel [name]
```
Changes the default channel that's set when logging in, accepted values are: whisper, normal, shout and global.

```
/sethidetime /sethidedelay /setht [seconds]
```
Changes the amount of time in seconds that the chat window will stay active.

```
/setfadetime /setft [seconds]
```
Changes the amount of time in seconds it should take to transition the chat window's opactiy from active to inactive (or hidden), this occurs after the hide timer.

```
/setopacity /set% [0-100]
```
Changes the opacity of the active chat, when pressing enter the opactiy will always be 100.
This value ranges from 0 to 100, where 0 means completely transparent and 100 is fully opaque.

```
/setinactiveopacity /setiopacity /seti% [0-100]
```
Changes the opacity of the inactive chat, this is only relevant when always displaying the chat window.
This value ranges from 0 to 100, where 0 means completely transparent and 100 is fully opaque.


## Installation (manual)
- Download [BepInEx Package](https://valheim.thunderstore.io/package/denikson/BepInExPack_Valheim/) and follow it's installation procedure.
- Download the latest version of VChat from [here](https://github.com/ItsKaa/VChat/releases/latest/).
- Navigate to the Valheim installation folder.
  - For Windows:
    - Open Steam and right-click on Valheim - Manage - Browse local files. This will open the directory where Valheim is installed.
- Extract the contents (or just VChat.dll) of the VChat.zip archive into Valheim/BepInEx/plugins/
- Launch valheim and VChat should load. You can modify the configuration file located in Valheim/BepInEx/config/org.itskaa.vchat.cfg or use the ingame commands.
- Note that for the global chat to work, this mod has to be installed on the server as well, if you are the owner, please repeat this process for the server.

Or you can use the mod manager [r2modman](https://thunderstore.io/package/ebkr/r2modman/).

## Changelog
1.0.0
- Initial release

1.1.0
- Clients no longer need to install VChat to access the global chat when running on a server-wide instance
- VChat clients can now communicate over the global chat channel even if the server is not running it
- Added an opacity setting for the chat window, added an active and inactive opacity
- Added an option to change the hide timer of the chat window
- Added a fancy fade out effect to the chat window
- Added option to change the default chat channel
- Added a GitHub version checker
- Pings no longer send a blank message to the chat
- Fixed chat input color when spawning
- Fixed pings activating the chat window
