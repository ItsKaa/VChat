## VChat
A chat improvement mod for Valheim that aims to add quality-of-life features to Valheim, this mod has features for both servers and clients and is packed in a single .dll file.

With 2.0.0 introduced, VChat now has the ability to host custom server-wide channels, these channels can be customised by the players, this feature adds a varieity of uses, examples would be clans, factions, parties, temporary one-on-one chats, and probably more! Please note that for now these channels are only supported for dedicated servers. The server-sided binary data is stored next to the .db world file, as 'worldName.vchat.bin'.

## Features
- Adds custom server-hosted channels that are accessible from both vanilla clients and clients with VChat installed, these channels can be created by players and are fully customizable, these channels are private by default and players will have to invite others into them.
- Adds a new global chat channel that doesn't ping players.
- All channels are accessible by clients that do not have this mod installed, as long as the server has VChat. Note that VChat does add features to the channels added by the VChat server, so it is advisable to install this mod on every client, but optional.
- Colours! colours for every chat channel (whisper, local, shout, global & custom) and also colours the input field, even when typing channel commands like `/shout [text]`.
- Sent message history that can be called using arrow up and down when the chat is focused.
- Easily configurable using ingame commands that can be also be adjusted in the configuration file.
- Removes the annoying auto caps and lowercases for the shout and whisper channels.
- Able to modify the opacity, width and height of the chat window.
- Various other settings to improve the ingame chat.

## For Developers
To use the custom channels in your own mod, the simplest way is to add VChat as a dependency to your mod.
Then create a channel using the `ServerChannelManager` class after the event `VChatPlugin.OnInitialised` is triggered.  
When the channel is created, you can use `ServerChannelManager.SendMessageToAllPeersInChannel` to send a message and `ServerChannelManager.OnCustomChannelMessageReceived` to read messages from the custom channels.  
  
Alternatively, you can also use network messages to handle channels, feel free to explore the data structures for that.

## Commands:
```
/s /l /say /local  
/y /sh /yell /shout  
/w /whisper  
/g /global [text]
/[customChannelName] [text]
```
Switches to the provided chat channel, and if text is entered, also send a message to that channel.
- Examples:
  - "/g Hello VChat" will send a message to the global chat channel and set it as the active channel for your next message.
  - "/sh" will switch the currently active chat channel to shout.
  - "/test [text]" will send a message to the server-hosted custom channel named "Test", if it exists.

```
/addchannel [name]
```
Adds a channel to the server with the provided name, channel names must be unique.

```
/disband [channel]
```
Disbands a channel with the name, provided you have the permission to do so - either being the owner of the channel or an administrator in-game.

```
/invite [channel] [player]
```
Invites an online player to the channel, if you have the permission - having access to the channel means you can invite.

```
/remove [channel] [player]
```
Remove a player from a channel.
The owner of the channel can also be removed by an administrator.

```
/setcolor [customChannelName] [value]
```
Changes the color of a custom channel with the provided color, this accepts either a html string like #ff0000 or a name, like 'red'.

```
/setlocalcolor /setshoutcolor /setwhispercolor /setglobalcolor [value]
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

```
/setwidth [value]
```
Changes the width of the chat window, this is based of 1920x1080 values regardless of the screen resolution.
The default width is 500.

```
/setheight [value]
```
Changes the height of the chat window, this is based of 1920x1080 values regardless of the screen resolution.
The default height is 400.

```
/setbuffersize [value]
```
Changes the maximum amount of visible messages in the chat window, if this is set to 15 the function will resume as normal.

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

1.2.0
- Global chat now works on non-dedicated servers
- Added setting to change the width and height of the chat window
- Added setting to change the maximum amount of visible messages
- Added support for other server-sided mods to read global messages (dedicated-servers only)

1.2.1
- Fixed the input size when changing the width of the chat-box.

2.0.0
- Added custom server-hosted channels and added a few new commands to manage these.
- Moved configuration file to VChat.cfg
- Added NexusID to the configuration
