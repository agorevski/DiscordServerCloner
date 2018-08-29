# Discord Server Cloner

## Descript 

The title basically says it all.

* Locates chat messages coming from the 'from' server (based simply off of the channel name)
    * Sends them in the format **{username}: {message}**

* Has an additional parameter to clone all of the channels that it finds (based off of channel name)

## Setup

Change the settings in appsettings.json

* From/To discord servers
* Set the flag to clone channels first

## Known Issues - Room for Improvement

* Does not clone categories
* Does not properly clone 'Voice Channels' - will try to clone them on every attempt
* Does not 'delete' Channels that are no longer in existence
* Will clone channels you do not actually have access to
* Does not support inlined images and some other basic formats
* #channels and @username don't clone perfectly

# Future Workitems

* Clone historical data
* Fix the bugs above