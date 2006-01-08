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

using System;
using System.Net;
using System.IO;
using System.Collections;
using System.Threading;

namespace DAAP {

    public delegate void SongHandler (object o, Song song);
    public delegate void PlaylistHandler (object o, Playlist pl);

    public class Database : ICloneable {

        private const int ChunkLength = 8192;
        private const string SongQuery = "meta=dmap.itemid,dmap.itemname,dmap.itemkind,dmap.persistentid," +
                                         "daap.songalbum,daap.songgrouping,daap.songartist,daap.songbitrate," +
                                         "daap.songbeatsperminute,daap.songcomment,daap.songcodectype," +
                                         "daap.songcodecsubtype,daap.songcompilation,daap.songcomposer," +
                                         "daap.songdateadded,daap.songdatemodified,daap.songdisccount," +
                                         "daap.songdiscnumber,daap.songdisabled,daap.songeqpreset," +
                                         "daap.songformat,daap.songgenre,daap.songdescription," +
                                         "daap.songsamplerate,daap.songsize,daap.songstarttime," +
                                         "daap.songstoptime,daap.songtime,daap.songtrackcount," +
                                         "daap.songtracknumber,daap.songuserrating,daap.songyear," +
                                         "daap.songdatakind,daap.songdataurl,com.apple.itunes.norm-volume," +
                                         "com.apple.itunes.itms-songid,com.apple.itunes.itms-artistid," +
                                         "com.apple.itunes.itms-playlistid,com.apple.itunes.itms-composerid," +
                                         "com.apple.itunes.itms-genreid";

        private static int nextid = 1;
        private Client client;
        private int id;
        private long persistentId;
        private string name;
        private ArrayList songs = new ArrayList ();
        private ArrayList playlists = new ArrayList ();
        private Playlist basePlaylist = new Playlist ();
        private int nextSongId = 1;

        public event SongHandler SongAdded;
        public event SongHandler SongRemoved;
        public event PlaylistHandler PlaylistAdded;
        public event PlaylistHandler PlaylistRemoved;

        public int Id {
            get { return id; }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                basePlaylist.Name = value;
            }
        }
        
        public IEnumerable Songs {
            get { return songs; }
        }
        
        public int SongCount {
            get { return songs.Count; }
        }

        public Song SongAt(int index)
        {
            return songs[index] as Song;
        }

        public Playlist[] Playlists {
            get { return (Playlist[]) playlists.ToArray (typeof (Playlist)); }
        }

        internal Client Client {
            get { return client; }
        }

        private Database () {
            this.id = nextid++;
        }

        public Database (string name) : this () {
            this.Name = name;
        }

        internal Database (Client client, ContentNode dbNode) : this () {
            this.client = client;

            Parse (dbNode);
        }

        private void Parse (ContentNode dbNode) {
            foreach (ContentNode item in (ContentNode[]) dbNode.Value) {

                switch (item.Name) {
                case "dmap.itemid":
                    id = (int) item.Value;
                    break;
                case "dmap.persistentid":
                    persistentId = (long) item.Value;
                    break;
                case "dmap.itemname":
                    name = (string) item.Value;
                    break;
                default:
                    break;
                }
            }
        }

        public Song LookupSongById (int id) {
            foreach (Song song in songs) {
                if (song.Id == id)
                    return song;
            }

            return null;
        }

        public Playlist LookupPlaylistById (int id) {
            if (id == basePlaylist.Id)
                return basePlaylist;

            foreach (Playlist pl in playlists) {
                if (pl.Id == id)
                    return pl;
            }

            return null;
        }

        internal ContentNode ToSongsNode (string[] fields, int[] deletedIds) {

            ArrayList songNodes = new ArrayList ();
            foreach (Song song in songs) {
                songNodes.Add (song.ToNode (fields));
            }

            ArrayList deletedNodes = null;

            if (deletedIds.Length > 0) {
                deletedNodes = new ArrayList ();
                
                foreach (int id in deletedIds) {
                    deletedNodes.Add (new ContentNode ("dmap.itemid", id));
                }
            }

            ArrayList children = new ArrayList ();
            children.Add (new ContentNode ("dmap.status", 200));
            children.Add (new ContentNode ("dmap.updatetype", deletedNodes == null ? (byte) 0 : (byte) 1));
            children.Add (new ContentNode ("dmap.specifiedtotalcount", songs.Count));
            children.Add (new ContentNode ("dmap.returnedcount", songs.Count));
            children.Add (new ContentNode ("dmap.listing", songNodes));

            if (deletedNodes != null) {
                children.Add (new ContentNode ("dmap.deletedidlisting", deletedNodes));
            }
            
            return new ContentNode ("daap.databasesongs", children);
        }

        internal ContentNode ToPlaylistsNode () {
            ArrayList nodes = new ArrayList ();

            nodes.Add (basePlaylist.ToNode (true));
            
            foreach (Playlist pl in playlists) {
                nodes.Add (pl.ToNode (false));
            }

            return new ContentNode ("daap.databaseplaylists",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.updatetype", (byte) 0),
                                    new ContentNode ("dmap.specifiedtotalcount", nodes.Count),
                                    new ContentNode ("dmap.returnedcount", nodes.Count),
                                    new ContentNode ("dmap.listing", nodes));
        }

        internal ContentNode ToDatabaseNode () {
            return new ContentNode ("dmap.listingitem",
                                    new ContentNode ("dmap.itemid", id),
                                    new ContentNode ("dmap.persistentid", (long) id),
                                    new ContentNode ("dmap.itemname", name),
                                    new ContentNode ("dmap.itemcount", songs.Count),
                                    new ContentNode ("dmap.containercount", playlists.Count + 1));
        }

        public void Clear () {
            if (client != null)
                throw new InvalidOperationException ("cannot clear client databases");

            ClearPlaylists ();
            ClearSongs ();
        }

        private void ClearPlaylists () {
            foreach (Playlist pl in (ArrayList) playlists.Clone ()) {
                RemovePlaylist (pl);
            }
        }

        private void ClearSongs () {
            foreach (Song song in (ArrayList) songs.Clone ()) {
                RemoveSong (song);
            }
        }

        private bool IsUpdateResponse (ContentNode node) {
            return node.Name == "dmap.updateresponse";
        }

        private void RefreshPlaylists (string revquery) {
            byte[] playlistsData = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers", id, revquery));
            ContentNode playlistsNode = ContentParser.Parse (client.Bag, playlistsData);

            if (IsUpdateResponse (playlistsNode))
                return;

            // handle playlist additions/changes
            ArrayList plids = new ArrayList ();
            
            foreach (ContentNode playlistNode in (ContentNode[]) playlistsNode.GetChild ("dmap.listing").Value) {
                Playlist pl = Playlist.FromNode (playlistNode);

                if (pl != null) {
                    plids.Add (pl.Id);
                    Playlist existing = LookupPlaylistById (pl.Id);

                    if (existing == null) {
                        AddPlaylist (pl);
                    } else {
                        existing.Update (pl);
                    }
                }
            }

            // delete playlists that no longer exist
            foreach (Playlist pl in (ArrayList) playlists.Clone ()) {
                if (!plids.Contains (pl.Id)) {
                    RemovePlaylist (pl);
                }
            }

            plids = null;

            // add/remove songs in the playlists
            foreach (Playlist pl in playlists) {
                byte[] playlistSongsData = client.Fetcher.Fetch (String.Format ("/databases/{0}/containers/{1}/items",
                                                                                id, pl.Id), revquery);
                ContentNode playlistSongsNode = ContentParser.Parse (client.Bag, playlistSongsData);

                if (IsUpdateResponse (playlistSongsNode))
                    return;

                if ((byte) playlistSongsNode.GetChild ("dmap.updatetype").Value == 1) {

                    // handle playlist song deletions
                    ContentNode deleteList = playlistSongsNode.GetChild ("dmap.deletedidlisting");

                    if (deleteList != null) {
                        foreach (ContentNode deleted in (ContentNode[]) deleteList.Value) {
                            int index = pl.LookupIndexByContainerId ((int) deleted.Value);

                            if (index < 0)
                                continue;

                            pl.RemoveAt (index);
                        }
                    }
                }

                // add new songs, or reorder existing ones

                int plindex = 0;
                foreach (ContentNode plSongNode in (ContentNode[]) playlistSongsNode.GetChild ("dmap.listing").Value) {
                    Song plsong = null;
                    int containerId = 0;
                    Song.FromPlaylistNode (this, plSongNode, out plsong, out containerId);

                    if (pl[plindex] != null && pl.GetContainerId (plindex) != containerId) {
                        pl.RemoveAt (plindex);
                        pl.InsertSong (plindex, plsong, containerId);
                    } else if (pl[plindex] == null) {
                        pl.InsertSong (plindex, plsong, containerId);
                    }

                    plindex++;
                }
            }
        }

        private void RefreshSongs (string revquery) {
            byte[] songsData = client.Fetcher.Fetch (String.Format ("/databases/{0}/items", id),
                                                     SongQuery + "&" + revquery);
            ContentNode songsNode = ContentParser.Parse (client.Bag, songsData);

            if (IsUpdateResponse (songsNode))
                return;

            // handle song additions/changes
            foreach (ContentNode songNode in (ContentNode[]) songsNode.GetChild ("dmap.listing").Value) {
                Song song = Song.FromNode (songNode);
                Song existing = LookupSongById (song.Id);

                if (existing == null)
                    AddSong (song);
                else
                    existing.Update (song);
            }

            if ((byte) songsNode.GetChild ("dmap.updatetype").Value == 1) {

                // handle song deletions
                ContentNode deleteList = songsNode.GetChild ("dmap.deletedidlisting");

                if (deleteList != null) {
                    foreach (ContentNode deleted in (ContentNode[]) deleteList.Value) {
                        Song song = LookupSongById ((int) deleted.Value);

                        if (song != null)
                            RemoveSong (song);
                    }
                }
            }
        }

        internal void Refresh (int newrev) {
            if (client == null)
                throw new InvalidOperationException ("cannot refresh server databases");

            string revquery = null;

            if (client.Revision != 0)
                revquery = String.Format ("revision-number={0}&delta={1}", newrev, newrev - client.Revision);

            RefreshSongs (revquery);
            RefreshPlaylists (revquery);
        }

        private HttpWebResponse FetchSong (Song song) {
            return client.Fetcher.FetchFile (String.Format ("/databases/{0}/items/{1}.{2}", id, song.Id, song.Format));
        }
        
        public Stream StreamSong (Song song, out long length) {
            HttpWebResponse response = FetchSong (song);
            length = response.ContentLength;
            return response.GetResponseStream ();
        }

        public void DownloadSong (Song song, string dest) {

            HttpWebResponse response = FetchSong (song);
            
            BinaryWriter writer = new BinaryWriter (File.Open (dest, FileMode.Create));

            try {
                using (BinaryReader reader = new BinaryReader (response.GetResponseStream ())) {
                    int count = 0;

                    while (count < response.ContentLength) {
                        byte[] buf = reader.ReadBytes ((int) Math.Min (ChunkLength,
                                                                       response.ContentLength - ChunkLength));
                        writer.Write (buf);
                        count += buf.Length;
                    }
                }
            } finally {
                writer.Close ();
                response.Close ();
            }
        }

        public void AddSong (Song song) {
            if (song.Id == 0)
                song.SetId (nextSongId++);
            
            songs.Add (song);
            basePlaylist.AddSong (song);

            if (SongAdded != null)
                SongAdded (this, song);
        }

        public void RemoveSong (Song song) {
            songs.Remove (song);
            basePlaylist.RemoveSong (song);

            foreach (Playlist pl in playlists) {
                pl.RemoveSong (song);
            }

            if (SongRemoved != null)
                SongRemoved (this, song);
        }

        public void AddPlaylist (Playlist pl) {
            playlists.Add (pl);

            if (PlaylistAdded != null)
                PlaylistAdded (this, pl);
        }

        public void RemovePlaylist (Playlist pl) {
            playlists.Remove (pl);

            if (PlaylistRemoved != null)
                PlaylistRemoved (this, pl);
        }

        private Playlist ClonePlaylist (Database db, Playlist pl) {
            Playlist clonePl = new Playlist (pl.Name);
            clonePl.Id = pl.Id;

            Song[] plsongs = pl.Songs;
            for (int i = 0; i < plsongs.Length; i++) {
                clonePl.AddSong (db.LookupSongById (plsongs[i].Id), pl.GetContainerId (i));
            }

            return clonePl;
        }

        public object Clone () {
            Database db = new Database (this.name);
            db.id = id;
            db.persistentId = persistentId;

            ArrayList cloneSongs = new ArrayList ();
            foreach (Song song in songs) {
                cloneSongs.Add (song.Clone ());
            }

            db.songs = cloneSongs;

            ArrayList clonePlaylists = new ArrayList ();
            foreach (Playlist pl in playlists) {
                clonePlaylists.Add (ClonePlaylist (db, pl));
            }

            db.playlists = clonePlaylists;
            db.basePlaylist = ClonePlaylist (db, basePlaylist);
            return db;
        }
    }
}
