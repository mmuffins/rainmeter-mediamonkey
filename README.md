# Rainmeter-MediaMonkey
A rainmeter plugin to get data from [MediaMonkey](http://www.mediamonkey.com). The plugin was designed with compatibility with the [official NowPlaying plugin](https://docs.rainmeter.net/manual-beta/plugins/nowplaying/) in mind, so most configurations for NowPlaying should be usable with minimal configuration changes.

Currently only MediaMonkey version 4 is supported, support for version 5 is planned once a stable version is available.


## Usage
Copy MediaMonkey.dll in your \Rainmeter\Plugins directory and create a measure with settings Plugin=MediaMonkey.

## Parameters
### Settings
- __PlayerName__ - Reference to the parent of a child measure. Not required for parent measures.
- __MMVersion__ - 4 or 5, indicating what version of mediamonkey is used. Required for parent measures.
- __PlayerPath__ - Path to MediaMonkey.exe. If not provided, the plugin will attempt to auto-locate MediaMonkey.
- __PlayerType__ - Type of the measure that should be returned. See below for valid measures.
- __DisableLeadingZero__ - Removes the leading zero for the measures Duration and Position (i.e. MM:SS becomes M:SS). The default value is 0.


Also see the official [Rainmeter manual](https://docs.rainmeter.net/manual/measures/) for further information on the usage of parameters and measures.

### Measures
- __Album__ - Album name of the current song.
- __Artist__ - Artist of the current song.
- __Cover__ - Path to the cover  of the current song. The image needs to be saved as file (as opposed to files stored in the tags) and be configured as image type 'Cover (front)'.
- __Duration__ - Length of the current song in seconds. If used as string, the format returned is MM:SS.
- __File__ - File path to the current song.
- __Genre__ - Genre of the current song.
- __Number__ - Track number of the current song. This measure should be used as string since the track number in MediaMonkey can also be a string.
- __Position__ - Playback position of the current in seconds. If used as string, the format returned is MM:SS.
- __Progress__ - Playback position of the current song as percentage.
- __Rating__ - Rating of the current song from 0.0 to 5.0. 0.0 is returned for 0 (i.e. the bomb), -1 is returned for an unknown rating.
- __Repeat__ - 0 if repeat is off, 1 if on.
- __Shuffle__ - 0 if shuffle is off, 1 if on.
- __State__ - 0 if stopped, 1 for playing, 2 for paused.
- __Status__ - 0 if MediaMonkey is closed, 1 if it is opened.
- __Title__ - Title of the current song.
- __Volume__ - Player volume from 0 to 100.
- __Year__ - Year of the current song.

### Bangs
- __Play__ - Plays the current song.
- __Pause__ - Pauses the current song.
- __PlayPause__ - Toggles Play and Pause.
- __Stop__ - Stops the current song.
- __Previous__ - Change to the previous song.
- __Next__ - Change to the next song.
- __OpenPlayer__ - Open MediaMonkey.
- __ClosePlayer__ - Quit MediaMonkey.
- __TogglePlayer__ - Open MediaMonkey if it is closed, quit if it is running.
- __SetRating__ - Set the rating of the current song to d where d can be a value between 0.0 and 5.0. Mid-ratings (i.e. 1.5, 3.5) are supported. For uknown rating use value -1.
- __SetPosition__ - Set the progress of the current song to d (e.g. SetPosition 35 to jump to 35%).
- __SetShuffle__ - 1 for shuffle on, 0 for shuffle off, -1 for toggle shuffle.
- __SetRepeat__ - 1 for repeat on, 0 for repeat off, -1 for repeat shuffle.
- __SetVolume__ - Set the volume of the player song to d (e.g. SetPosition 35 to jump to 35%) or a relative value (e.g. SetVolume +20 increases the volume by 20%).


Also see the official [Rainmeter manual](https://docs.rainmeter.net/manual-beta/bangs/#CommandMeasure) for further information on the usage of bangs.

## Example
```ini
[mPlayer]
Measure=Plugin
Plugin=MediaMonkey.dll
MMVersion=4
PlayerPath="C:\Program Files (x86)\MediaMonkey\MediaMonkey.exe"
DisableLeadingZero=0
PlayerType=TITLE
Substitute="":"N\A"

[mAlbum]
Measure=Plugin
Plugin=MediaMonkey.dll
PlayerName=[mPlayer]
PlayerType=ALBUM
Substitute="":"N\A"

[mArtist]
Measure=Plugin
Plugin=MediaMonkey.dll
PlayerName=[mPlayer]
PlayerType=artist
Substitute="":"N\A"

[mCover]
Measure=Plugin
Plugin=MediaMonkey.dll
PlayerName=[mPlayer]
PlayerType=COVER
Substitute="":"#@#Default.png"

[mRating]
Measure=Plugin
Plugin=MediaMonkey.dll
PlayerName=[mPlayer]
PlayerType=RATING

[mVolume]
Measure=Plugin
Plugin=MediaMonkey.dll
PlayerName=[mPlayer]
PlayerType=VOLUME
```