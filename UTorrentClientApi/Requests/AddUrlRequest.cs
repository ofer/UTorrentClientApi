﻿using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using UTorrent.Api.File;
using UTorrent.Api.Tools;

namespace UTorrent.Api
{
    public class AddUrlRequest : BaseAddRequest<AddUrlResponse>
    {
        #region Properties

        public Uri InputUrl { get; protected set; }

        private TorrentInfo _torrentInfo;
        public TorrentInfo TorrentInfo
        {
            get
            {
                return _torrentInfo;
            }
        }

        #endregion

        #region Fluent Setter

        public AddUrlRequest SetUri(Uri uri)
        {
            if (uri == null)
                throw new ArgumentNullException("uri");

            if (string.Equals(uri.Scheme, "magnet", StringComparison.OrdinalIgnoreCase))
            {
                var torrent = TorrentInfo.Parse(uri);
                _torrentInfo = torrent;
            }

            InputUrl = uri;
            return this;
        }

        #endregion

        protected override void ToUrl(StringBuilder sb)
        {
            if (sb == null)
                throw new ArgumentNullException("sb");

            base.ToUrl(sb);

            if (InputUrl != null)
                sb.Append("&s=").Append(InputUrl);

            if (UrlAction == UrlAction.AddUrl && InputUrl == null)
                throw new InvalidOperationException("FileUrl is missing with AddUrl action");
        }

#if !PORTABLE
        protected override void OnProcessingRequest(System.Net.HttpWebRequest wr)
        {
        }
#endif

        protected override void OnProcessedRequest(AddUrlResponse result)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            base.OnProcessedRequest(result);
        }

        protected override bool CheckAction(UrlAction action)
        {
            return (action == UrlAction.AddUrl);
        }

        protected override Data.Torrent FindAddedTorrent(AddUrlResponse result)
        {
            if (result == null)
                throw new ArgumentNullException("result");

            if (InputUrl != null && string.Equals(InputUrl.Scheme, "magnet", StringComparison.OrdinalIgnoreCase))
            {
                //BTIH
                Regex reg = new Regex("BTIH:([A-F0-9]{40})");
                var url = InputUrl.ToString().ToUpperInvariant();
                Match match = reg.Match(url);
                if (match.Success)
                {
                    string hash = match.Groups[1].Value;
                    var torrent = result.Result.Torrents.OrderByDescending(t => t.AddedDate).FirstOrDefault(item => string.Equals(item.Hash, hash, StringComparison.OrdinalIgnoreCase));
                    return torrent;
                }
                else
                {
                    // Base 32 not raw hash
                    reg = new Regex("BTIH:([A-Z0-9]{32})");
                    url = InputUrl.ToString().ToUpperInvariant();
                    match = reg.Match(url);

                    if (!match.Success) return null;

                    var data = Base32Helper.ToBytes(match.Groups[1].Value);
                    string hash = BitConverter.ToString(data).Replace("-", string.Empty);
                    var torrent = result.Result.Torrents.OrderByDescending(t => t.AddedDate).FirstOrDefault(item => string.Equals(item.Hash, hash, StringComparison.OrdinalIgnoreCase));
                    return torrent;
                }
            }

            return null;
        }
    }
}
