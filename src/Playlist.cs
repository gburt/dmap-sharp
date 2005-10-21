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
using System.Collections;

namespace DAAP {

    public delegate void PlaylistSongHandler (object o, int index, Song song);

    public class Playlist {

        private static int nextid = 1;
        
        private int id;
        private string name = String.Empty;
        private ArrayList songs = new ArrayList ();
        private ArrayList containerIds = new ArrayList ();

        public event PlaylistSongHandler SongAdded;
        public event PlaylistSongHandler SongRemoved;
        public event EventHandler NameChanged;

        public Song this[int index] {
            get {
                if (songs.Count > index)
                    return (Song) songs[index];
                else
                    return null;
            }
            set { songs[index] = value; }
        }
        
        public Song[] Songs {
            get { return (Song[]) songs.ToArray (typeof (Song)); }
        }

        internal int Id {
            get { return id; }
            set { id = value; }
        }

        public string Name {
            get { return name; }
            set {
                name = value;
                if (NameChanged != null)
                    NameChanged (this, new EventArgs ());
            }
        }

        internal Playlist () {
            id = nextid++;
        }

        public Playlist (string name) : this () {
            this.name = name;
        }

        public void InsertSong (int index, Song song) {
            InsertSong (index, song, songs.Count + 1);
        }

        internal void InsertSong (int index, Song song, int id) {
            songs.Insert (index, song);
            containerIds.Insert (index, id);

            if (SongAdded != null)
                SongAdded (this, index, song);
        }

        public void Clear () {
            songs.Clear ();
        }

        public void AddSong (Song song) {
            AddSong (song, songs.Count + 1);
        }
        
        internal void AddSong (Song song, int id) {
            songs.Add (song);
            containerIds.Add (id);

            if (SongAdded != null)
                SongAdded (this, songs.Count - 1, song);
        }

        public void RemoveAt (int index) {
            Song song = (Song) songs[index];
            songs.RemoveAt (index);
            containerIds.RemoveAt (index);
            
            if (SongRemoved != null)
                SongRemoved (this, index, song);
        }

        public bool RemoveSong (Song song) {
            int index;
            bool ret = false;
            
            while ((index = IndexOf (song)) >= 0) {
                ret = true;
                RemoveAt (index);
            }

            return ret;
        }

        public int IndexOf (Song song) {
            return songs.IndexOf (song);
        }

        internal int GetContainerId (int index) {
            return (int) containerIds[index];
        }

        internal ContentNode ToSongsNode (int[] deletedIds) {
            ArrayList songNodes = new ArrayList ();

            for (int i = 0; i < songs.Count; i++) {
                Song song = songs[i] as Song;
                songNodes.Add (song.ToPlaylistNode ((int) containerIds[i]));
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

            if (deletedNodes != null)
                children.Add (new ContentNode ("dmap.deletedidlisting", deletedNodes));
            
            
            return new ContentNode ("daap.playlistsongs", children);
        }

        internal ContentNode ToNode (bool basePlaylist) {

            ArrayList nodes = new ArrayList ();

            nodes.Add (new ContentNode ("dmap.itemid", id));
            nodes.Add (new ContentNode ("dmap.persistentid", (long) id));
            nodes.Add (new ContentNode ("dmap.itemname", name));
            nodes.Add (new ContentNode ("dmap.itemcount", songs.Count));
            if (basePlaylist)
                nodes.Add (new ContentNode ("daap.baseplaylist", (byte) 1));
            
            return new ContentNode ("dmap.listingitem", nodes);
        }

        internal static Playlist FromNode (ContentNode node) {
            Playlist pl = new Playlist ();

            foreach (ContentNode child in (ContentNode[]) node.Value) {
                switch (child.Name) {
                case  "daap.baseplaylist":
                    return null;
                case "dmap.itemid":
                    pl.Id = (int) child.Value;
                    break;
                case "dmap.itemname":
                    pl.Name = (string) child.Value;
                    break;
                default:
                    break;
                }
            }

            return pl;
        }

        internal void Update (Playlist pl) {
            if (pl.Name == name)
                return;

            Name = pl.Name;
        }

        internal int LookupIndexByContainerId (int id) {
            return containerIds.IndexOf (id);
        }
    }

}
