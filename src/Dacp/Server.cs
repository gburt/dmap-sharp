/*
 * daap-sharp
 * Copyright (C) 2005  James Willcox <snorp@snorp.net>
 * 
 * This library is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Lesser General Public
 * License as published by the Free Software Foundation; either
 * version 2.1 of the License, or (at your option) any later version.
 * 
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Lesser General Public License for more details.
 * 
 * You should have received a copy of the GNU Lesser General Public
 * License along with this library; if not, write to the Free Software
 * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
 */

// Digital Audio Control Protocol

using System;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Web;

using Mono.Zeroconf;

using Dmap;

namespace Dacp
{
    public enum Repeat {
        Off = 0,
        Single = 1,
        All = 2
    }

    public enum Shuffle {
        Off = 0,
        On = 1
    }

    public enum Status {
        Paused = 3,
        Playing = 4
    }

    public enum Update {
        Progress = 2,
        State = 3,
        Track = 4,
        Cover = 5
    }

    public interface IArtist
    {
        int Id { get; }
        string Name { get; }
        int TrackCount { get; }
    }

    public interface IPlayer
    {
        int Volume { get; set; }
        Shuffle Shuffle { get; set; }
        Repeat Repeat { get; set; }
        int Position { get; set; }
        int Rating { get; set; }

        Status Status { get; }
        string TrackName { get; }
        string TrackArtist { get; }
        string TrackGenre { get; }
        string TrackAlbum { get; }
        int TrackAlbumId { get; }
        int TrackRemaining { get; }
        int TrackDuration { get; }

        string GetArtworkPath (int w, int h);
        void PlayPause ();
        void Previous ();
        void Next ();
        IEnumerable<IArtist> GetArtists (int offset, int limit);
    }

    public class ClientFinder : IDisposable
    {
        ServiceBrowser browser;

        public ClientFinder ()
        {
            browser = new ServiceBrowser ();
            browser.ServiceAdded += (o, a) => {
                a.Service.Resolved += (o2, a2) => {
                    Console.WriteLine ("Service added: {0} on {1}", a2.Service.FullName, a2.Service.HostTarget);
                };
                a.Service.Resolve ();
            };
            //browser.ServiceRemoved += OnServiceRemoved;
            browser.Browse ("_touch-remote._tcp", null);
        }

        public void Dispose ()
        {
            browser.Dispose ();
        }
    }

    public class Server<D, P, T> : DatabaseServer<D, P, T>
        where D : IDatabase<P, T>
        where P : IPlaylist<T>
        where T : ITrack
    {
        IPlayer player;

        static string [] properties = new string [] { "dacp.playingtime", "dmcp.volume", "dacp.shufflestate", "dacp.repeatstate", "dacp.userrating" };

        public Server (IPlayer player, string name) : base (name)
        {
            this.player = player;
            ZeroconfType = "_touch-able._tcp";
        }

        static Regex ctrl_int = new Regex ("/ctrl-int/([0-9]+)/(.+)$", RegexOptions.Compiled);
        static Regex browse = new Regex ("/databases/(\\d+)/browse/(\\w+)$", RegexOptions.Compiled);

        protected override bool HandleRequest (Socket client, string username, string path, NameValueCollection query, int range, int delta, int clientRev)
        {
            var match = ctrl_int.Match (path);
            if (match.Success) {
                //int dbid = Int32.Parse (match.Groups[1].Value);
                string cmd = match.Groups[2].Value;

                switch (cmd) {
                case "playstatusupdate":
                    var play_status_node = new ContentNode ("dmcp.status",
                        new ContentNode ("dmcp.mediarevision", (int) revision),
                        new ContentNode ("dacp.state", (int) player.Status),
                        new ContentNode ("dacp.shuffle", (int) player.Shuffle),
                        new ContentNode ("dacp.repeat", (int) player.Repeat),
                        // new ContentNode ("dacp.nowplaying", ContentType.LongLong); dbId; playlistId, playlistItemId, itemId
                        // new ContentNode ("dacp.albumshuffle", ContentType.Long);
                        new ContentNode ("dacp.nowplayingname", player.TrackName ?? ""),
                        new ContentNode ("dacp.nowplayingartist", player.TrackArtist ?? ""),
                        new ContentNode ("dacp.nowplayingalbum", player.TrackAlbum ?? ""),
                        new ContentNode ("dacp.nowplayinggenre", player.TrackGenre ?? ""),
                        new ContentNode ("daap.songalbumid", (long)player.TrackAlbumId),
                        new ContentNode ("dacp.remainingtime", (int) player.TrackRemaining),
                        new ContentNode ("dacp.songtime", (int) player.TrackDuration)
                    );
                    ws.WriteResponse (client, play_status_node);
                    return true;

                case "nowplayingartwork":
                    int width = 320, height = 320;
                    Int32.TryParse (query["mw"], out width);
                    Int32.TryParse (query["mh"], out height);
                    string file = player.GetArtworkPath (width, height);
                    if (file != null) {
                        ws.WriteResponseFile (client, file, 0);
                        return true;
                    }
                    break;

                case "playpause":
                    player.PlayPause ();
                    ws.WriteOk (client);
                    return true;

                case "previtem":
                    player.Previous ();
                    ws.WriteOk (client);
                    return true;

                case "nextitem":
                    player.Next ();
                    ws.WriteOk (client);
                    return true;

                case "playspec":
                    // playspec?database-spec='dmap.persistentid:16621530181618731553'&playlist-spec='dmap.persistentid:9378496334192532210'&dacp.shufflestate=1&session-id=514488449
                    break;

                case "items":
                    /*var db = Databases.FirstOrDefault (d => d.Id == dbid);
                    // &type=music
                    ws.WriteResponse (client,
                        db.FindTracks (query["query"], query["sort"])
                          .ToTracksNode (query["meta"].Split (',')));
                    return true;*/
                    break;

                case "getproperty":
                    if (query["properties"] == "dmcp.volume") {
                        return true;
                    }
                    foreach (var prop in properties) {
                        if (query[prop] != null) {
                            int val = 0;
                            switch (prop) {
                                case "dacp.playingtime":  val = player.Position; break;
                                case "dmcp.volume":       val = player.Volume; break;
                                case "dacp.shufflestate": val = (int) player.Shuffle; break;
                                case "dacp.repeatstate":  val = (int) player.Repeat; break;
                                case "dacp.userrating":   val = (int) player.Rating; break;
                            }

                            ws.WriteResponse (client,
                                new ContentNode ("dmcp.getpropertyresponse", 
                                    new ContentNode (prop, val))
                            );
                            return true;
                        }
                    }
                    break;

                case "setproperty":
                    foreach (var prop in properties) {
                        if (query[prop] != null) {
                            int val = Int32.Parse (query[prop]);
                            switch (prop) {
                                case "dacp.playingtime":  player.Position = val; break;
                                case "dmcp.volume":       player.Volume = val; break;
                                case "dacp.shufflestate": player.Shuffle = (Shuffle)val; break;
                                case "dacp.repeatstate":  player.Repeat = (Repeat)val; break;
                                case "dacp.userrating":   player.Rating = val; break;
                            }
                        }
                    }
                    ws.WriteOk (client);
                    return true;

                case "cue":
                    /*string command = query["command"]; // play, clear, add
                    string index = query["index"];
                    string sort = query["sort"]; // album, artist
                    string search_query = query["query"];*/
                    break;
                }
            }

            match = browse.Match (path);
            if (match.Success) {
                //int dbid = Int32.Parse (match.Groups[1].Value);
                //var db = Databases.FirstOrDefault (d => d.Id == dbid);
                string type = match.Groups[2].Value;
                if (type == "artists") {
                    int offset = 0, limit = 50;
                    string index = query["index"];
                    if (!String.IsNullOrEmpty (index)) {
                        var parts = index.Split ('-');
                        if (parts.Length == 1) {
                            // Not sure if this is right
                            //limit = Int32.Parse (parts[0]);
                        } else if (parts.Length == 2) {
                            offset = Int32.Parse (parts[0]);
                            limit = Int32.Parse (parts[1]);
                        }
                    }

                    ws.WriteResponse (client, Browse (
                        "daap.browseartistlisting",
                        player.GetArtists (offset, limit).Select (a => a.Name)
                    ));
                    return true;
                }
            }

            bool ret = base.HandleRequest (client, username, path, query, range, delta, clientRev);
            if (!ret) {
                Console.WriteLine ("Dacp: asked to handle {0} w/ query={1}", path, ToString (query));
            }
            return ret;
        }

        private ContentNode Browse (string type, IEnumerable<string> values)
        {
            var items = values.Select (s => new ContentNode ("dmap.listingitemstring", s)).ToArray ();

            return new ContentNode ("daap.databasebrowse",
                new ContentNode ("dmap.status", 200),
                new ContentNode ("dmap.updatetype", (byte) 0),
                new ContentNode ("dmap.specifiedtotalcount", items.Length),
                new ContentNode ("dmap.returnedcount", items.Length),
                new ContentNode (type, items)
            );
        }


        private static string ToString (NameValueCollection query)
        {
            var sb = new StringBuilder ();
            foreach (var key in query.AllKeys) {
                sb.AppendFormat ("{0}={1}&", key, query[key]);
            }
            return sb.ToString ();
        }

        protected override void AddTxtRecords (ITxtRecord record)
        {
            record.Add ("CtlN", "dmap-sharp");
            record.Add ("OSsi", "0x1F6");
            record.Add ("Ver", "131073");
            record.Add ("DvTy", "iTunes");
            record.Add ("DvSv", "2049");
            //record.Add ("DbId", hash);
        }

        /*internal override ContentNode GetServerInfoNode ()
        {
            return serverInfo.ToNode (databases.Count);
        }*/

    }
}
