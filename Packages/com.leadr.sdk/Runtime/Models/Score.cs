using System;
using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr.Models
{
    public class Score
    {
        public string Id { get; private set; }
        public string AccountId { get; private set; }
        public string GameId { get; private set; }
        public string BoardId { get; private set; }
        public string PlayerName { get; private set; }
        public double Value { get; private set; }
        public string ValueDisplay { get; private set; }
        public Dictionary<string, object> Metadata { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        internal static Score FromJson(Dictionary<string, object> json)
        {
            if (json == null)
                return null;

            return new Score
            {
                Id = json.GetString("id"),
                AccountId = json.GetString("account_id"),
                GameId = json.GetString("game_id"),
                BoardId = json.GetString("board_id"),
                PlayerName = json.GetString("player_name"),
                Value = json.GetDouble("value"),
                ValueDisplay = json.GetString("value_display"),
                Metadata = json.GetDict("metadata"),
                CreatedAt = json.GetDateTimeRequired("created_at"),
                UpdatedAt = json.GetDateTimeRequired("updated_at")
            };
        }
    }
}
