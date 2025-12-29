using System;
using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr.Models
{
    public class Session
    {
        public string DeviceId { get; private set; }
        public string GameId { get; private set; }
        public string AccountId { get; private set; }
        public string Status { get; private set; }
        public int ExpiresIn { get; private set; }

        internal string AccessToken { get; private set; }
        internal string RefreshToken { get; private set; }

        internal static Session FromJson(Dictionary<string, object> json)
        {
            if (json == null)
                return null;

            return new Session
            {
                DeviceId = json.GetString("id"),
                GameId = json.GetString("game_id"),
                AccountId = json.GetString("account_id"),
                Status = json.GetString("status"),
                ExpiresIn = json.GetInt("expires_in"),
                AccessToken = json.GetString("access_token"),
                RefreshToken = json.GetString("refresh_token")
            };
        }
    }
}
