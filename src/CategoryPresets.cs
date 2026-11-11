using System;
using System.Collections.Generic;

namespace NetLimiterParser
{
    internal static class CategoryPresets
    {
        public const int CurrentVersion = 3;

        public static List<CategoryDef> CreateDefaults()
        {
            List<CategoryDef> list = new List<CategoryDef>();

            CategoryDef discord = new CategoryDef("Discord", "#5865F2");
            discord.Rules.Add(Proc("discord.exe"));
            discord.Rules.Add(Proc("discordcanary.exe"));
            discord.Rules.Add(Proc("discordptb.exe"));
            discord.Rules.Add(Proc("discorddevelopment.exe"));
            discord.Rules.Add(Domain("discord.com"));
            discord.Rules.Add(Domain("discord.gg"));
            discord.Rules.Add(Domain("discordapp.com"));
            discord.Rules.Add(Domain("discordapp.net"));
            discord.Rules.Add(Domain("discordcdn.com"));
            discord.Rules.Add(Domain("discordsays.com"));
            list.Add(discord);

            CategoryDef games = new CategoryDef("Игры", "#8B5CF6");
            games.Rules.Add(Proc("steam.exe"));
            games.Rules.Add(Proc("steamwebhelper.exe"));
            games.Rules.Add(Proc("steamservice.exe"));
            games.Rules.Add(Proc("steamerrorreporter.exe"));
            games.Rules.Add(Proc("epicgameslauncher.exe"));
            games.Rules.Add(Proc("epicwebhelper.exe"));
            games.Rules.Add(Proc("riotclientservices.exe"));
            games.Rules.Add(Proc("riotclientux.exe"));
            games.Rules.Add(Proc("valorant.exe"));
            games.Rules.Add(Proc("leagueclient.exe"));
            games.Rules.Add(Proc("league of legends.exe"));
            games.Rules.Add(Proc("battle.net.exe"));
            games.Rules.Add(Proc("minecraft.exe"));
            games.Rules.Add(Proc("minecraftlauncher.exe"));
            games.Rules.Add(Proc("robloxplayerbeta.exe"));
            games.Rules.Add(Proc("robloxplayer.exe"));
            games.Rules.Add(Proc("gta5.exe"));
            games.Rules.Add(Proc("gtav.exe"));
            games.Rules.Add(Proc("playgtav.exe"));
            games.Rules.Add(Proc("rdr2.exe"));
            games.Rules.Add(Proc("tarkov.exe"));
            games.Rules.Add(Proc("escapefromtarkov.exe"));
            games.Rules.Add(Proc("battleye.exe"));
            games.Rules.Add(Proc("pubg.exe"));
            games.Rules.Add(Proc("cs2.exe"));
            games.Rules.Add(Proc("csgo.exe"));
            games.Rules.Add(Proc("dota2.exe"));
            games.Rules.Add(Proc("witcher3.exe"));
            games.Rules.Add(Proc("cyberpunk2077.exe"));
            games.Rules.Add(Proc("eldenring.exe"));
            games.Rules.Add(Proc("fallout4.exe"));
            games.Rules.Add(Proc("skyrimse.exe"));
            games.Rules.Add(Proc("rust.exe"));
            games.Rules.Add(Proc("rustclient.exe"));
            games.Rules.Add(Proc("arkascended.exe"));
            games.Rules.Add(Proc("genshinimpact.exe"));
            games.Rules.Add(Proc("starrail.exe"));
            games.Rules.Add(Proc("zenlesszonezero.exe"));
            games.Rules.Add(Proc("wutheringwaves.exe"));
            games.Rules.Add(Proc("destiny2.exe"));
            games.Rules.Add(Proc("overwatch.exe"));
            games.Rules.Add(Proc("pathofexile.exe"));
            games.Rules.Add(Proc("pathofexile_x64.exe"));
            games.Rules.Add(Proc("warframe.x64.exe"));
            games.Rules.Add(Proc("bf2042.exe"));
            games.Rules.Add(Proc("battlefield2042.exe"));
            games.Rules.Add(Proc("modernwarfare.exe"));
            games.Rules.Add(Proc("warzone.exe"));
            games.Rules.Add(Proc("farcry6.exe"));
            games.Rules.Add(Proc("assassinscreed*.exe"));
            games.Rules.Add(Proc("eadesktop.exe"));
            games.Rules.Add(Proc("ea desktop.exe"));
            games.Rules.Add(Proc("origin.exe"));
            games.Rules.Add(Proc("galaxyclient.exe"));
            games.Rules.Add(Proc("rockstargameslauncher.exe"));
            games.Rules.Add(Proc("gamingservices.exe"));
            games.Rules.Add(Proc("gamingservicesnet.exe"));
            games.Rules.Add(Domain("steampowered.com"));
            games.Rules.Add(Domain("steamcommunity.com"));
            games.Rules.Add(Domain("steamcontent.com"));
            games.Rules.Add(Domain("steamstatic.com"));
            games.Rules.Add(Domain("epicgames.com"));
            games.Rules.Add(Domain("epicgames.dev"));
            games.Rules.Add(Domain("unrealengine.com"));
            games.Rules.Add(Domain("riotgames.com"));
            games.Rules.Add(Domain("leagueoflegends.com"));
            games.Rules.Add(Domain("valorant.com"));
            games.Rules.Add(Domain("blizzard.com"));
            games.Rules.Add(Domain("battle.net"));
            games.Rules.Add(Domain("blizzardcdn.com"));
            games.Rules.Add(Domain("minecraft.net"));
            games.Rules.Add(Domain("mojang.com"));
            games.Rules.Add(Domain("roblox.com"));
            games.Rules.Add(Domain("rbxlabs.com"));
            games.Rules.Add(Domain("escapefromtarkov.com"));
            games.Rules.Add(Domain("battlestate.games"));
            games.Rules.Add(Domain("pubg.com"));
            games.Rules.Add(Domain("dota2.com"));
            games.Rules.Add(Domain("rockstargames.com"));
            games.Rules.Add(Domain("gog.com"));
            games.Rules.Add(Domain("ea.com"));
            games.Rules.Add(Domain("origin.com"));
            games.Rules.Add(Domain("ubisoft.com"));
            games.Rules.Add(Domain("ubisoftconnect.com"));
            games.Rules.Add(Domain("genshinimpact.com"));
            games.Rules.Add(Domain("hoyoverse.com"));
            games.Rules.Add(Domain("mihoyo.com"));
            games.Rules.Add(Domain("bungie.net"));
            games.Rules.Add(Domain("destinythegame.com"));
            games.Rules.Add(Domain("warframe.com"));
            games.Rules.Add(Domain("pathofexile.com"));
            games.Rules.Add(Domain("worldofwarcraft.com"));
            games.Rules.Add(Domain("xboxlive.com"));
            games.Rules.Add(Domain("xbox.com"));
            games.Rules.Add(Port("27015-27050"));
            list.Add(games);

            CategoryDef browsers = new CategoryDef("Браузеры", "#2563EB");
            browsers.Rules.Add(Proc("chrome.exe"));
            browsers.Rules.Add(Proc("msedge.exe"));
            browsers.Rules.Add(Proc("msedgewebview2.exe"));
            browsers.Rules.Add(Proc("firefox.exe"));
            browsers.Rules.Add(Proc("opera.exe"));
            browsers.Rules.Add(Proc("opera_gx.exe"));
            browsers.Rules.Add(Proc("brave.exe"));
            browsers.Rules.Add(Proc("vivaldi.exe"));
            browsers.Rules.Add(Proc("yandex.exe"));
            browsers.Rules.Add(Proc("browser.exe"));
            browsers.Rules.Add(Proc("chromium.exe"));
            browsers.Rules.Add(Proc("thorium.exe"));
            browsers.Rules.Add(Proc("arc.exe"));
            browsers.Rules.Add(Proc("iexplore.exe"));
            browsers.Rules.Add(Proc("waterfox.exe"));
            browsers.Rules.Add(Proc("librewolf.exe"));
            list.Add(browsers);

            CategoryDef messengers = new CategoryDef("Мессенджеры", "#06B6D4");
            messengers.Rules.Add(Proc("telegram.exe"));
            messengers.Rules.Add(Proc("whatsapp.exe"));
            messengers.Rules.Add(Proc("whatsappdesktop.exe"));
            messengers.Rules.Add(Proc("viber.exe"));
            messengers.Rules.Add(Proc("signal.exe"));
            messengers.Rules.Add(Proc("slack.exe"));
            messengers.Rules.Add(Proc("teams.exe"));
            messengers.Rules.Add(Proc("ms-teams.exe"));
            messengers.Rules.Add(Proc("zoom.exe"));
            messengers.Rules.Add(Proc("skype.exe"));
            messengers.Rules.Add(Proc("element.exe"));
            messengers.Rules.Add(Proc("wire.exe"));
            messengers.Rules.Add(Proc("threema.exe"));
            messengers.Rules.Add(Proc("line.exe"));
            messengers.Rules.Add(Proc("wechat.exe"));
            messengers.Rules.Add(Proc("qq.exe"));
            messengers.Rules.Add(Proc("icq.exe"));
            messengers.Rules.Add(Proc("vkteams.exe"));
            messengers.Rules.Add(Domain("telegram.org"));
            messengers.Rules.Add(Domain("telegram.me"));
            messengers.Rules.Add(Domain("t.me"));
            messengers.Rules.Add(Domain("whatsapp.com"));
            messengers.Rules.Add(Domain("whatsapp.net"));
            messengers.Rules.Add(Domain("signal.org"));
            messengers.Rules.Add(Domain("slack.com"));
            messengers.Rules.Add(Domain("slack-edge.com"));
            messengers.Rules.Add(Domain("slack-msgs.com"));
            messengers.Rules.Add(Domain("teams.microsoft.com"));
            messengers.Rules.Add(Domain("zoom.us"));
            messengers.Rules.Add(Domain("skype.com"));
            messengers.Rules.Add(Domain("viber.com"));
            messengers.Rules.Add(Domain("element.io"));
            messengers.Rules.Add(Domain("matrix.org"));
            messengers.Rules.Add(Domain("line.me"));
            messengers.Rules.Add(Domain("wechat.com"));
            messengers.Rules.Add(Domain("icq.com"));
            list.Add(messengers);

            CategoryDef vpn = new CategoryDef("VPN / Прокси", "#0EA5E9");
            vpn.Rules.Add(Proc("warp-svc.exe"));
            vpn.Rules.Add(Proc("warp.exe"));
            vpn.Rules.Add(Proc("wireguard.exe"));
            vpn.Rules.Add(Proc("wg.exe"));
            vpn.Rules.Add(Proc("openvpn.exe"));
            vpn.Rules.Add(Proc("openvpn-gui.exe"));
            vpn.Rules.Add(Proc("nekoray.exe"));
            vpn.Rules.Add(Proc("nekobox.exe"));
            vpn.Rules.Add(Proc("nekobox_core.exe"));
            vpn.Rules.Add(Proc("xray.exe"));
            vpn.Rules.Add(Proc("v2ray.exe"));
            vpn.Rules.Add(Proc("v2raya.exe"));
            vpn.Rules.Add(Proc("clash.exe"));
            vpn.Rules.Add(Proc("clash-verge.exe"));
            vpn.Rules.Add(Proc("clash-verge-service.exe"));
            vpn.Rules.Add(Proc("mihomo.exe"));
            vpn.Rules.Add(Proc("sing-box.exe"));
            vpn.Rules.Add(Proc("hysteria.exe"));
            vpn.Rules.Add(Proc("tuic.exe"));
            vpn.Rules.Add(Proc("shadowsocks.exe"));
            vpn.Rules.Add(Proc("ss-local.exe"));
            vpn.Rules.Add(Proc("ssr.exe"));
            vpn.Rules.Add(Proc("trojan.exe"));
            vpn.Rules.Add(Proc("zerotier-one_x64.exe"));
            vpn.Rules.Add(Proc("zerotier-one.exe"));
            vpn.Rules.Add(Proc("tailscaled.exe"));
            vpn.Rules.Add(Proc("tailscale-ipn.exe"));
            vpn.Rules.Add(Proc("amneziawg.exe"));
            vpn.Rules.Add(Proc("amneziawg-go.exe"));
            vpn.Rules.Add(Proc("amnezia.exe"));
            vpn.Rules.Add(Proc("outline.exe"));
            vpn.Rules.Add(Proc("protonvpn.exe"));
            vpn.Rules.Add(Proc("nordvpn.exe"));
            vpn.Rules.Add(Proc("expressvpn.exe"));
            vpn.Rules.Add(Proc("windscribe.exe"));
            vpn.Rules.Add(Proc("softether.exe"));
            vpn.Rules.Add(Proc("vpnclient.exe"));
            vpn.Rules.Add(Proc("vpnsvc.exe"));
            vpn.Rules.Add(Proc("torguard.exe"));
            vpn.Rules.Add(Proc("mullvad.exe"));
            vpn.Rules.Add(Proc("mullvad-daemon.exe"));
            vpn.Rules.Add(Proc("psiphon3.exe"));
            vpn.Rules.Add(Proc("torbrowser.exe"));
            vpn.Rules.Add(Proc("tor.exe"));
            vpn.Rules.Add(Domain("cloudflareclient.com"));
            vpn.Rules.Add(Domain("tailscale.com"));
            vpn.Rules.Add(Domain("zerotier.com"));
            vpn.Rules.Add(Domain("protonvpn.com"));
            vpn.Rules.Add(Domain("nordvpn.com"));
            vpn.Rules.Add(Domain("expressvpn.com"));
            vpn.Rules.Add(Domain("mullvad.net"));
            vpn.Rules.Add(Domain("windscribe.com"));
            vpn.Rules.Add(Domain("psiphon.ca"));
            vpn.Rules.Add(Domain("torproject.org"));
            vpn.Rules.Add(Ip("162.159.192.0/24"));
            vpn.Rules.Add(Ip("162.159.193.0/24"));
            list.Add(vpn);

            CategoryDef video = new CategoryDef("Видео и стриминг", "#EF4444");
            video.Rules.Add(Proc("spotify.exe"));
            video.Rules.Add(Proc("vlc.exe"));
            video.Rules.Add(Proc("mpv.exe"));
            video.Rules.Add(Proc("potplayermini64.exe"));
            video.Rules.Add(Proc("potplayermini.exe"));
            video.Rules.Add(Proc("wmplayer.exe"));
            video.Rules.Add(Proc("musicbee.exe"));
            video.Rules.Add(Proc("aimp.exe"));
            video.Rules.Add(Proc("tidal.exe"));
            video.Rules.Add(Proc("deezer.exe"));
            video.Rules.Add(Proc("obs64.exe"));
            video.Rules.Add(Proc("obs32.exe"));
            video.Rules.Add(Proc("youtube.exe"));
            video.Rules.Add(Domain("youtube.com"));
            video.Rules.Add(Domain("youtu.be"));
            video.Rules.Add(Domain("ytimg.com"));
            video.Rules.Add(Domain("googlevideo.com"));
            video.Rules.Add(Domain("ytcontent.com"));
            video.Rules.Add(Domain("twitch.tv"));
            video.Rules.Add(Domain("ttvnw.net"));
            video.Rules.Add(Domain("twitchcdn.net"));
            video.Rules.Add(Domain("netflix.com"));
            video.Rules.Add(Domain("nflxvideo.net"));
            video.Rules.Add(Domain("nflximg.net"));
            video.Rules.Add(Domain("hulu.com"));
            video.Rules.Add(Domain("disneyplus.com"));
            video.Rules.Add(Domain("primevideo.com"));
            video.Rules.Add(Domain("aiv-cdn.net"));
            video.Rules.Add(Domain("rutube.ru"));
            video.Rules.Add(Domain("vkvideo.ru"));
            video.Rules.Add(Domain("dailymotion.com"));
            video.Rules.Add(Domain("vimeo.com"));
            video.Rules.Add(Domain("soundcloud.com"));
            video.Rules.Add(Domain("spotify.com"));
            video.Rules.Add(Domain("scdn.co"));
            video.Rules.Add(Domain("tidal.com"));
            video.Rules.Add(Domain("deezer.com"));
            video.Rules.Add(Domain("music.apple.com"));
            video.Rules.Add(Domain("music.yandex.ru"));
            video.Rules.Add(Domain("zvuk.com"));
            video.Rules.Add(Domain("boom.ru"));
            list.Add(video);

            CategoryDef social = new CategoryDef("Соцсети", "#EC4899");
            social.Rules.Add(Domain("vk.com"));
            social.Rules.Add(Domain("vkontakte.ru"));
            social.Rules.Add(Domain("ok.ru"));
            social.Rules.Add(Domain("odnoklassniki.ru"));
            social.Rules.Add(Domain("facebook.com"));
            social.Rules.Add(Domain("fbcdn.net"));
            social.Rules.Add(Domain("fb.com"));
            social.Rules.Add(Domain("instagram.com"));
            social.Rules.Add(Domain("cdninstagram.com"));
            social.Rules.Add(Domain("x.com"));
            social.Rules.Add(Domain("twitter.com"));
            social.Rules.Add(Domain("twimg.com"));
            social.Rules.Add(Domain("reddit.com"));
            social.Rules.Add(Domain("redd.it"));
            social.Rules.Add(Domain("redditmedia.com"));
            social.Rules.Add(Domain("redditstatic.com"));
            social.Rules.Add(Domain("tumblr.com"));
            social.Rules.Add(Domain("pinterest.com"));
            social.Rules.Add(Domain("linkedin.com"));
            social.Rules.Add(Domain("snapchat.com"));
            social.Rules.Add(Domain("threads.net"));
            social.Rules.Add(Domain("mastodon.social"));
            social.Rules.Add(Domain("bsky.app"));
            social.Rules.Add(Domain("bsky.social"));
            social.Rules.Add(Domain("tiktok.com"));
            social.Rules.Add(Domain("tiktokcdn.com"));
            social.Rules.Add(Domain("byteoversea.com"));
            social.Rules.Add(Domain("muscdn.com"));
            social.Rules.Add(Domain("douyin.com"));
            social.Rules.Add(Domain("weibo.com"));
            list.Add(social);

            CategoryDef cloud = new CategoryDef("Облака и файлы", "#0D9488");
            cloud.Rules.Add(Proc("onedrive.exe"));
            cloud.Rules.Add(Proc("dropbox.exe"));
            cloud.Rules.Add(Proc("dropboxupdate.exe"));
            cloud.Rules.Add(Proc("googledrivefs.exe"));
            cloud.Rules.Add(Proc("backup and sync.exe"));
            cloud.Rules.Add(Proc("megasync.exe"));
            cloud.Rules.Add(Proc("mega.exe"));
            cloud.Rules.Add(Proc("yandexdisk.exe"));
            cloud.Rules.Add(Proc("disk-o.exe"));
            cloud.Rules.Add(Proc("nextcloud.exe"));
            cloud.Rules.Add(Proc("owncloud.exe"));
            cloud.Rules.Add(Proc("syncthing.exe"));
            cloud.Rules.Add(Proc("rclone.exe"));
            cloud.Rules.Add(Proc("seafile.exe"));
            cloud.Rules.Add(Proc("pcloud.exe"));
            cloud.Rules.Add(Proc("icedrive.exe"));
            cloud.Rules.Add(Proc("filezilla.exe"));
            cloud.Rules.Add(Proc("winscp.exe"));
            cloud.Rules.Add(Proc("totalcmd.exe"));
            cloud.Rules.Add(Domain("dropbox.com"));
            cloud.Rules.Add(Domain("dropboxusercontent.com"));
            cloud.Rules.Add(Domain("dropboxstatic.com"));
            cloud.Rules.Add(Domain("onedrive.live.com"));
            cloud.Rules.Add(Domain("onedrive.com"));
            cloud.Rules.Add(Domain("sharepoint.com"));
            cloud.Rules.Add(Domain("mega.nz"));
            cloud.Rules.Add(Domain("mega.io"));
            cloud.Rules.Add(Domain("yadi.sk"));
            cloud.Rules.Add(Domain("disk.yandex.ru"));
            cloud.Rules.Add(Domain("disk.yandex.com"));
            cloud.Rules.Add(Domain("cloud.mail.ru"));
            cloud.Rules.Add(Domain("nextcloud.com"));
            cloud.Rules.Add(Domain("owncloud.com"));
            cloud.Rules.Add(Domain("pcloud.com"));
            cloud.Rules.Add(Domain("sync.com"));
            cloud.Rules.Add(Domain("box.com"));
            cloud.Rules.Add(Domain("1drv.ms"));
            cloud.Rules.Add(Domain("drive.google.com"));
            list.Add(cloud);

            CategoryDef torrents = new CategoryDef("Торренты", "#F59E0B");
            torrents.Rules.Add(Proc("transmission-qt.exe"));
            torrents.Rules.Add(Proc("transmission.exe"));
            torrents.Rules.Add(Proc("transmission-daemon.exe"));
            torrents.Rules.Add(Proc("qbittorrent.exe"));
            torrents.Rules.Add(Proc("utorrent.exe"));
            torrents.Rules.Add(Proc("bittorrent.exe"));
            torrents.Rules.Add(Proc("deluge.exe"));
            torrents.Rules.Add(Proc("deluged.exe"));
            torrents.Rules.Add(Proc("vuze.exe"));
            torrents.Rules.Add(Proc("tixati.exe"));
            torrents.Rules.Add(Proc("aria2c.exe"));
            torrents.Rules.Add(Proc("amule.exe"));
            torrents.Rules.Add(Proc("emule.exe"));
            torrents.Rules.Add(Proc("frostwire.exe"));
            torrents.Rules.Add(Proc("webtorrent.exe"));
            torrents.Rules.Add(Proc("mediaget.exe"));
            torrents.Rules.Add(Port("6881-6999"));
            torrents.Rules.Add(Port("51413"));
            list.Add(torrents);

            CategoryDef remote = new CategoryDef("Удалённый доступ", "#7C3AED");
            remote.Rules.Add(Proc("anydesk.exe"));
            remote.Rules.Add(Proc("teamviewer.exe"));
            remote.Rules.Add(Proc("teamviewer_service.exe"));
            remote.Rules.Add(Proc("tv_w32.exe"));
            remote.Rules.Add(Proc("tv_x64.exe"));
            remote.Rules.Add(Proc("rustdesk.exe"));
            remote.Rules.Add(Proc("rustdesk*.exe"));
            remote.Rules.Add(Proc("parsec.exe"));
            remote.Rules.Add(Proc("moonlight.exe"));
            remote.Rules.Add(Proc("sunshine.exe"));
            remote.Rules.Add(Proc("rdpclip.exe"));
            remote.Rules.Add(Proc("mstsc.exe"));
            remote.Rules.Add(Proc("vncviewer.exe"));
            remote.Rules.Add(Proc("winvnc4.exe"));
            remote.Rules.Add(Proc("vncserver.exe"));
            remote.Rules.Add(Proc("tightvnc.exe"));
            remote.Rules.Add(Proc("ultraviewer.exe"));
            remote.Rules.Add(Proc("supremo.exe"));
            remote.Rules.Add(Proc("dwagent.exe"));
            remote.Rules.Add(Proc("sragentsvc.exe"));
            remote.Rules.Add(Proc("splashtop.exe"));
            remote.Rules.Add(Proc("logmein.exe"));
            remote.Rules.Add(Proc("gotomypc.exe"));
            remote.Rules.Add(Proc("radmin.exe"));
            remote.Rules.Add(Domain("anydesk.com"));
            remote.Rules.Add(Domain("teamviewer.com"));
            remote.Rules.Add(Domain("rustdesk.com"));
            remote.Rules.Add(Domain("parsec.app"));
            remote.Rules.Add(Domain("parsecgaming.com"));
            remote.Rules.Add(Domain("moonlight-stream.org"));
            remote.Rules.Add(Domain("logmein.com"));
            remote.Rules.Add(Domain("gotomypc.com"));
            remote.Rules.Add(Domain("splashtop.com"));
            remote.Rules.Add(Domain("radmin.com"));
            remote.Rules.Add(Domain("dwservice.net"));
            remote.Rules.Add(Port("tcp:3389"));
            remote.Rules.Add(Port("5900-5905"));
            remote.Rules.Add(Port("21115-21119"));
            list.Add(remote);

            CategoryDef ai = new CategoryDef("ИИ-сервисы", "#14B8A6");
            ai.Rules.Add(Proc("ollama.exe"));
            ai.Rules.Add(Proc("ollama_llama_server.exe"));
            ai.Rules.Add(Proc("lm studio.exe"));
            ai.Rules.Add(Proc("lmstudio.exe"));
            ai.Rules.Add(Proc("koboldcpp.exe"));
            ai.Rules.Add(Proc("jan.exe"));
            ai.Rules.Add(Proc("gpt4all.exe"));
            ai.Rules.Add(Proc("llamafile.exe"));
            ai.Rules.Add(Proc("chatbox.exe"));
            ai.Rules.Add(Proc("msty.exe"));
            ai.Rules.Add(Proc("claude.exe"));
            ai.Rules.Add(Proc("chatgpt.exe"));
            ai.Rules.Add(Proc("codex.exe"));
            ai.Rules.Add(Proc("opencode.exe"));
            ai.Rules.Add(Proc("opencode-cli.exe"));
            ai.Rules.Add(Proc("cursor.exe"));
            ai.Rules.Add(Domain("openai.com"));
            ai.Rules.Add(Domain("chatgpt.com"));
            ai.Rules.Add(Domain("oaistatic.com"));
            ai.Rules.Add(Domain("oaiusercontent.com"));
            ai.Rules.Add(Domain("anthropic.com"));
            ai.Rules.Add(Domain("claude.ai"));
            ai.Rules.Add(Domain("claudeusercontent.com"));
            ai.Rules.Add(Domain("gemini.google.com"));
            ai.Rules.Add(Domain("generativelanguage.googleapis.com"));
            ai.Rules.Add(Domain("bard.google.com"));
            ai.Rules.Add(Domain("x.ai"));
            ai.Rules.Add(Domain("grok.com"));
            ai.Rules.Add(Domain("perplexity.ai"));
            ai.Rules.Add(Domain("copilot.microsoft.com"));
            ai.Rules.Add(Domain("githubcopilot.com"));
            ai.Rules.Add(Domain("huggingface.co"));
            ai.Rules.Add(Domain("hf.co"));
            ai.Rules.Add(Domain("replicate.com"));
            ai.Rules.Add(Domain("together.xyz"));
            ai.Rules.Add(Domain("fireworks.ai"));
            ai.Rules.Add(Domain("groq.com"));
            ai.Rules.Add(Domain("mistral.ai"));
            ai.Rules.Add(Domain("deepseek.com"));
            ai.Rules.Add(Domain("poe.com"));
            ai.Rules.Add(Domain("character.ai"));
            ai.Rules.Add(Domain("midjourney.com"));
            ai.Rules.Add(Domain("stability.ai"));
            ai.Rules.Add(Domain("ideogram.ai"));
            ai.Rules.Add(Domain("runwayml.com"));
            ai.Rules.Add(Domain("elevenlabs.io"));
            ai.Rules.Add(Domain("suno.com"));
            ai.Rules.Add(Domain("udio.com"));
            ai.Rules.Add(Domain("openrouter.ai"));
            ai.Rules.Add(Domain("ollama.com"));
            ai.Rules.Add(Domain("lmstudio.ai"));
            list.Add(ai);

            CategoryDef dev = new CategoryDef("Разработка", "#64748B");
            dev.Rules.Add(Proc("code.exe"));
            dev.Rules.Add(Proc("code - insiders.exe"));
            dev.Rules.Add(Proc("devenv.exe"));
            dev.Rules.Add(Proc("idea64.exe"));
            dev.Rules.Add(Proc("idea.exe"));
            dev.Rules.Add(Proc("pycharm64.exe"));
            dev.Rules.Add(Proc("webstorm64.exe"));
            dev.Rules.Add(Proc("clion64.exe"));
            dev.Rules.Add(Proc("rider64.exe"));
            dev.Rules.Add(Proc("goland64.exe"));
            dev.Rules.Add(Proc("studio64.exe"));
            dev.Rules.Add(Proc("eclipse.exe"));
            dev.Rules.Add(Proc("netbeans.exe"));
            dev.Rules.Add(Proc("sublime_text.exe"));
            dev.Rules.Add(Proc("notepad++.exe"));
            dev.Rules.Add(Proc("atom.exe"));
            dev.Rules.Add(Proc("git.exe"));
            dev.Rules.Add(Proc("git-remote-https.exe"));
            dev.Rules.Add(Proc("githubdesktop.exe"));
            dev.Rules.Add(Proc("github desktop.exe"));
            dev.Rules.Add(Proc("sourcetree.exe"));
            dev.Rules.Add(Proc("fork.exe"));
            dev.Rules.Add(Proc("docker desktop.exe"));
            dev.Rules.Add(Proc("docker.exe"));
            dev.Rules.Add(Proc("com.docker.backend.exe"));
            dev.Rules.Add(Proc("dockerd.exe"));
            dev.Rules.Add(Proc("wsl.exe"));
            dev.Rules.Add(Proc("wslservice.exe"));
            dev.Rules.Add(Proc("node.exe"));
            dev.Rules.Add(Proc("npm.exe"));
            dev.Rules.Add(Proc("npx.exe"));
            dev.Rules.Add(Proc("yarn.exe"));
            dev.Rules.Add(Proc("pnpm.exe"));
            dev.Rules.Add(Proc("python.exe"));
            dev.Rules.Add(Proc("pythonw.exe"));
            dev.Rules.Add(Proc("pip.exe"));
            dev.Rules.Add(Proc("dotnet.exe"));
            dev.Rules.Add(Proc("msbuild.exe"));
            dev.Rules.Add(Proc("nuget.exe"));
            dev.Rules.Add(Proc("java.exe"));
            dev.Rules.Add(Proc("javaw.exe"));
            dev.Rules.Add(Proc("javac.exe"));
            dev.Rules.Add(Proc("mvn.exe"));
            dev.Rules.Add(Proc("gradle.exe"));
            dev.Rules.Add(Proc("cargo.exe"));
            dev.Rules.Add(Proc("rustc.exe"));
            dev.Rules.Add(Proc("go.exe"));
            dev.Rules.Add(Proc("gcc.exe"));
            dev.Rules.Add(Proc("cmake.exe"));
            dev.Rules.Add(Proc("ninja.exe"));
            dev.Rules.Add(Proc("make.exe"));
            dev.Rules.Add(Proc("mingw32-make.exe"));
            dev.Rules.Add(Proc("jupyter-notebook.exe"));
            dev.Rules.Add(Proc("jupyter.exe"));
            dev.Rules.Add(Domain("github.com"));
            dev.Rules.Add(Domain("githubusercontent.com"));
            dev.Rules.Add(Domain("githubassets.com"));
            dev.Rules.Add(Domain("gitlab.com"));
            dev.Rules.Add(Domain("bitbucket.org"));
            dev.Rules.Add(Domain("stackoverflow.com"));
            dev.Rules.Add(Domain("stackexchange.com"));
            dev.Rules.Add(Domain("npmjs.com"));
            dev.Rules.Add(Domain("npmjs.org"));
            dev.Rules.Add(Domain("pypi.org"));
            dev.Rules.Add(Domain("python.org"));
            dev.Rules.Add(Domain("nuget.org"));
            dev.Rules.Add(Domain("crates.io"));
            dev.Rules.Add(Domain("rust-lang.org"));
            dev.Rules.Add(Domain("golang.org"));
            dev.Rules.Add(Domain("go.dev"));
            dev.Rules.Add(Domain("nodejs.org"));
            dev.Rules.Add(Domain("docker.com"));
            dev.Rules.Add(Domain("docker.io"));
            dev.Rules.Add(Domain("jetbrains.com"));
            dev.Rules.Add(Domain("visualstudio.com"));
            dev.Rules.Add(Domain("visualstudio.microsoft.com"));
            dev.Rules.Add(Domain("dev.azure.com"));
            dev.Rules.Add(Domain("maven.org"));
            dev.Rules.Add(Domain("gradle.org"));
            dev.Rules.Add(Domain("readthedocs.io"));
            dev.Rules.Add(Domain("developer.mozilla.org"));
            list.Add(dev);

            CategoryDef updates = new CategoryDef("Обновления", "#A3A3A3");
            updates.Rules.Add(Proc("tiworker.exe"));
            updates.Rules.Add(Proc("mousocoreworker.exe"));
            updates.Rules.Add(Proc("usoclient.exe"));
            updates.Rules.Add(Proc("wuauclt.exe"));
            updates.Rules.Add(Proc("wuauclt1.exe"));
            updates.Rules.Add(Proc("googleupdate.exe"));
            updates.Rules.Add(Proc("googleupdateonmachine.exe"));
            updates.Rules.Add(Proc("microsoftedgeupdate.exe"));
            updates.Rules.Add(Proc("microsoftedgeupdater.exe"));
            updates.Rules.Add(Proc("armouryupdater.exe"));
            updates.Rules.Add(Proc("ccxprocess.exe"));
            updates.Rules.Add(Proc("adobeupdater.exe"));
            updates.Rules.Add(Proc("updater.exe"));
            updates.Rules.Add(Domain("windowsupdate.com"));
            updates.Rules.Add(Domain("update.microsoft.com"));
            updates.Rules.Add(Domain("delivery.mp.microsoft.com"));
            updates.Rules.Add(Domain("definitionupdates.microsoft.com"));
            updates.Rules.Add(Domain("download.windowsupdate.com"));
            updates.Rules.Add(Domain("catalog.update.microsoft.com"));
            updates.Rules.Add(Domain("edgeupdates.microsoft.com"));
            updates.Rules.Add(Domain("msedge.api.cdp.microsoft.com"));
            updates.Rules.Add(Domain("googleupdate.com"));
            updates.Rules.Add(Domain("update.googleapis.com"));
            updates.Rules.Add(Domain("gvt1.com"));
            updates.Rules.Add(Domain("gvt2.com"));
            updates.Rules.Add(Domain("dl.google.com"));
            list.Add(updates);

            CategoryDef system = new CategoryDef("Система", "#475569");
            system.Rules.Add(Proc("system"));
            system.Rules.Add(Proc("svchost.exe"));
            system.Rules.Add(Proc("lsass.exe"));
            system.Rules.Add(Proc("services.exe"));
            system.Rules.Add(Proc("wininit.exe"));
            system.Rules.Add(Proc("smss.exe"));
            system.Rules.Add(Proc("csrss.exe"));
            system.Rules.Add(Proc("winlogon.exe"));
            system.Rules.Add(Proc("fontdrvhost.exe"));
            system.Rules.Add(Proc("dwm.exe"));
            system.Rules.Add(Proc("explorer.exe"));
            system.Rules.Add(Proc("taskhostw.exe"));
            system.Rules.Add(Proc("sihost.exe"));
            system.Rules.Add(Proc("ctfmon.exe"));
            system.Rules.Add(Proc("spoolsv.exe"));
            system.Rules.Add(Proc("audiodg.exe"));
            system.Rules.Add(Proc("searchapp.exe"));
            system.Rules.Add(Proc("searchhost.exe"));
            system.Rules.Add(Proc("startmenuexperiencehost.exe"));
            system.Rules.Add(Proc("shellexperiencehost.exe"));
            system.Rules.Add(Proc("runtimebroker.exe"));
            system.Rules.Add(Proc("backgroundtaskhost.exe"));
            system.Rules.Add(Proc("wmiprvse.exe"));
            system.Rules.Add(Proc("msmpeng.exe"));
            system.Rules.Add(Proc("nissrv.exe"));
            system.Rules.Add(Proc("securityhealthservice.exe"));
            system.Rules.Add(Proc("mpcmdrun.exe"));
            system.Rules.Add(Domain("msftconnecttest.com"));
            system.Rules.Add(Domain("msftncsi.com"));
            list.Add(system);

            CategoryDef lan = new CategoryDef("Локальная сеть", "#84CC16");
            lan.Rules.Add(Domain("localhost"));
            lan.Rules.Add(Ip("10.0.0.0/8"));
            lan.Rules.Add(Ip("172.16.0.0/12"));
            lan.Rules.Add(Ip("192.168.0.0/16"));
            lan.Rules.Add(Ip("127.0.0.0/8"));
            lan.Rules.Add(Ip("169.254.0.0/16"));
            lan.Rules.Add(Ip("224.0.0.0/4"));
            lan.Rules.Add(Ip("255.255.255.255/32"));
            lan.Rules.Add(Ip("fc00::/7"));
            lan.Rules.Add(Ip("fe80::/10"));
            lan.Rules.Add(Ip("::1/128"));
            lan.Rules.Add(Ip("ff00::/8"));
            list.Add(lan);

            AddExpandedRules(list);

            list.Add(new CategoryDef("Другое", "#94A3B8"));

            return list;
        }

        /// <summary>
        /// Дополнения версии 2: добавляются и в новые установки, и в существующий
        /// categories.json. Метод идемпотентен, поэтому его можно вызывать повторно.
        /// </summary>
        public static void AddExpandedRules(List<CategoryDef> list)
        {
            if (list == null) return;

            CategoryDef discord = FindCategory(list, "Discord");
            if (discord != null)
            {
                AddProc(discord, "discordhookhelper*.exe");
            }

            CategoryDef games = FindCategory(list, "Игры");
            if (games != null)
            {
                AddProc(games, "hd-player.exe");
                AddProc(games, "hd-multiinstancemanager.exe");
                AddProc(games, "bluestacks*.exe");
                AddProc(games, "gamebar.exe");
                AddDomain(games, "bluestacks.com");
            }

            CategoryDef browsers = FindCategory(list, "Браузеры");
            if (browsers != null)
            {
                AddProc(browsers, "browser_assistant.exe");
                AddProc(browsers, "zen.exe");
                AddProc(browsers, "floorp.exe");
                AddProc(browsers, "sidekick.exe");
            }

            CategoryDef messengers = FindCategory(list, "Мессенджеры");
            if (messengers != null)
            {
                AddProc(messengers, "mattermost.exe");
                AddProc(messengers, "rocketchat.exe");
                AddProc(messengers, "zulip.exe");
                AddProc(messengers, "webex.exe");
                AddProc(messengers, "webexmta.exe");
                AddProc(messengers, "gotomeeting.exe");
                AddProc(messengers, "jitsi.exe");
                AddDomain(messengers, "mattermost.com");
                AddDomain(messengers, "rocket.chat");
                AddDomain(messengers, "zulip.org");
                AddDomain(messengers, "webex.com");
                AddDomain(messengers, "gotomeeting.com");
                AddDomain(messengers, "jitsi.org");
            }

            CategoryDef vpn = FindCategory(list, "VPN / Прокси");
            if (vpn != null)
            {
                AddProc(vpn, "amnezia*.exe");
                AddProc(vpn, "wireguardservice.exe");
                AddProc(vpn, "windscribe*.exe");
                AddProc(vpn, "cloudflare warp.exe");
                AddProc(vpn, "warp-dex.exe");
                AddProc(vpn, "v2rayn.exe");
                AddProc(vpn, "v2ray-local.exe");
                AddProc(vpn, "shadowsocks-net-local.exe");
                AddProc(vpn, "flclash*.exe");
                AddProc(vpn, "clash-verge*.exe");
                AddProc(vpn, "clash-meta.exe");
                AddProc(vpn, "clash-nyanpasu.exe");
                AddProc(vpn, "tunnelbear*.exe");
                AddProc(vpn, "clearvpn.exe");
                AddProc(vpn, "setupclearvpn.exe");
                AddProc(vpn, "opera-proxy*.exe");
                AddProc(vpn, "proxyserver.exe");
                AddProc(vpn, "pac-server.exe");
                AddProc(vpn, "ccproxy.exe");
                AddProc(vpn, "dnscrypt-proxy.exe");
                AddProc(vpn, "simplednscrypt.exe");
                AddProc(vpn, "wiresock*.exe");
                AddProc(vpn, "koala clash.exe");
                AddProc(vpn, "koala-clash.exe");
                AddProc(vpn, "zapret.exe");
                AddProc(vpn, "ag_dns.exe");
                AddProc(vpn, "camo-proxy.exe");
                AddProc(vpn, "yogadns.exe");
                AddProc(vpn, "tun2socks.exe");
                AddProc(vpn, "zerotier_desktop_ui.exe");
                AddProc(vpn, "hiddify*.exe");
                AddProc(vpn, "protonvpn*.exe");
                AddProc(vpn, "mullvad*.exe");
                AddProc(vpn, "koala-mihomo.exe");
                AddProc(vpn, "mihomo*.exe");
                AddDomain(vpn, "amnezia.org");
                AddDomain(vpn, "v2ray.com");
                AddDomain(vpn, "v2fly.org");
                AddDomain(vpn, "hiddify.com");
                AddDomain(vpn, "flclash.com");
                AddDomain(vpn, "dnscrypt.info");
                AddDomain(vpn, "yogadns.com");
            }

            CategoryDef video = FindCategory(list, "Видео и стриминг");
            if (video != null)
            {
                AddProc(video, "ffplay.exe");
                AddProc(video, "wmpnetwk.exe");
                AddProc(video, "audio-player.exe");
                AddProc(video, "keenetic-audio-player*.exe");
                AddProc(video, "radiokeenetik.exe");
            }

            CategoryDef cloud = FindCategory(list, "Облака и файлы");
            if (cloud != null)
            {
                AddProc(cloud, "snapdlbin.exe");
                AddProc(cloud, "snapdownloader.exe");
                AddProc(cloud, "byclickdownloader*.exe");
                AddProc(cloud, "yt-dlp.exe");
                AddProc(cloud, "youtube-dl.exe");
                AddProc(cloud, "httpget.exe");
                AddProc(cloud, "idman.exe");
                AddProc(cloud, "internetdownloadmanager.exe");
                AddProc(cloud, "jdownloader*.exe");
                AddProc(cloud, "eagleget.exe");
                AddProc(cloud, "freedownloadmanager.exe");
                AddProc(cloud, "xdm.exe");
                AddProc(cloud, "mipony.exe");
                AddProc(cloud, "freeytvdownloader.exe");
                AddProc(cloud, "freeyoutubedownload*.exe");
                AddProc(cloud, "freeyoutubeconverter*.exe");
                AddDomain(cloud, "snapdownloader.com");
                AddDomain(cloud, "byclickdownloader.com");
                AddDomain(cloud, "jdownloader.org");
                AddDomain(cloud, "internetdownloadmanager.com");
            }

            CategoryDef remote = FindCategory(list, "Удалённый доступ");
            if (remote != null)
            {
                AddProc(remote, "nomachine.exe");
                AddProc(remote, "nxplayer.exe");
                AddProc(remote, "nxserver.exe");
                AddProc(remote, "mremoteng.exe");
                AddProc(remote, "rdm.exe");
                AddProc(remote, "remoteutilities.exe");
                AddProc(remote, "rutserv.exe");
                AddProc(remote, "rutview.exe");
                AddProc(remote, "screenconnect.client.exe");
                AddProc(remote, "connectwisecontrol.client.exe");
                AddProc(remote, "ateraagent.exe");
                AddDomain(remote, "nomachine.com");
                AddDomain(remote, "mremoteng.org");
                AddDomain(remote, "remoteutilities.com");
                AddDomain(remote, "screenconnect.com");
                AddDomain(remote, "connectwise.com");
                AddDomain(remote, "atera.com");
            }

            CategoryDef ai = FindCategory(list, "ИИ-сервисы");
            if (ai != null)
            {
                AddProc(ai, "kimi*.exe");
                AddProc(ai, "bionic.exe");
                AddProc(ai, "ollama app.exe");
                AddProc(ai, "lms.exe");
                AddProc(ai, "llama-server.exe");
                AddProc(ai, "llama-cli.exe");
                AddProc(ai, "claude*.exe");
                AddProc(ai, "cowork-svc.exe");
                AddProc(ai, "chatgpt*.exe");
                AddProc(ai, "codex*.exe");
                AddProc(ai, "antigravity*.exe");
                AddProc(ai, "language_server_windows_x64.exe");
                AddProc(ai, "*\\programs\\antigravity\\resources\\bin\\language_server.exe");
                AddProc(ai, "kilo.exe");
                AddProc(ai, "copilot-runtime.exe");
                AddProc(ai, "*\\openai\\codex\\runtimes\\cua_node\\*\\bin\\node_repl.exe");
                AddProc(ai, "*\\openai\\codex\\bin\\*\\node_repl.exe");
                AddProc(ai, "*\\gpt4all\\bin\\chat.exe");
                AddProc(ai, "windsurf.exe");
                AddProc(ai, "trae.exe");
                AddProc(ai, "perplexity.exe");
                AddProc(ai, "comet.exe");
                AddProc(ai, "copilot.exe");
                AddProc(ai, "anythingllm.exe");
                AddProc(ai, "open-webui.exe");
                AddDomain(ai, "kimi.com");
                AddDomain(ai, "kimi.ai");
                AddDomain(ai, "moonshot.cn");
                AddDomain(ai, "moonshot.ai");
                AddDomain(ai, "antigravity.google");
                AddDomain(ai, "kilocode.ai");
                AddDomain(ai, "windsurf.com");
                AddDomain(ai, "codeium.com");
                AddDomain(ai, "qwen.ai");
                AddDomain(ai, "chatglm.cn");
                AddDomain(ai, "zhipuai.cn");
                AddDomain(ai, "cohere.com");
                AddDomain(ai, "ai21.com");
                AddDomain(ai, "together.ai");
                AddDomain(ai, "anyscale.com");
            }

            CategoryDef dev = FindCategory(list, "Разработка");
            if (dev != null)
            {
                AddProc(dev, "reqable.exe");
                AddProc(dev, "fiddler.exe");
                AddProc(dev, "charles.exe");
                AddProc(dev, "postman.exe");
                AddProc(dev, "insomnia.exe");
                AddProc(dev, "bruno.exe");
                AddProc(dev, "db browser for sqlite.exe");
                AddProc(dev, "db browser for sqlcipher.exe");
                AddProc(dev, "heidisql.exe");
                AddProc(dev, "dbeaver.exe");
                AddProc(dev, "arduino ide.exe");
                AddProc(dev, "arduino-cli.exe");
                AddProc(dev, "arduino-language-server.exe");
                AddProc(dev, "arduino-fwuploader.exe");
                AddProc(dev, "mdns-discovery.exe");
                AddProc(dev, "jinx.exe");
                AddProc(dev, "apk-editor-studio.exe");
                AddProc(dev, "adb.exe");
                AddProc(dev, "fastboot.exe");
                AddProc(dev, "cloudcode_cli.exe");
                AddProc(dev, "uv.exe");
                AddProc(dev, "git-credential-manager.exe");
                AddProc(dev, "cef_server.exe");
                AddProc(dev, "vsce-sign.exe");
                AddProc(dev, "pacman.exe");
                AddProc(dev, "zed.exe");
                AddProc(dev, "rider*.exe");
                AddProc(dev, "adbappcontrol.exe");
                AddDomain(dev, "reqable.com");
                AddDomain(dev, "fiddler.com");
                AddDomain(dev, "charlesproxy.com");
                AddDomain(dev, "postman.com");
                AddDomain(dev, "insomnia.rest");
                AddDomain(dev, "dbeaver.io");
                AddDomain(dev, "sqlitebrowser.org");
                AddDomain(dev, "heidisql.com");
                AddDomain(dev, "arduino.cc");
                AddDomain(dev, "astral.sh");
                AddDomain(dev, "zed.dev");
                AddDomain(dev, "cloud.google.com");
            }

            CategoryDef updates = FindCategory(list, "Обновления");
            if (updates != null)
            {
                AddProc(updates, "opera_autoupdate.exe");
                AddProc(updates, "winget.exe");
                AddProc(updates, "windowspackagemanagerserver.exe");
                AddProc(updates, "sihclient.exe");
                AddProc(updates, "ruximics.exe");
                AddProc(updates, "logi_download_assistant.exe");
                AddProc(updates, "updaterv2.exe");
            }

            CategoryDef system = FindCategory(list, "Система");
            if (system != null)
            {
                AddProc(system, "smartscreen.exe");
                AddProc(system, "compattelrunner.exe");
                AddProc(system, "devicecensus.exe");
                AddProc(system, "dashost.exe");
                AddProc(system, "dllhost.exe");
                AddProc(system, "wermgr.exe");
                AddProc(system, "werfault.exe");
                AddProc(system, "sppextcomobj.exe");
                AddProc(system, "sdiagnhost.exe");
                AddProc(system, "clipesuconsumer.exe");
                AddProc(system, "apphostregistrationverifier.exe");
                AddProc(system, "systemsettings.exe");
                AddProc(system, "wwahost.exe");
                AddProc(system, "lockapp.exe");
                AddProc(system, "winstore.app.exe");
                AddProc(system, "storedesktopextension.exe");
                AddProc(system, "phoneexperiencehost.exe");
                AddProc(system, "photos.exe");
                AddProc(system, "3dviewer.exe");
                AddProc(system, "msiexec.exe");
                AddProc(system, "regasm.exe");
                AddProc(system, "pingsender.exe");
                AddProc(system, "nearby_share.exe");
                AddProc(system, "dirmngr.exe");
                AddProc(system, "gpg.exe");
                AddProc(system, "gpg-agent.exe");
                AddProc(system, "gpg-connect-agent.exe");
                AddProc(system, "printui.exe");
                AddProc(system, "asuscertservice.exe");
                AddProc(system, "fnplicensingservice.exe");
                AddProc(system, "radeonsoftware.exe");
                AddProc(system, "amdbugreportform.exe");
                AddProc(system, "nlsvc.exe");
                AddProc(system, "nlclientapp.exe");
                AddProc(system, "forward");
                AddProc(system, "procexp64.exe");
                AddProc(system, "autoruns64.exe");
                AddProc(system, "recuva64.exe");
                AddProc(system, "recoverit.exe");
                AddProc(system, "recoverit_installer.exe");
                AddProc(system, "puran file recovery.exe");
                AddProc(system, "partassist.exe");
                AddProc(system, "hdd regenerator.exe");
                AddProc(system, "ccleaner.exe");
                AddProc(system, "ccleaner64.exe");
                AddProc(system, "7zfm.exe");
                AddProc(system, "winrar.exe");
                AddProc(system, "bandizip.exe");
                AddProc(system, "bcuninstaller.exe");
                AddProc(system, "revouninstaller.exe");
                AddProc(system, "crystaldiskinfo.exe");
                AddProc(system, "crystaldiskmark.exe");
                AddProc(system, "hwinfo64.exe");
                AddProc(system, "gpu-z.exe");
                AddProc(system, "cpuz.exe");
                AddProc(system, "aida64.exe");
                AddProc(system, "powershell.exe");
                AddProc(system, "powershell_ise.exe");
                AddProc(system, "pwsh.exe");
                AddProc(system, "cmd.exe");
                AddProc(system, "wscript.exe");
                AddProc(system, "cscript.exe");
                AddProc(system, "mshta.exe");
                AddProc(system, "mpdefendercoreservice.exe");
                AddProc(system, "msmpeng.exe");
                AddProc(system, "mpcmdrun.exe");
                AddProc(system, "nissrv.exe");
                AddProc(system, "mpsigstub.exe");
                AddProc(system, "securityhealthservice.exe");
                AddProc(system, "securityhealthsystray.exe");
                AddProc(system, "drengsrv.exe");
                AddProc(system, "drss.exe");
                AddProc(system, "microsoft.msn.weather.exe");
            }

            CategoryDef office = EnsureCategoryBefore(list, "Офис и документы", "#D97706", "Обновления");
            AddProc(office, "winword.exe");
            AddProc(office, "excel.exe");
            AddProc(office, "powerpnt.exe");
            AddProc(office, "outlook.exe");
            AddProc(office, "onenote.exe");
            AddProc(office, "onenotem.exe");
            AddProc(office, "msaccess.exe");
            AddProc(office, "mspub.exe");
            AddProc(office, "visio.exe");
            AddProc(office, "olk.exe");
            AddProc(office, "xpdagent.exe");
            AddProc(office, "msoia.exe");
            AddProc(office, "sdxhelper.exe");
            AddProc(office, "officeclicktorun.exe");
            AddProc(office, "officec2rclient.exe");
            AddProc(office, "m365copilot.exe");
            AddProc(office, "libreoffice.exe");
            AddProc(office, "soffice.exe");
            AddProc(office, "soffice.bin");
            AddProc(office, "wps.exe");
            AddProc(office, "et.exe");
            AddProc(office, "wpp.exe");
            AddProc(office, "acrobat.exe");
            AddProc(office, "acrord32.exe");
            AddProc(office, "foxitreader.exe");
            AddProc(office, "sumatrapdf.exe");
            AddDomain(office, "office.com");
            AddDomain(office, "office365.com");
            AddDomain(office, "microsoft365.com");
            AddDomain(office, "microsoftoffice.com");
            AddDomain(office, "officeapps.live.com");
            AddDomain(office, "outlook.com");
            AddDomain(office, "outlook.office.com");
            AddDomain(office, "outlook.office365.com");

            CategoryDef vm = EnsureCategoryBefore(list, "Виртуальные машины", "#0891B2", "Система");
            AddProc(vm, "virtualbox*.exe");
            AddProc(vm, "vbox*.exe");
            AddProc(vm, "vmware*.exe");
            AddProc(vm, "vmplayer.exe");
            AddProc(vm, "qemu*.exe");
            AddProc(vm, "prl_*.exe");
            AddProc(vm, "parallels*.exe");
            AddProc(vm, "vmcompute.exe");
            AddProc(vm, "vmms.exe");
            AddProc(vm, "vmwp.exe");
            AddProc(vm, "virtualbox-*-win.exe");
            AddDomain(vm, "virtualbox.org");
            AddDomain(vm, "vmware.com");
            AddDomain(vm, "parallels.com");
            AddDomain(vm, "qemu.org");

            CategoryDef network = EnsureCategoryBefore(list, "Сеть и диагностика", "#10B981", "Система");
            AddProc(network, "wireshark.exe");
            AddProc(network, "dumpcap.exe");
            AddProc(network, "tshark.exe");
            AddProc(network, "nmap.exe");
            AddProc(network, "ncat.exe");
            AddProc(network, "netcat.exe");
            AddProc(network, "putty.exe");
            AddProc(network, "plink.exe");
            AddProc(network, "kitty.exe");
            AddProc(network, "ssh.exe");
            AddProc(network, "telnet.exe");
            AddProc(network, "nslookup.exe");
            AddProc(network, "ping.exe");
            AddProc(network, "tracert.exe");
            AddProc(network, "pathping.exe");
            AddProc(network, "winbox.exe");
            AddProc(network, "winbox64.exe");
            AddProc(network, "tftpd64.exe");
            AddProc(network, "tftp.exe");
            AddProc(network, "bvssh.exe");
            AddProc(network, "mtr.exe");
            AddProc(network, "fing.exe");
            AddProc(network, "advanced_ip_scanner.exe");
            AddProc(network, "angryipscanner.exe");
            AddProc(network, "inssider.exe");
            AddProc(network, "netstumbler.exe");
            AddProc(network, "curl.exe");
            AddProc(network, "curl-kyber.exe");
            AddProc(network, "dnsbench*.exe");
            AddProc(network, "wbr_installer.exe");
            AddDomain(network, "wireshark.org");
            AddDomain(network, "nmap.org");
            AddDomain(network, "putty.org");
            AddDomain(network, "mikrotik.com");
            AddDomain(network, "bitvise.com");
            AddDomain(network, "tftpd64.com");
        }

        private static CategoryDef FindCategory(List<CategoryDef> list, string name)
        {
            for (int i = 0; i < list.Count; i++)
            {
                CategoryDef category = list[i];
                if (category != null && string.Equals(category.Name, name, StringComparison.OrdinalIgnoreCase))
                    return category;
            }
            return null;
        }

        private static CategoryDef EnsureCategoryBefore(List<CategoryDef> list, string name, string color, string anchorName)
        {
            CategoryDef existing = FindCategory(list, name);
            if (existing != null) return existing;

            CategoryDef created = new CategoryDef(name, color);
            int index = -1;

            for (int i = 0; i < list.Count; i++)
            {
                CategoryDef category = list[i];
                if (category != null && string.Equals(category.Name, anchorName, StringComparison.OrdinalIgnoreCase))
                {
                    index = i;
                    break;
                }
            }

            if (index < 0)
            {
                for (int i = 0; i < list.Count; i++)
                {
                    CategoryDef category = list[i];
                    if (category != null && string.Equals(category.Name, "Другое", StringComparison.OrdinalIgnoreCase))
                    {
                        index = i;
                        break;
                    }
                }
            }

            if (index >= 0) list.Insert(index, created);
            else list.Add(created);
            return created;
        }

        private static bool HasRule(CategoryDef category, string type, string pattern)
        {
            foreach (CategoryRule rule in category.Rules)
            {
                if (rule != null &&
                    string.Equals(rule.Type, type, StringComparison.OrdinalIgnoreCase) &&
                    string.Equals(rule.Pattern, pattern, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        private static void AddProc(CategoryDef category, string pattern)
        {
            if (!HasRule(category, "process", pattern)) category.Rules.Add(Proc(pattern));
        }

        private static void AddDomain(CategoryDef category, string pattern)
        {
            if (!HasRule(category, "domain", pattern)) category.Rules.Add(Domain(pattern));
        }

        private static CategoryRule Proc(string pattern)
        {
            return new CategoryRule("process", pattern, "");
        }

        private static CategoryRule Domain(string pattern)
        {
            return new CategoryRule("domain", pattern, "");
        }

        private static CategoryRule Ip(string pattern)
        {
            return new CategoryRule("ip", pattern, "");
        }

        private static CategoryRule Port(string pattern)
        {
            return new CategoryRule("port", pattern, "");
        }
    }
}
