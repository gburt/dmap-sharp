using System;
using System.Collections.Generic;
using System.Linq;

using Dmap;

namespace Daap
{
    internal static class ContentWriters
    {
        public static ContentNode ContainersNode<D, P, T> (this IEnumerable<D> databases)
            where T : ITrack
            where P : IPlaylist<T>
            where D : IDatabase<P, T>
        {
            var db_nodes = databases.Select (db =>
                new ContentNode ("dmap.listingitem",
                    new ContentNode ("dmap.itemid", db.Id),
                    new ContentNode ("dmap.persistentid", (long) db.Id),
                    new ContentNode ("dmap.itemname", db.Name),
                    new ContentNode ("dmap.itemcount", db.TrackCount),
                    new ContentNode ("dmap.containercount", db.PlaylistCount + 1)
                )
            ).ToArray ();

            return new ContentNode ("daap.serverdatabases",
                new ContentNode ("dmap.status", 200),
                new ContentNode ("dmap.updatetype", (byte) 0),
                new ContentNode ("dmap.specifiedtotalcount", db_nodes.Length),
                new ContentNode ("dmap.returnedcount", db_nodes.Length),
                new ContentNode ("dmap.listing", db_nodes)
            );
        }

        public static ContentNode PlaylistsNode<P, T> (this IDatabase<P, T> db)
            where T : ITrack
            where P : IPlaylist<T>
        {
            var nodes = db.Playlists.Select (pl => {
                return new ContentNode ("dmap.listingitem",
                    new ContentNode ("dmap.itemid", pl.Id),
                    new ContentNode ("dmap.persistentid", (long) pl.Id),
                    new ContentNode ("dmap.parentcontainerid", 0),
                    new ContentNode ("dmap.itemname", pl.Name),
                    new ContentNode ("dmap.itemcount", pl.TrackCount),
                    new ContentNode ("daap.baseplaylist", (byte)(pl.IsBasePlaylist ? 1 : 0))
                ); }
            ).ToArray ();

            return new ContentNode ("daap.databaseplaylists",
                new ContentNode ("dmap.status", 200),
                new ContentNode ("dmap.updatetype", (byte) 0),
                new ContentNode ("dmap.specifiedtotalcount", nodes.Length),
                new ContentNode ("dmap.returnedcount", nodes.Length),
                new ContentNode ("dmap.listing", nodes)
            );
        }

        public static ContentNode ToTracksNode<P, T> (this IDatabase<P, T> db, string[] fields)
            where T : ITrack
            where P : IPlaylist<T>
        {
            var track_nodes = db.Tracks.Select (t => t.ToNode (fields)).ToArray ();

            var children = new List <ContentNode> ();
            children.Add (new ContentNode ("dmap.status", 200));
            children.Add (new ContentNode ("dmap.updatetype", (byte) 0));
            children.Add (new ContentNode ("dmap.specifiedtotalcount", db.TrackCount));
            children.Add (new ContentNode ("dmap.returnedcount", db.TrackCount));
            children.Add (new ContentNode ("dmap.listing", track_nodes));

            return new ContentNode ("daap.databasesongs", children);
        }

        public static ContentNode ToTracksNode<T> (this IPlaylist<T> playlist)
            where T : ITrack
        {
            var track_nodes = new List<ContentNode> ();
            for (int i = 0; i < playlist.TrackCount; i++) {
                playlist[i].ToPlaylistNode (playlist.GetContainerId (i));
            }

            var children = new List<ContentNode> ();
            children.Add (new ContentNode ("dmap.status", 200));
            children.Add (new ContentNode ("dmap.updatetype", (byte) 0));
            children.Add (new ContentNode ("dmap.specifiedtotalcount", playlist.TrackCount));
            children.Add (new ContentNode ("dmap.returnedcount", playlist.TrackCount));
            children.Add (new ContentNode ("dmap.listing", track_nodes.ToArray ()));

            return new ContentNode ("daap.playlistsongs", children);
        }

        public static ContentNode ToPlaylistNode (this ITrack track, int containerId)
        {
            return new ContentNode ("dmap.listingitem",
                new ContentNode ("dmap.itemkind", (byte) 2),
                new ContentNode ("daap.songdatakind", (byte) 0),
                new ContentNode ("dmap.itemid", track.Id),
                new ContentNode ("dmap.containeritemid", containerId),
                new ContentNode ("dmap.itemname", track.Title ?? "")
            );
        }

        public static ContentNode ToNode (this ITrack track, string[] fields)
        {
            var nodes = new List<ContentNode> ();
            foreach (string field in fields) {
                object val = null;

                switch (field) {
                case "dmap.itemid":
                    val = track.Id;
                    break;
                case "dmap.itemname":
                    val = track.Title;
                    break;
                case "dmap.itemkind":
                    val = (byte) 2;
                    break;
                case "dmap.persistentid":
                    val = (long) track.Id;
                    break;
                case "daap.songalbum":
                    val = track.Album;
                    break;
                case "daap.songgrouping":
                    val = String.Empty;
                    break;
                case "daap.songartist":
                    val = track.Artist;
                    break;
                case "daap.songbitrate":
                    val = (short) track.BitRate;
                    break;
                case "daap.songbeatsperminute":
                    val = (short) 0;
                    break;
                case "daap.songcomment":
                    val = String.Empty;
                    break;
                case "daap.songcompilation":
                    val = (byte) 0;
                    break;
                case "daap.songcomposer":
                    val = String.Empty;
                    break;
                case "daap.songdateadded":
                    val = track.DateAdded;
                    break;
                case "daap.songdatemodified":
                    val = track.DateModified;
                    break;
                case "daap.songdisccount":
                    val = (short) track.DiscCount;
                    break;
                case "daap.songdiscnumber":
                    val = (short) track.DiscNumber;
                    break;
                case "daap.songdisabled":
                    val = (byte) 0;
                    break;
                case "daap.songeqpreset":
                    val = String.Empty;
                    break;
                case "daap.songformat":
                    val = track.Format;
                    break;
                case "daap.songgenre":
                    val = track.Genre;
                    break;
                case "daap.songdescription":
                    val = String.Empty;
                    break;
                case "daap.songrelativevolume":
                    val = (int) 0;
                    break;
                case "daap.songsamplerate":
                    val = 0;
                    break;
                case "daap.songsize":
                    val = track.Size;
                    break;
                case "daap.songstarttime":
                    val = 0;
                    break;
                case "daap.songstoptime":
                    val = 0;
                    break;
                case "daap.songtime":
                    val = (int) track.Duration.TotalMilliseconds;
                    break;
                case "daap.songtrackcount":
                    val = (short) track.TrackCount;
                    break;
                case "daap.songtracknumber":
                    val = (short) track.TrackNumber;
                    break;
                case "daap.songuserrating":
                    val = (byte) 0;
                    break;
                case "daap.songyear":
                    val = (short) track.Year;
                    break;
                case "daap.songdatakind":
                    val = (byte) 0;
                    break;
                case "daap.songdataurl":
                    val = String.Empty;
                    break;
                default:
                    break;
                }

                if (val != null) {
                    // iTunes wants this to go first, sigh
                    if (field == "dmap.itemkind")
                        nodes.Insert (0, new ContentNode (field, val));
                    else
                        nodes.Add (new ContentNode (field, val));
                }
            }

            return new ContentNode ("dmap.listingitem", nodes);
        }

    }
}
