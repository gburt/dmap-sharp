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
                db.SongAdded += OnSongAdded;
                db.SongRemoved += OnSongRemoved;
                db.PlaylistAdded += OnPlaylistAdded;
                db.PlaylistRemoved += OnPlaylistRemoved;

                foreach (Playlist pl in db.Playlists) {
                    pl.SongAdded += OnPlaylistSongAdded;
                    pl.SongRemoved += OnPlaylistSongRemoved;
                    pl.NameChanged += OnPlaylistNameChanged;
                }

                foreach (Song song in db.Songs) {
                    song.Updated += OnSongUpdated;
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

        private static void OnSongUpdated (object o, EventArgs args) {
            Console.WriteLine ("Song '{0}' was updated.", (o as Song).Title);
        }

        private static void OnSongAdded (object o, Song song) {
            Console.WriteLine ("Song '{0}' added to '{1}'", song.Title, (o as Database).Name);
        }

        private static void OnSongRemoved (object o, Song song) {
            Console.WriteLine ("Song '{0}' removed from '{1}'", song.Title, (o as Database).Name);
        }

        private static void OnPlaylistAdded (object o, Playlist pl) {
            Console.WriteLine ("Playlist '{0}' added to '{1}'", pl.Name, (o as Database).Name);
            pl.SongAdded += OnPlaylistSongAdded;
            pl.SongRemoved += OnPlaylistSongRemoved;
        }

        private static void OnPlaylistRemoved (object o, Playlist pl) {
            Console.WriteLine ("Playlist '{0}' removed from '{1}'", pl.Name, (o as Database).Name);
        }

        private static void OnPlaylistNameChanged (object o, EventArgs args) {
            Console.WriteLine ("Playlist name changed to '{0}'", (o as Playlist).Name);
        }

        private static void OnPlaylistSongAdded (object o, int index, Song song) {
            Console.WriteLine ("Song '{0}' added to '{1}' at {2}", song.Title, (o as Playlist).Name,
                               index);
        }

        private static void OnPlaylistSongRemoved (object o, int index, Song song) {
            Console.WriteLine ("Song '{0}' removed from '{1}' at {2}", song.Title, (o as Playlist).Name,
                               index);
        }
    }
}
