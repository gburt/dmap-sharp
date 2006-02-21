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

    public class Song : ICloneable {

        private string artist;
        private string album;
        private string title;
        private int year;
        private string format;
        private TimeSpan duration;
        private int id;
        private int size;
        private string genre;
        private int trackNumber;
        private int trackCount;
        private string fileName;
        private DateTime dateAdded = DateTime.Now;
        private DateTime dateModified = DateTime.Now;
        private short bitrate;

        public event EventHandler Updated;
        
        public string Artist {
            get { return artist; }
            set {
                artist = value;
                EmitUpdated ();
            }
        }
        
        public string Album {
            get { return album; }
            set {
                album = value;
                EmitUpdated ();
            }
        }
        
        public string Title {
            get { return title; }
            set {
                title = value;
                EmitUpdated ();
            }
        }
        
        public int Year {
            get { return year; }
            set {
                year = value;
                EmitUpdated ();
            }
        }
        
        public string Format {
            get { return format; }
            set {
                format = value;
                EmitUpdated ();
            }
        }
        
        public TimeSpan Duration {
            get { return duration; }
            set {
                duration = value;
                EmitUpdated ();
            }
        }
        
        public int Id {
            get { return id; }
        }
        
        public int Size {
            get { return size; }
            set {
                size = value;
                EmitUpdated ();
            }
        }
        
        public string Genre {
            get { return genre; }
            set {
                genre = value;
                EmitUpdated ();
            }
        }
        
        public int TrackNumber {
            get { return trackNumber; }
            set {
                trackNumber = value;
                EmitUpdated ();
            }
        }
        
        public int TrackCount {
            get { return trackCount; }
            set {
                trackCount = value;
                EmitUpdated ();
            }
        }
        
        public string FileName {
            get { return fileName; }
            set {
                fileName = value;
                EmitUpdated ();
            }
        }
        
        public DateTime DateAdded {
            get { return dateAdded; }
            set {
                dateAdded = value;
                EmitUpdated ();
            }
        }
        
        public DateTime DateModified {
            get { return dateModified; }
            set {
                dateModified = value;
                EmitUpdated ();
            }
        }

        public short BitRate {
            get { return bitrate; }
            set { bitrate = value; }
        }

        public object Clone () {
            Song song = new Song ();
            song.artist = artist;
            song.album = album;
            song.title = title;
            song.year = year;
            song.format = format;
            song.duration = duration;
            song.id = id;
            song.size = size;
            song.genre = genre;
            song.trackNumber = trackNumber;
            song.trackCount = trackCount;
            song.fileName = fileName;
            song.dateAdded = dateAdded;
            song.dateModified = dateModified;
            song.bitrate = bitrate;

            return song;
        }

        public override string ToString () {
            return String.Format ("{0} - {1}.{2} ({3}): {4}", artist, title, format, duration, id);
        }

        internal void SetId (int id) {
            this.id = id;
        }

        internal ContentNode ToNode (string[] fields) {

            ArrayList nodes = new ArrayList ();
            
            foreach (string field in fields) {
                object val = null;
                
                switch (field) {
                case "dmap.itemid":
                    val = id;
                    break;
                case "dmap.itemname":
                    val = title;
                    break;
                case "dmap.itemkind":
                    val = (byte) 2;
                    break;
                case "dmap.persistentid":
                    val = (long) id;
                    break;
                case "daap.songalbum":
                    val = album;
                    break;
                case "daap.songgrouping":
                    val = String.Empty;
                    break;
                case "daap.songartist":
                    val = artist;
                    break;
                case "daap.songbitrate":
                    val = (short) bitrate;
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
                    val = dateAdded;
                    break;
                case "daap.songdatemodified":
                    val = dateModified;
                    break;
                case "daap.songdisccount":
                    val = (short) 0;
                    break;
                case "daap.songdiscnumber":
                    val = (short) 0;
                    break;
                case "daap.songdisabled":
                    val = (byte) 0;
                    break;
                case "daap.songeqpreset":
                    val = String.Empty;
                    break;
                case "daap.songformat":
                    val = format;
                    break;
                case "daap.songgenre":
                    val = genre;
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
                    val = size;
                    break;
                case "daap.songstarttime":
                    val = 0;
                    break;
                case "daap.songstoptime":
                    val = 0;
                    break;
                case "daap.songtime":
                    val = (int) duration.TotalMilliseconds;
                    break;
                case "daap.songtrackcount":
                    val = (short) trackCount;
                    break;
                case "daap.songtracknumber":
                    val = (short) trackNumber;
                    break;
                case "daap.songuserrating":
                    val = (byte) 0;
                    break;
                case "daap.songyear":
                    val = (short) year;
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

        internal static Song FromNode (ContentNode node) {
            Song song = new Song ();
            
            foreach (ContentNode field in (ContentNode[]) node.Value) {
                switch (field.Name) {
                case "dmap.itemid":
                    song.id = (int) field.Value;
                    break;
                case "daap.songartist":
                    song.artist = (string) field.Value;
                    break;
                case "dmap.itemname":
                    song.title = (string) field.Value;
                    break;
                case "daap.songalbum":
                    song.album = (string) field.Value;
                    break;
                case "daap.songtime":
                    song.duration = TimeSpan.FromMilliseconds ((int) field.Value);
                    break;
                case "daap.songformat":
                    song.format = (string) field.Value;
                    break;
                case "daap.songgenre":
                    song.genre = (string) field.Value;
                    break;
                case "daap.songsize":
                    song.size = (int) field.Value;
                    break;
                case "daap.songtrackcount":
                    song.trackCount = (short) field.Value;
                    break;
                case "daap.songtracknumber":
                    song.trackNumber = (short) field.Value;
                    break;
                case "daap.bitrate":
                    song.bitrate = (short) field.Value;
                    break;
                case "daap.songdateadded":
                    song.dateAdded = (DateTime) field.Value;
                    break;
                case "daap.songdatemodified":
                    song.dateModified = (DateTime) field.Value;
                    break;
                default:
                    break;
                }
            }

            return song;
        }

        internal ContentNode ToPlaylistNode (int containerId) {
            return new ContentNode ("dmap.listingitem",
                                    new ContentNode ("dmap.itemkind", (byte) 2),
                                    new ContentNode ("daap.songdatakind", (byte) 0),
                                    new ContentNode ("dmap.itemid", Id),
                                    new ContentNode ("dmap.containeritemid", containerId),
                                    new ContentNode ("dmap.itemname", Title == null ? String.Empty : Title));
        }

        internal static void FromPlaylistNode (Database db, ContentNode node, out Song song, out int containerId) {
            song = null;
            containerId = 0;
            
            foreach (ContentNode field in (ContentNode[]) node.Value) {
                switch (field.Name) {
                case "dmap.itemid":
                    song = db.LookupSongById ((int) field.Value);
                    break;
                case "dmap.containeritemid":
                    containerId = (int) field.Value;
                    break;
                default:
                    break;
                }
            }
        }

        private bool Equals (Song song) {
            return artist == song.Artist &&
                album == song.Album &&
                title == song.Title &&
                year == song.Year &&
                format == song.Format &&
                duration == song.Duration &&
                size == song.Size &&
                genre == song.Genre &&
                trackNumber == song.TrackNumber &&
                trackCount == song.TrackCount &&
                dateAdded == song.DateAdded &&
                dateModified == song.DateModified &&
                bitrate == song.BitRate;
        }

        internal void Update (Song song) {
            if (Equals (song))
                return;

            artist = song.Artist;
            album = song.Album;
            title = song.Title;
            year = song.Year;
            format = song.Format;
            duration = song.Duration;
            size = song.Size;
            genre = song.Genre;
            trackNumber = song.TrackNumber;
            trackCount = song.TrackCount;
            dateAdded = song.DateAdded;
            dateModified = song.DateModified;
            bitrate = song.BitRate;

            EmitUpdated ();
        }

        private void EmitUpdated () {
            if (Updated != null)
                Updated (this, new EventArgs ());
        }
    }
}
