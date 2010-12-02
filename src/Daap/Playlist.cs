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
using System.Collections.Generic;
using System.Collections.ObjectModel;

using Dmap;

namespace Daap
{
    public delegate void PlaylistTrackHandler (object o, int index, Track track);

    public class Playlist : IPlaylist<Track>
    {
        private static int nextid = 1;

        private int id;
        private string name = String.Empty;
        private List<Track> tracks = new List<Track> ();
        private List<int> containerIds = new List<int> ();

        public event PlaylistTrackHandler TrackAdded;
        public event PlaylistTrackHandler TrackRemoved;
        public event EventHandler NameChanged;

        public Track this[int index] {
            get {
                if (tracks.Count > index)
                    return tracks[index];
                else
                    return null;
            }
            set { tracks[index] = value; }
        }

        public int TrackCount { get { return tracks.Count; } }

        public bool IsBasePlaylist { get; internal set; }

        public IEnumerable<Track> Tracks {
            get { return new ReadOnlyCollection<Track> (tracks); }
        }

        public int Id {
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

        public void InsertTrack (int index, Track track) {
            InsertTrack (index, track, tracks.Count + 1);
        }

        internal void InsertTrack (int index, Track track, int id) {
            tracks.Insert (index, track);
            containerIds.Insert (index, id);

            if (TrackAdded != null)
                TrackAdded (this, index, track);
        }

        public void Clear () {
            tracks.Clear ();
        }

        public void AddTrack (Track track) {
            AddTrack (track, tracks.Count + 1);
        }

        internal void AddTrack (Track track, int id) {
            tracks.Add (track);
            containerIds.Add (id);

            if (TrackAdded != null)
                TrackAdded (this, tracks.Count - 1, track);
        }

        public void RemoveAt (int index) {
            Track track = (Track) tracks[index];
            tracks.RemoveAt (index);
            containerIds.RemoveAt (index);

            if (TrackRemoved != null)
                TrackRemoved (this, index, track);
        }

        public bool RemoveTrack (Track track) {
            int index;
            bool ret = false;

            while ((index = IndexOf (track)) >= 0) {
                ret = true;
                RemoveAt (index);
            }

            return ret;
        }

        public int IndexOf (Track track) {
            return tracks.IndexOf (track);
        }

        public int GetContainerId (int index) {
            return (int) containerIds[index];
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
