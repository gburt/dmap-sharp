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

namespace DAAP.Tools {

    public class SampleClient {

        public static int Main (string[] args) {

            if (args.Length == 0 || args[0] == "--help") {
                Console.WriteLine ("Usage: sample-client <host> [<song_id> <song_id> ...]");
                return 1;
            }

            Client client = new Client (args[0], 3689);
            client.Login ();

            try {
                Console.WriteLine ("Server: " + client.Name);
                
                if (args.Length > 1) {
                    for (int i = 1; i < args.Length; i++) {
                        
                        foreach (Database db in client.Databases) {
                            int id = Int32.Parse (args[i]);
                            Song song = db.LookupSongById (id);
                            
                            if (song == null) {
                                Console.WriteLine ("WARNING: no song with id '{0}' was found.", id);
                                continue;
                            }
                            
                            Console.WriteLine ("Downloading: " + song.Title);
                            db.DownloadSong (song,
                                             String.Format ("./{0} - {1}.{2}", song.Artist, song.Title, song.Format));
                        }
                    }
                } else {
                    foreach (Database db in client.Databases) {
                        Console.WriteLine ("Database: " + db.Name);
                        
                        foreach (Song song in db.Songs)
                            Console.WriteLine (song);
                    }
                }
            } finally {
                client.Logout ();
            }
            
            return 0;
        }
    }
}

