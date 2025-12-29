using System;
using System.Collections.Generic;
using Leadr.Internal;

namespace Leadr.Models
{
    public class Board
    {
        public string Id { get; private set; }
        public string AccountId { get; private set; }
        public string GameId { get; private set; }
        public string Name { get; private set; }
        public string Slug { get; private set; }
        public string ShortCode { get; private set; }
        public string Icon { get; private set; }
        public string Unit { get; private set; }
        public bool IsActive { get; private set; }
        public bool IsPublished { get; private set; }
        public string SortDirection { get; private set; }
        public string KeepStrategy { get; private set; }
        public List<string> Tags { get; private set; }
        public string Description { get; private set; }
        public DateTime? StartsAt { get; private set; }
        public DateTime? EndsAt { get; private set; }
        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        internal static Board FromJson(Dictionary<string, object> json)
        {
            if (json == null)
                return null;

            return new Board
            {
                Id = json.GetString("id"),
                AccountId = json.GetString("account_id"),
                GameId = json.GetString("game_id"),
                Name = json.GetString("name"),
                Slug = json.GetString("slug"),
                ShortCode = json.GetString("short_code"),
                Icon = json.GetString("icon"),
                Unit = json.GetString("unit"),
                IsActive = json.GetBool("is_active"),
                IsPublished = json.GetBool("is_published"),
                SortDirection = json.GetString("sort_direction"),
                KeepStrategy = json.GetString("keep_strategy"),
                Tags = json.GetStringList("tags"),
                Description = json.GetString("description"),
                StartsAt = json.GetDateTime("starts_at"),
                EndsAt = json.GetDateTime("ends_at"),
                CreatedAt = json.GetDateTimeRequired("created_at"),
                UpdatedAt = json.GetDateTimeRequired("updated_at")
            };
        }
    }
}
