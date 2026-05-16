using ConsoleTables;
using SteamKit2;
using PluginLocale = FriendsManager.Localization;
using ArchiSteamFarm.Localization;
using ArchiSteamFarm.Steam;
using Interaction = ArchiSteamFarm.Steam.Interaction;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using System;
using System.ComponentModel;
using ArchiSteamFarm.Core;
using System.Linq;
using ArchiSteamFarm;

namespace FriendsManager;

internal static class Commands {
	private static readonly string[] TableHeader = ["SteamID", "Profile Name"];
	internal static async Task<string?> OnBotCommand(Bot bot, EAccess access, string[] args, ulong steamID = 0) {
		ArgumentNullException.ThrowIfNull(bot);

		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		if ((args == null) || (args.Length == 0)) {
			throw new ArgumentNullException(nameof(args));
		}

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		switch (args.Length) {
			case 1:
				switch (args[0].ToUpperInvariant()) {
					case "FR" or "FRIENDS":
						return ResponseFriends(access, bot);
					case "SINV" or "SENTINVITES":
						return ResponseSentInvites(access, bot);
					case "RINV" or "RECEIVEDINVITES":
						return ResponseReceivedInvites(access, bot);
					case "FMVERSION" or "FMV":
						return ResponseVersion(access);
					case "RMAFR" or "REMOVEALLFRIENDS":
						return await ResponseRemoveAllFriends(access, bot).ConfigureAwait(false);
					case "AAINV" or "ACCEPTALLINVITES":
						return await ResponseAcceptAllInvites(access, bot).ConfigureAwait(false);
					case "DAINV" or "DECLINEALLINVITES":
						return await ResponseDeclineAllInvites(access, bot).ConfigureAwait(false);
				}

				break;

			default:
				switch (args[0].ToUpperInvariant()) {
					case "FRIENDS" or "FR":
						return await ResponseFriends(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "SENTINVITES" or "SINV":
						return await ResponseSentInvites(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "RECEIVEDINVITES" or "RINV":
						return await ResponseReceivedInvites(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "SFINV" or "SENDINVITE" when args.Length > 2:
						return await ResponseSendInvite(access, args[1], Utilities.GetArgsAsText(args, 2, ","), steamID).ConfigureAwait(false);
					case "SFINV" or "SENDINVITE":
						return await ResponseSendInvite(access, bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
					case "RMFR" or "REMOVEFRIEND" when args.Length > 2:
						return await ResponseRemoveFriend(access, args[1], Utilities.GetArgsAsText(args, 2, ","), steamID).ConfigureAwait(false);
					case "RMFR" or "REMOVEFRIEND":
						return await ResponseRemoveFriend(access, bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
					case "RMAFR" or "REMOVEALLFRIENDS":
						return await ResponseRemoveAllFriends(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "AINV" or "ACCEPTINVITE" when args.Length > 2:
						return await ResponseAcceptInvite(access, args[1], Utilities.GetArgsAsText(args, 2, ","), steamID).ConfigureAwait(false);
					case "AINV" or "ACCEPTINVITE":
						return await ResponseAcceptInvite(access, bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
					case "DINV" or "DECLINEINVITE" when args.Length > 2:
						return await ResponseDeclineInvite(access, args[1], Utilities.GetArgsAsText(args, 2, ","), steamID).ConfigureAwait(false);
					case "DINV" or "DECLINEINVITE":
						return await ResponseDeclineInvite(access, bot, Utilities.GetArgsAsText(args, 1, ",")).ConfigureAwait(false);
					case "AAINV" or "ACCEPTALLINVITES":
						return await ResponseAcceptAllInvites(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
					case "DAINV" or "DECLINEALLINVITES":
						return await ResponseDeclineAllInvites(access, Utilities.GetArgsAsText(args, 1, ","), steamID).ConfigureAwait(false);
				}

				break;
		}

		return null;
	}
	private static IEnumerable<SteamID> GetRawSteamID(string target) {
		if (ulong.TryParse(target, out ulong rawID)) {
			yield return (SteamID) rawID;
		}
	}
	private static HashSet<SteamID> ResolveTargetsToSteamIDs(string targetsText) {
		string[] targets = targetsText.Split(SharedInfo.ListElementSeparators, StringSplitOptions.RemoveEmptyEntries);

		HashSet<SteamID> steamIDs = [];

		foreach (string target in targets) {
			HashSet<Bot>? bots = Bot.GetBots(target);

			IEnumerable<SteamID> candidates = bots?.Count > 0 ?
				bots.Select(static bot => (SteamID) bot.SteamID) : GetRawSteamID(target);

			foreach (SteamID candidate in candidates) {
				if (candidate.IsValid && candidate.IsIndividualAccount) {
					_ = steamIDs.Add(candidate);
				}
			}
		}

		return steamIDs;
	}
	private static HashSet<SteamID> GetFriends(SteamFriends steamFriends, EFriendRelationship relationship = EFriendRelationship.Friend) =>
		[.. Enumerable.Range(0, steamFriends.GetFriendCount())
			.Select(i => steamFriends.GetFriendByIndex(i))
			.Where(friendID => steamFriends.GetFriendRelationship(friendID) == relationship)];

	private static string? ResponseFriends(EAccess access, Bot bot) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		SteamFriends steamFriends = bot.SteamFriends;

		HashSet<SteamID> friends = GetFriends(steamFriends);

		if (friends.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(friends)));
		}

		ConsoleTable friendsTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		foreach (SteamID friend in friends) {
			_ = friendsTable.AddRow(friend.ConvertToUInt64(), steamFriends.GetFriendPersonaName(friend) ?? string.Empty);
		}

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotFriends(friends.Count), friendsTable);

		return bot.Commands.FormatBotResponse(result);
	}
	private static async Task<string?> ResponseFriends(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseFriends(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static string? ResponseSentInvites(EAccess access, Bot bot) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		SteamFriends steamFriends = bot.SteamFriends;

		HashSet<SteamID> sentInvites = GetFriends(steamFriends, EFriendRelationship.RequestInitiator);

		if (sentInvites.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(sentInvites)));
		}

		ConsoleTable sentInvitesTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		foreach (SteamID steamID in sentInvites) {
			_ = sentInvitesTable.AddRow(steamID.ConvertToUInt64(), steamFriends.GetFriendPersonaName(steamID) ?? string.Empty);
		}

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotSentInvites(sentInvites.Count), sentInvitesTable);

		return bot.Commands.FormatBotResponse(result);
	}
	private static async Task<string?> ResponseSentInvites(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseSentInvites(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static string? ResponseReceivedInvites(EAccess access, Bot bot) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		SteamFriends steamFriends = bot.SteamFriends;

		HashSet<SteamID> receivedInvites = GetFriends(steamFriends, EFriendRelationship.RequestRecipient);

		if (receivedInvites.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(receivedInvites)));
		}

		ConsoleTable receivedInvitesTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		foreach (SteamID steamID in receivedInvites) {
			_ = receivedInvitesTable.AddRow(steamID.ConvertToUInt64(), steamFriends.GetFriendPersonaName(steamID) ?? string.Empty);
		}

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotReceivedInvites(receivedInvites.Count), receivedInvitesTable);

		return bot.Commands.FormatBotResponse(result);
	}
	private static async Task<string?> ResponseReceivedInvites(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => Task.Run(() => ResponseReceivedInvites(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot)))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static async Task<string?> ResponseSendInvite(EAccess access, Bot bot, string targetsText) {
		ArgumentException.ThrowIfNullOrEmpty(targetsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		if (bot.IsAccountLimited) {
			return bot.Commands.FormatBotResponse(PluginLocale.Strings.BotAccountLimited);
		}

		HashSet<SteamID> steamIDs = ResolveTargetsToSteamIDs(targetsText);

		if (steamIDs.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(steamIDs)));
		}

		ushort successCount = 0;

		foreach (ulong steamID in steamIDs) {
			bot.ArchiLogger.LogGenericInfo($"steam id: {steamID}");
			if (await bot.ArchiHandler.AddFriend(steamID).ConfigureAwait(false)) {
				successCount++;
			}
		}

		return bot.Commands.FormatBotResponse(PluginLocale.Strings.FormatBotAddedFriends(successCount, steamIDs.Count));
	}
	private static async Task<string?> ResponseSendInvite(EAccess access, string botNames, string targetsText, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseSendInvite(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, targetsText))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static async Task<string?> ResponseRemoveFriend(EAccess access, Bot bot, string targetsText) {
		ArgumentException.ThrowIfNullOrEmpty(targetsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		HashSet<SteamID> steamIDs = ResolveTargetsToSteamIDs(targetsText);

		if (steamIDs.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(steamIDs)));
		}

		SteamFriends steamFriends = bot.SteamFriends;

		HashSet<SteamID> friendsToRemove = [.. GetFriends(steamFriends).Where(steamIDs.Contains)];

		ushort successCount = 0;

		foreach (ulong friend in friendsToRemove) {
			if (await bot.ArchiHandler.RemoveFriend(friend).ConfigureAwait(false)) {
				successCount++;
			}
		}

		ConsoleTable removedFriendsTable = new ConsoleTable(TableHeader).Configure(o => o.EnableCount = false);

		foreach (SteamID friend in friendsToRemove) {
			_ = removedFriendsTable.AddRow(friend.ConvertToUInt64(), steamFriends.GetFriendPersonaName(friend) ?? string.Empty);
		}

		string result = string.Join(Environment.NewLine, PluginLocale.Strings.FormatBotRemovedFriends(successCount, friendsToRemove.Count), removedFriendsTable);

		return bot.Commands.FormatBotResponse(result);
	}
	private static async Task<string?> ResponseRemoveFriend(EAccess access, string botNames, string targetsText, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseRemoveFriend(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, targetsText))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static async Task<string?> ResponseRemoveAllFriends(EAccess access, Bot bot) {
		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		HashSet<SteamID> friends = GetFriends(bot.SteamFriends);

		if (friends.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(friends)));
		}

		ushort successCount = 0;

		foreach (ulong friend in friends) {
			if (await bot.ArchiHandler.RemoveFriend(friend).ConfigureAwait(false)) {
				successCount++;
			}
		}

		return bot.Commands.FormatBotResponse(PluginLocale.Strings.FormatBotRemovedFriends(successCount, friends.Count));
	}
	private static async Task<string?> ResponseRemoveAllFriends(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseRemoveAllFriends(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static async Task<string?> ResponseAcceptInvite(EAccess access, Bot bot, string targetsText) {
		ArgumentException.ThrowIfNullOrEmpty(targetsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		HashSet<SteamID> steamIDs = ResolveTargetsToSteamIDs(targetsText);

		if (steamIDs.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(steamIDs)));
		}

		SteamFriends steamFriends = bot.SteamFriends;

		HashSet<SteamID> receivedInvites = [.. steamIDs.Where(steamID => steamFriends.GetFriendRelationship(steamID) == EFriendRelationship.RequestRecipient)];

		ushort successCount = 0;

		foreach (ulong steamID in receivedInvites) {
			if (await bot.ArchiHandler.AddFriend(steamID).ConfigureAwait(false)) {
				successCount++;
			}
		}

		return bot.Commands.FormatBotResponse(PluginLocale.Strings.FormatBotAcceptedInvites(successCount, steamIDs.Count));
	}
	private static async Task<string?> ResponseAcceptInvite(EAccess access, string botNames, string targetsText, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAcceptInvite(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, targetsText))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}

	private static async Task<string?> ResponseDeclineInvite(EAccess access, Bot bot, string targetsText) {
		ArgumentException.ThrowIfNullOrEmpty(targetsText);

		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		HashSet<SteamID> steamIDs = ResolveTargetsToSteamIDs(targetsText);

		if (steamIDs.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsInvalid, nameof(steamIDs)));
		}

		SteamFriends steamFriends = bot.SteamFriends;

		HashSet<SteamID> pendingInvites = [.. steamIDs.Where(steamID => steamFriends.GetFriendRelationship(steamID) == EFriendRelationship.RequestRecipient)];

		ushort successCount = 0;

		foreach (ulong steamID in pendingInvites) {
			if (await bot.ArchiHandler.RemoveFriend(steamID).ConfigureAwait(false)) {
				successCount++;
			}
		}

		return bot.Commands.FormatBotResponse(PluginLocale.Strings.FormatBotDeclinedInvites(successCount, steamIDs.Count));
	}

	private static async Task<string?> ResponseDeclineInvite(EAccess access, string botNames, string targetsText, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseDeclineInvite(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot, targetsText))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static async Task<string?> ResponseAcceptAllInvites(EAccess access, Bot bot) {
		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		HashSet<SteamID> receivedInvites = GetFriends(bot.SteamFriends, EFriendRelationship.RequestRecipient);

		if (receivedInvites.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(receivedInvites)));
		}

		ushort successCount = 0;

		foreach (ulong steamID in receivedInvites) {
			if (await bot.ArchiHandler.AddFriend(steamID).ConfigureAwait(false)) {
				successCount++;
			}
		}

		return bot.Commands.FormatBotResponse(PluginLocale.Strings.FormatBotAcceptedInvites(successCount, receivedInvites.Count));
	}
	private static async Task<string?> ResponseAcceptAllInvites(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseAcceptAllInvites(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static async Task<string?> ResponseDeclineAllInvites(EAccess access, Bot bot) {
		if (access < EAccess.Master) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		if (!bot.IsConnectedAndLoggedOn) {
			return bot.Commands.FormatBotResponse(Strings.BotNotConnected);
		}

		HashSet<SteamID> receivedInvites = GetFriends(bot.SteamFriends, EFriendRelationship.RequestRecipient);

		if (receivedInvites.Count == 0) {
			return bot.Commands.FormatBotResponse(string.Format(CultureInfo.CurrentCulture, Strings.ErrorIsEmpty, nameof(receivedInvites)));
		}

		ushort successCount = 0;

		foreach (ulong steamID in receivedInvites) {
			if (await bot.ArchiHandler.RemoveFriend(steamID).ConfigureAwait(false)) {
				successCount++;
			}
		}

		return bot.Commands.FormatBotResponse(PluginLocale.Strings.FormatBotDeclinedInvites(successCount, receivedInvites.Count));
	}
	private static async Task<string?> ResponseDeclineAllInvites(EAccess access, string botNames, ulong steamID = 0) {
		if (!Enum.IsDefined(access)) {
			throw new InvalidEnumArgumentException(nameof(access), (int) access, typeof(EAccess));
		}

		ArgumentException.ThrowIfNullOrEmpty(botNames);

		if ((steamID != 0) && !new SteamID(steamID).IsIndividualAccount) {
			throw new ArgumentOutOfRangeException(nameof(steamID));
		}

		HashSet<Bot>? bots = Bot.GetBots(botNames);

		if ((bots == null) || (bots.Count == 0)) {
			return access >= EAccess.Master ? Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotNotFound, botNames)) : null;
		}

		IList<string?> results = await Utilities.InParallel(bots.Select(bot => ResponseDeclineAllInvites(Interaction.Commands.GetProxyAccess(bot, access, steamID), bot))).ConfigureAwait(false);

		List<string> responses = [.. results.Where(static result => !string.IsNullOrEmpty(result)).Select(static result => result!)];

		return responses.Count > 0 ? string.Join(Environment.NewLine, responses) : null;
	}
	private static string? ResponseVersion(EAccess access) {
		if (access < EAccess.FamilySharing) {
			return access > EAccess.None ? Interaction.Commands.FormatStaticResponse(Strings.ErrorAccessDenied) : null;
		}

		return Interaction.Commands.FormatStaticResponse(string.Format(CultureInfo.CurrentCulture, Strings.BotVersion, nameof(FriendsManagerPlugin), typeof(FriendsManagerPlugin).Assembly.GetName().Version));
	}
}
