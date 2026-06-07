## Installation
- Download latest `FriendsManager.zip` archive from [release page](https://github.com/dm1tz/FriendsManager/releases/latest).
- Extract archive contents into ASF `plugins` directory.

## Commands
Command | Alias | Access | Description
--- | ---| --- | ---
`friends [Bots]` | `fr` | `FamilySharing` | Prints friends list of given bot instances.
`sentinvites [Bots]` | `sinv` | `FamilySharing` | Prints sent friend invites of given bot instances.
`receivedinvites [Bots]` | `rinv` | `FamilySharing` | Prints received friend invites of given bot instances.
`sendinvite [Bots] <Targets>` | `sfinv` | `Master` | Sends a friend invite from given bot instances. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`removefriend [Bots] <Targets>` | `rmfr` | `Master` | Removes friend from given bot instances friends list. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`removeallfriends [Bots]` | `rmafr` | `Master` | Removes all friends from given bot instances friends list.
`acceptinvite [Bots] <Targets>` | `ainv` | `Master` | Accepts received friend invites from given targets. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`acceptallinvites [Bots]` | `aainv` | `Master` | Accepts all pending received friend invites for given bot instances.
`declineinvite [Bots] <Targets>` | `dinv` | `Master` | Declines received friend invites from given targets. Each target is resolved as a bot name first, falling back to a raw SteamID64.
`declineallinvites [Bots]` | `dainv` | `Master` | Declines all pending received friend invites for given bot instances.
`fmversion` | `fmv` | `FamilySharing` | Prints plugin version.
