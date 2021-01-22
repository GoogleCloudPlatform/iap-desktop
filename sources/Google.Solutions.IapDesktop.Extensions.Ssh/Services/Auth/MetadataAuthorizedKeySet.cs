using Google.Apis.Compute.v1.Data;
using Google.Apis.Util;
using Google.Solutions.Common.Util;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Google.Solutions.IapDesktop.Extensions.Ssh.Services.Auth
{
    /// <summary>
    /// A set of authorized keys.
    /// See https://cloud.google.com/compute/docs/instances/adding-removing-ssh-keys.
    /// </summary>
    public class MetadataAuthorizedKeySet
    {
        public const string LegacyMetadataKey = "sshKeys";
        public const string MetadataKey = "ssh-keys";

        public IEnumerable<MetadataAuthorizedKey> Keys { get; }

        //---------------------------------------------------------------------
        // Metadata.
        //---------------------------------------------------------------------
        private MetadataAuthorizedKeySet(IEnumerable<MetadataAuthorizedKey> keys)
        {
            this.Keys = keys;
        }

        public static MetadataAuthorizedKeySet FromMetadata(Metadata.ItemsData data)
        {
            Utilities.ThrowIfNull(data, nameof(data));
            if (data.Key != MetadataKey)
            {
                throw new ArgumentException("Not a valid metadata key");
            }

            return new MetadataAuthorizedKeySet(
                data.Value
                    .Split('\n')
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => MetadataAuthorizedKey.Parse(line))
                    .ToList());
        }

        public MetadataAuthorizedKeySet Add(MetadataAuthorizedKey key)
        {
            if (Contains(key))
            {
                return this;
            }
            else
            {
                return new MetadataAuthorizedKeySet(this.Keys.ConcatItem(key));
            }
        }

        public MetadataAuthorizedKeySet RemoveExpiredKeys()
        {
            return new MetadataAuthorizedKeySet(
                this.Keys.Where(k =>
                {
                    if (k is ManagedMetadataAuthorizedKey managed)
                    {
                        return managed.Metadata.ExpireOn >= DateTime.UtcNow;
                    }
                    else
                    {
                        return true;
                    }
                }));
        }

        public bool Contains(MetadataAuthorizedKey key)
        {
            return this.Keys
                .Any(k => k.Key == key.Key &&
                          k.KeyType == key.KeyType &&
                          k.LoginUsername == key.LoginUsername);
        }

        public Metadata.ItemsData ToMetadata()
        {
            return new Metadata.ItemsData()
            {
                Key = MetadataKey,
                Value = ToString()
            };
        }

        public override string ToString()
        {
            return string.Join("\n", this.Keys);
        }
    }
}
