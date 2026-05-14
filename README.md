## Installation
- Download the latest `FriendsManager.zip` archive from the [release page](https://github.com/dm1tz/FriendsManager/releases/latest).
- Extract archive contents into the ASF `plugins` directory.

## Commands
Command | Alias | Access | Description
--- | ---| --- | ---
`friends [Bots]` | `fr` | `FamilySharing`| Prints friends list of the given bot instances.
`sentinvites [Bots]` | `sinv` | `FamilySharing`| Prints sent friend invites of the given bot instances.
`receivedinvites [Bots]` | `rinv` | `FamilySharing`| Prints received friend invites of the given bot instances.
`addfriend [Bots] <Targets>` | `adfr` | `Master`| Sends a friend invite from the given bot instances. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`removefriend [Bots] <Targets>` | `rmfr` | `Master`| Removes friend from the given bot instances friends list. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`removeallfriends [Bots]` | `rmafr` | `Master`| Removes all friends from the given bot instances friends list.
`acceptinvite [Bots] <Targets>` | `ainv` | `Master`| Accepts received friend invites from the given targets. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`declineinvite [Bots] <Targets>` | `dinv` | `Master`| Declines received friend invites from the given targets. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`fmversion` | `fmv` | `FamilySharing` | Prints the actual version of plugin.
