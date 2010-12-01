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

    /*public interface Player
    {
        Status  Status { get; }
        Shuffle Shuffle { get; }
        Repeat  Repeat { get; }

        Stream GetCoverArt (int width, int height);
    }*/

    public class Server<D, P, T> : DatabaseServer<D, P, T>
        where D : IDatabase<P, T>
        where P : IPlaylist<T>
        where T : ITrack
    {
        public Server (string name) : base (name)
        {
            ZeroconfType = "_touch-able._tcp";
        }

        static Regex ctrl_int = new Regex ("/ctrl-int/([0-9]+)/(.+)$", RegexOptions.Compiled);
        static Regex browse = new Regex ("/databases/(\\d+)/browse/(\\w+)$", RegexOptions.Compiled);

        protected override bool HandleRequest (Socket client, string username, string path, NameValueCollection query, int range, int delta, int clientRev)
        {
            var match = ctrl_int.Match (path);
            if (match.Success) {
                int dbid = Int32.Parse (match.Groups[1].Value);
                string cmd = match.Groups[2].Value;

                switch (cmd) {
                case "playstatusupdate":
                    int status = (int)Status.Playing;
                    int shuffle = (int)Shuffle.On;
                    int repeat = (int)Repeat.Single;
                    string track_name = "Foo Title";
                    string track_artist = "Foo Artist";
                    string track_album = "Foo Album";
                    long album_id = 2;
                    int remaining = 1000*50;
                    int total_duration = 1000*70;

                    var play_status_node = new ContentNode ("dmcp.status",
                        new ContentNode ("dmcp.mediarevision", (int) revision),
                        new ContentNode ("dacp.state", (int) status),
                        new ContentNode ("dacp.shuffle", (int) shuffle),
                        new ContentNode ("dacp.repeat", (int) repeat),
                        // new ContentNode ("dacp.nowplaying", ContentType.LongLong); dbId; playlistId, playlistItemId, itemId
                        // new ContentNode ("dacp.albumshuffle", ContentType.Long);
                        new ContentNode ("dacp.nowplayingname", track_name),
                        new ContentNode ("dacp.nowplayingartist", track_artist),
                        new ContentNode ("dacp.nowplayingalbum", track_album),
                        new ContentNode ("dacp.nowplayinggenre", "Rock"),
                        new ContentNode ("daap.songalbumid", album_id),
                        new ContentNode ("dacp.remainingtime", (int) remaining),
                        new ContentNode ("dacp.songtime", (int) total_duration)
                    );
                    ws.WriteResponse (client, play_status_node);
                    return true;

                case "nowplayingartwork":
                    //int width = query["mw"]
                    //int height = query["mh"]
                    string file = "/home/gabe/artwork.jpg";
                    ws.WriteResponseFile (client, file, 0);
                    return true;

                case "playpause":
                    break;

                case "previtem":
                    break;

                case "nextitem":
                    break;

                case "playspec":
                    // playspec?database-spec='dmap.persistentid:16621530181618731553'&playlist-spec='dmap.persistentid:9378496334192532210'&dacp.shufflestate=1&session-id=514488449
                    break;

                case "items":
                    // items?session-id=%s&meta=dmap.itemname,dmap.itemid,daap.songartist,daap.songalbum,daap.songalbum,daap.songtime,daap.songtracknumber&type=music&sort=album&query='daap.songalbumid:%s'"
                    break;

                case "getproperty":
                    if (query["properties"] == "dmcp.volume") {
                        ws.WriteResponse (client,
                            new ContentNode ("dmcp.getpropertyresponse", 
                                new ContentNode ("dmcp.volume", 50))
                        );
                        return true;
                    }
                    break;

                case "setproperty":
                    /*"dacp.playingtime"
                        dmcp.volume
                        dacp.shufflestate=1
                        dacp.repeatstate
                        dacp.userrating=100&database-spec='dmap.persistentid:16090061681534800669'&playlist-spec='dmap.persistentid:16090061681534800670'&song-spec='dmap.itemid:0x57'*/
                    break;

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
                int dbid = Int32.Parse (match.Groups[1].Value);
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
                        ArtistLookupFunc (offset, limit).Select (a => a.Name)
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

        public Func<int, int, IEnumerable<IArtist>> ArtistLookupFunc { get; set; }

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
