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

using Dmap;

namespace Daap
{
    public class Track : ITrack, ICloneable
    {
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
        private int discNumber;
        private int discCount;
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

        public int DiscNumber {
            get { return discNumber; }
            set {
                discNumber = value;
                EmitUpdated ();
            }
        }

        public int DiscCount {
            get { return discCount; }
            set {
                discCount = value;
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
            Track track = new Track ();
            track.artist = artist;
            track.album = album;
            track.title = title;
            track.year = year;
            track.format = format;
            track.duration = duration;
            track.id = id;
            track.size = size;
            track.genre = genre;
            track.trackNumber = trackNumber;
            track.trackCount = trackCount;
            track.fileName = fileName;
            track.dateAdded = dateAdded;
            track.dateModified = dateModified;
            track.bitrate = bitrate;

            return track;
        }

        public override string ToString () {
            return String.Format ("{0} - {1}.{2} ({3}): {4}", artist, title, format, duration, id);
        }

        internal void SetId (int id) {
            this.id = id;
        }

        internal static Track FromNode (ContentNode node) {
            Track track = new Track ();

            foreach (ContentNode field in (ContentNode[]) node.Value) {
                switch (field.Name) {
                case "dmap.itemid":
                    track.id = (int) field.Value;
                    break;
                case "daap.songartist":
                    track.artist = (string) field.Value;
                    break;
                case "dmap.itemname":
                    track.title = (string) field.Value;
                    break;
                case "daap.songalbum":
                    track.album = (string) field.Value;
                    break;
                case "daap.songtime":
                    track.duration = TimeSpan.FromMilliseconds ((int) field.Value);
                    break;
                case "daap.songformat":
                    track.format = (string) field.Value;
                    break;
                case "daap.songgenre":
                    track.genre = (string) field.Value;
                    break;
                case "daap.songsize":
                    track.size = (int) field.Value;
                    break;
                case "daap.songtrackcount":
                    track.trackCount = (short) field.Value;
                    break;
                case "daap.songtracknumber":
                    track.trackNumber = (short) field.Value;
                    break;
                case "daap.bitrate":
                    track.bitrate = (short) field.Value;
                    break;
                case "daap.songdateadded":
                    track.dateAdded = (DateTime) field.Value;
                    break;
                case "daap.songdatemodified":
                    track.dateModified = (DateTime) field.Value;
                    break;
                case "daap.songdiscnumber":
                    track.discNumber = (short) field.Value;
                    break;
                case "daap.songdisccount":
                    track.discCount = (short) field.Value;
                    break;
                default:
                    break;
                }
            }

            return track;
        }

        internal static void FromPlaylistNode (Database db, ContentNode node, out Track track, out int containerId) {
            track = null;
            containerId = 0;

            foreach (ContentNode field in (ContentNode[]) node.Value) {
                switch (field.Name) {
                case "dmap.itemid":
                    track = db.LookupTrackById ((int) field.Value);
                    break;
                case "dmap.containeritemid":
                    containerId = (int) field.Value;
                    break;
                default:
                    break;
                }
            }
        }

        private bool Equals (Track track) {
            return artist == track.Artist &&
                album == track.Album &&
                title == track.Title &&
                year == track.Year &&
                format == track.Format &&
                duration == track.Duration &&
                size == track.Size &&
                genre == track.Genre &&
                trackNumber == track.TrackNumber &&
                trackCount == track.TrackCount &&
                dateAdded == track.DateAdded &&
                dateModified == track.DateModified &&
                bitrate == track.BitRate;
        }

        internal void Update (Track track) {
            if (Equals (track))
                return;

            artist = track.Artist;
            album = track.Album;
            title = track.Title;
            year = track.Year;
            format = track.Format;
            duration = track.Duration;
            size = track.Size;
            genre = track.Genre;
            trackNumber = track.TrackNumber;
            trackCount = track.TrackCount;
            dateAdded = track.DateAdded;
            dateModified = track.DateModified;
            bitrate = track.BitRate;

            EmitUpdated ();
        }

        private void EmitUpdated () {
            if (Updated != null)
                Updated (this, new EventArgs ());
        }
    }
}
