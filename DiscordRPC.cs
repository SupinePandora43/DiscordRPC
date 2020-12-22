using GmodNET.API;
using System;
using System.Linq;
using System.Text;
using System.Threading;

namespace DiscordRPC
{
    public class DiscordRPC : IModule
    {
        public string ModuleName => "DiscordRPC";

        public string ModuleVersion => "0.1.3";

        Discord.Discord discord;
        private static readonly long client_ID = long.Parse("715425421574799400");
        bool disposed = false;
        Thread updater;
        bool stopThread = false;


        String map = "map";
        String gamemode = "gamemode";
        String activityDetails = "Details";
        void UpdateActivity()
        {
            var activityManager = discord.GetActivityManager();
            var lobbyManager = discord.GetLobbyManager();

            var activity = new Discord.Activity
            {
                Type = Discord.ActivityType.Playing,
                State = "Playing",
                Details = activityDetails,
                /*Timestamps =
            {
                Start = 5,
                End = 6,
            },*/
                Assets =
            {
                LargeImage = "gmod_logo",
                LargeText = map,
                SmallImage = "gmod_logo",
                SmallText = gamemode.First().ToString().ToUpper() + gamemode.Substring(1),
            },
                Instance = true,
            };

            activityManager.UpdateActivity(activity, result =>
            {
                Console.WriteLine("Update Activity {0}", result);
            });
        }
        public void UpdaterThread()
        {
            try
            {
                while (true)
                {
                    if (!disposed)
                    {
                        discord.RunCallbacks();
                        Thread.Sleep(1000 / 60);
                    }
                    if (stopThread)
                    {
                        break;
                    }
                }
            }
            finally
            {
                if (!disposed)
                {
                    discord.Dispose();
                    disposed = true;
                }
            }
        }

        public void Load(ILua lua, bool is_serverside, ModuleAssemblyLoadContext assembly_context)
        {
            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.GetField(-1, "print");
            lua.PushString("Hello World RPC");
            lua.MCall(1, 0);
            lua.Pop();

            int DiscordRPC_update_callbacks(ILua lua)
            {
                lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                lua.GetField(-1, "print");
                lua.PushString("It Works!");
                lua.MCall(1, 0);
                lua.Pop();

                lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                lua.GetField(-1, "game");
                lua.GetField(-1, "GetMap");
                lua.MCall(0, 1);
                map = lua.GetString(-1);
                lua.Pop();

                lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                lua.GetField(-1, "game");
                lua.GetField(-1, "SinglePlayer");
                lua.MCall(0, 1);
                bool IsSinglePlayer = lua.GetBool(-1);
                lua.Pop();

                lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                lua.GetField(-1, "engine");
                lua.GetField(-1, "ActiveGamemode");
                lua.MCall(0, 1);
                gamemode = lua.GetString(-1);
                lua.Pop();

                if (IsSinglePlayer)
                {
                    activityDetails = "SinglePlayer";
                }
                else
                {
                    lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                    lua.GetField(-1, "player");
                    lua.GetField(-1, "GetCount");
                    lua.MCall(0, 1);
                    int players = (int)lua.GetNumber(-1);
                    lua.Pop();

                    lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                    lua.GetField(-1, "game");
                    lua.GetField(-1, "MaxPlayers");
                    lua.MCall(0, 1);
                    int maxPlayers = (int)lua.GetNumber(-1);
                    lua.Pop();

                    activityDetails = $"{players}/{maxPlayers}";
                }

                /*if (!is_serverside)
                {
                    lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                    lua.GetField(-1, "LocalPlayer"); //team.GetScore(ply:Team())
                    lua.MCall(0, 1);

                    lua.GetField(-1, "Team");
                    lua.Push(-2);
                    lua.MCall(0, 1);
                    double team = lua.GetNumber(-1);
                    lua.Pop();

                    lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
                    lua.GetField(-1, "teams");
                    lua.GetField(-1, "GetScore");
                    lua.PushNumber(team);
                    lua.MCall(1, 1);
                    score = (int)lua.GetNumber(-1);
                    lua.Pop();
                }*/


                UpdateActivity();
                return 0;
            };

            lua.PushSpecial(SPECIAL_TABLES.SPECIAL_GLOB);
            lua.PushManagedFunction(DiscordRPC_update_callbacks);
            lua.SetField(-2, "DiscordRPC_update_activity");
            lua.Pop(1);


            discord = new Discord.Discord(client_ID, (UInt64)Discord.CreateFlags.Default);
            discord.SetLogHook(Discord.LogLevel.Debug, (level, message) =>
            {
                Console.WriteLine("Log[{0}] {1}", level, message);
            });

            var applicationManager = discord.GetApplicationManager();
            // Get the current locale. This can be used to determine what text or audio the user wants.
            Console.WriteLine("Current Locale: {0}", applicationManager.GetCurrentLocale());
            // Get the current branch. For example alpha or beta.
            Console.WriteLine("Current Branch: {0}", applicationManager.GetCurrentBranch());
            // If you want to verify information from your game's server then you can
            // grab the access token and send it to your server.
            //
            // This automatically looks for an environment variable passed by the Discord client,
            // if it does not exist the Discord client will focus itself for manual authorization.
            //
            // By-default the SDK grants the identify and rpc scopes.
            // Read more at https://discordapp.com/developers/docs/topics/oauth2
            // applicationManager.GetOAuth2Token((Discord.Result result, ref Discord.OAuth2Token oauth2Token) =>
            // {
            //     Console.WriteLine("Access Token {0}", oauth2Token.AccessToken);
            // });

            var activityManager = discord.GetActivityManager();
            var lobbyManager = discord.GetLobbyManager();
            // Received when someone accepts a request to join or invite.
            // Use secrets to receive back the information needed to add the user to the group/party/match
            activityManager.OnActivityJoin += secret =>
            {
                Console.WriteLine("OnJoin {0}", secret);
                lobbyManager.ConnectLobbyWithActivitySecret(secret, (Discord.Result result, ref Discord.Lobby lobby) =>
                {
                    Console.WriteLine("Connected to lobby: {0}", lobby.Id);
                    lobbyManager.ConnectNetwork(lobby.Id);
                    lobbyManager.OpenNetworkChannel(lobby.Id, 0, true);
                    foreach (var user in lobbyManager.GetMemberUsers(lobby.Id))
                    {
                        lobbyManager.SendNetworkMessage(lobby.Id, user.Id, 0,
                            Encoding.UTF8.GetBytes(String.Format("Hello, {0}!", user.Username)));
                    }
                    UpdateActivity();
                });
            };
            // Received when someone accepts a request to spectate
            activityManager.OnActivitySpectate += secret =>
            {
                Console.WriteLine("OnSpectate {0}", secret);
            };
            // A join request has been received. Render the request on the UI.
            activityManager.OnActivityJoinRequest += (ref Discord.User user) =>
            {
                Console.WriteLine("OnJoinRequest {0} {1}", user.Id, user.Username);
            };
            // An invite has been received. Consider rendering the user / activity on the UI.
            activityManager.OnActivityInvite += (Discord.ActivityActionType Type, ref Discord.User user, ref Discord.Activity activity2) =>
            {
                Console.WriteLine("OnInvite {0} {1} {2}", Type, user.Username, activity2.Name);
                // activityManager.AcceptInvite(user.Id, result =>
                // {
                //     Console.WriteLine("AcceptInvite {0}", result);
                // });
            };
            // This is used to register the game in the registry such that Discord can find it.
            // This is only needed by games acquired from other platforms, like Steam.
            // activityManager.RegisterCommand();

            var imageManager = discord.GetImageManager();

            var userManager = discord.GetUserManager();


            lobbyManager.OnLobbyMessage += (lobbyID, userID, data) =>
            {
                Console.WriteLine("lobby message: {0} {1}", lobbyID, Encoding.UTF8.GetString(data));
            };
            lobbyManager.OnNetworkMessage += (lobbyId, userId, channelId, data) =>
            {
                Console.WriteLine("network message: {0} {1} {2} {3}", lobbyId, userId, channelId, Encoding.UTF8.GetString(data));
            };
            lobbyManager.OnSpeaking += (lobbyID, userID, speaking) =>
            {
                Console.WriteLine("lobby speaking: {0} {1} {2}", lobbyID, userID, speaking);
            };

            UpdateActivity();


            /*
            var overlayManager = discord.GetOverlayManager();
            overlayManager.OnOverlayLocked += locked =>
            {
                Console.WriteLine("Overlay Locked: {0}", locked);
            };
            overlayManager.SetLocked(false);
            */

            var storageManager = discord.GetStorageManager();
            var contents = new byte[20000];
            var random = new Random();
            random.NextBytes(contents);
            Console.WriteLine("storage path: {0}", storageManager.GetPath());
            storageManager.WriteAsync("foo", contents, res =>
            {
                var files = storageManager.Files();
                foreach (var file in files)
                {
                    Console.WriteLine("file: {0} size: {1} last_modified: {2}", file.Filename, file.Size, file.LastModified);
                }
                storageManager.ReadAsyncPartial("foo", 400, 50, (result, data) =>
                {
                    Console.WriteLine("partial contents of foo match {0}", Enumerable.SequenceEqual(data, new ArraySegment<byte>(contents, 400, 50)));
                });
                storageManager.ReadAsync("foo", (result, data) =>
                {
                    Console.WriteLine("length of contents {0} data {1}", contents.Length, data.Length);
                    Console.WriteLine("contents of foo match {0}", Enumerable.SequenceEqual(data, contents));
                    Console.WriteLine("foo exists? {0}", storageManager.Exists("foo"));
                    storageManager.Delete("foo");
                    Console.WriteLine("post-delete foo exists? {0}", storageManager.Exists("foo"));
                });
            });

            var storeManager = discord.GetStoreManager();
            storeManager.OnEntitlementCreate += (ref Discord.Entitlement entitlement) =>
            {
                Console.WriteLine("Entitlement Create1: {0}", entitlement.Id);
            };

            // Start a purchase flow.
            // storeManager.StartPurchase(487507201519255552, result =>
            // {
            //     if (result == Discord.Result.Ok)
            //     {
            //         Console.WriteLine("Purchase Complete");
            //     }
            //     else
            //     {
            //         Console.WriteLine("Purchase Canceled");
            //     }
            // });

            // Get all entitlements.
            storeManager.FetchEntitlements(result =>
            {
                if (result == Discord.Result.Ok)
                {
                    foreach (var entitlement in storeManager.GetEntitlements())
                    {
                        Console.WriteLine("entitlement: {0} - {1} {2}", entitlement.Id, entitlement.Type, entitlement.SkuId);
                    }
                }
            });

            // Get all SKUs.
            storeManager.FetchSkus(result =>
            {
                if (result == Discord.Result.Ok)
                {
                    foreach (var sku in storeManager.GetSkus())
                    {
                        Console.WriteLine("sku: {0} - {1} {2}", sku.Name, sku.Price.Amount, sku.Price.Currency);
                    }
                }
            });
            updater = new Thread(UpdaterThread);
            updater.Start();
        }

        public void Unload(ILua lua)
        {
            stopThread = true;
            updater.Join();
            if (!disposed)
            {
                discord.Dispose();
                disposed = true;
            }
        }
    }
}
