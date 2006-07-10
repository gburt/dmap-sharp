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
using System.IO;
using System.Collections;

namespace DAAP.Tools {

    public class SampleClient {

        public static int Main (string[] args) {

            if (args.Length == 0 || args[0] == "--help") {
                Console.WriteLine ("Usage: sample-client <host> [<track_id> <track_id> ...]");
                Console.WriteLine ("Pass a track id of 'ALL' to download all tracks.");
                return 1;
            }

            ushort port = 3689;

            if (Environment.GetEnvironmentVariable ("PORT") != null)
                port = UInt16.Parse (Environment.GetEnvironmentVariable ("PORT"));
            
            Client client = new Client (args[0], port);

            if (client.AuthenticationMethod == AuthenticationMethod.None) {
                client.Login ();
            } else {
                string user = null;
                string pass = null;
                
                if (client.AuthenticationMethod == AuthenticationMethod.UserAndPassword) {
                    Console.Write ("Username for '{0}': ", client.Name);
                    user = Console.ReadLine ();
                }
                
                Console.Write ("Password for '{0}': ", client.Name);
                pass = Console.ReadLine ();

                client.Login (user, pass);
            }

            try {
                Console.WriteLine ("Server: " + client.Name);
                
                if (args.Length > 1) {
                    for (int i = 1; i < args.Length; i++) {
                        
                        foreach (Database db in client.Databases) {
                            if (args[i] == "ALL") {
                                for(int j = 0; j < db.TrackCount; j++) {
                                    Console.WriteLine ("Downloading ({0} of {1}): {2}", j + 1, db.TrackCount,
                                                       db.TrackAt(j).Title);
                                    DownloadTrack (db, db.TrackAt(j));
                                }
                            } else {
                            
                                int id = Int32.Parse (args[i]);
                                Track track = db.LookupTrackById (id);
                                
                                if (track == null) {
                                    Console.WriteLine ("WARNING: no track with id '{0}' was found.", id);
                                    continue;
                                }
                                
                                Console.WriteLine ("Downloading: " + track.Title);
                                DownloadTrack (db, track);
                            }
                        }
                    }
                } else {
                    foreach (Database db in client.Databases) {
                        Console.WriteLine ("Database: " + db.Name);
                        
                        foreach (Track track in db.Tracks)
                            Console.WriteLine (track);

                        foreach (Playlist pl in db.Playlists) {
                            Console.WriteLine ("Playlist: " + pl.Name);

                            foreach (Track track in pl.Tracks) {
                                Console.WriteLine (track);
                            }
                        }
                    }
                }
            } finally {
                client.Logout ();
            }
            
            return 0;
        }

        private static void DownloadTrack (Database db, Track track) {
            string artist = "Unknown";
            string album = "Unknown";
            string title = "Unknown";

            if (track.Artist != null && track.Artist != String.Empty)
                artist = track.Artist;

            if (track.Album != null && track.Album != String.Empty)
                album = track.Album;

            if (track.Title != null && track.Title != String.Empty)
                title = track.Title;
            
            string dir = Path.Combine (artist, album);

            Directory.CreateDirectory (dir);
            db.DownloadTrack (track,
                             Path.Combine (dir, String.Format ("{0} - {1}.{2}", artist, title, track.Format)));
        }
    }
}

