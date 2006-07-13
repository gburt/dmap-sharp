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

namespace DAAP.Tools {

    public class UpdateTest {

        private static ArrayList clients = new ArrayList ();
        
        public static void Main (string[] args) {
            if (args.Length == 0) {
                ServiceLocator locator = new ServiceLocator ();
                locator.Found += OnServiceFound;
                locator.Removed += OnServiceRemoved;
                locator.Start ();
            } else {
                string host = args[0];

                ushort port = 3689;
                if (args.Length > 1)
                    port = UInt16.Parse (args[1]);
                    
                Client client = new Client (host, port);
                client.Login ();
                AddClient (client);
            }

            Console.WriteLine ("Press enter to quit");
            Console.ReadLine ();

            foreach (Client client in clients) {
                client.Logout ();
            }
        }

        private static void OnServiceFound (object o, ServiceArgs args) {
            Console.WriteLine ("Found: " + args.Service.Name);

            Client client = new Client (args.Service);
            client.Login ();

            AddClient (client);
        }

        private static void AddClient (Client client) {
            foreach (Database db in client.Databases) {
                db.TrackAdded += OnTrackAdded;
                db.TrackRemoved += OnTrackRemoved;
                db.PlaylistAdded += OnPlaylistAdded;
                db.PlaylistRemoved += OnPlaylistRemoved;

                foreach (Playlist pl in db.Playlists) {
                    pl.TrackAdded += OnPlaylistTrackAdded;
                    pl.TrackRemoved += OnPlaylistTrackRemoved;
                    pl.NameChanged += OnPlaylistNameChanged;
                }

                foreach (Track track in db.Tracks) {
                    track.Updated += OnTrackUpdated;
                }
                
                Console.WriteLine ("Added database: " + db.Name);
            }

            clients.Add (client);
        }

        private static void OnServiceRemoved (object o, ServiceArgs args) {
            Console.WriteLine ("Removed: " + args.Service.Name);

            foreach (Client client in clients) {
                if (client.Name == args.Service.Name) {
                    clients.Remove (client);
                    break;
                }
            }
        }

        private static void OnTrackUpdated (object o, EventArgs args) {
            Console.WriteLine ("Track '{0}' was updated.", (o as Track).Title);
        }

        private static void OnTrackAdded (object o, TrackArgs args) {
            Console.WriteLine ("Track '{0}' added to '{1}'", args.Track.Title, (o as Database).Name);
        }

        private static void OnTrackRemoved (object o, TrackArgs args) {
            Console.WriteLine ("Track '{0}' removed from '{1}'", args.Track.Title, (o as Database).Name);
        }

        private static void OnPlaylistAdded (object o, PlaylistArgs args) {
            Console.WriteLine ("Playlist '{0}' added to '{1}'", args.Playlist.Name, (o as Database).Name);
            args.Playlist.TrackAdded += OnPlaylistTrackAdded;
            args.Playlist.TrackRemoved += OnPlaylistTrackRemoved;
        }

        private static void OnPlaylistRemoved (object o, PlaylistArgs args) {
            Console.WriteLine ("Playlist '{0}' removed from '{1}'", args.Playlist.Name, (o as Database).Name);
        }

        private static void OnPlaylistNameChanged (object o, EventArgs args) {
            Console.WriteLine ("Playlist name changed to '{0}'", (o as Playlist).Name);
        }

        private static void OnPlaylistTrackAdded (object o, int index, Track track) {
            Console.WriteLine ("Track '{0}' added to '{1}' at {2}", track.Title, (o as Playlist).Name,
                               index);
        }

        private static void OnPlaylistTrackRemoved (object o, int index, Track track) {
            Console.WriteLine ("Track '{0}' removed from '{1}' at {2}", track.Title, (o as Playlist).Name,
                               index);
        }
    }
}
