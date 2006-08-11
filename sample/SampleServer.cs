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
using DAAP;
using Entagged;

public class SampleServer
{
    private static Server server;
    
    public static void Main (string[] args) { 
        string server_name = "Sample Server";
        string database_name = "Sample Database";
        ushort port = 3689;
        
        Database db = new Database (database_name);

        for (int i = 0; i < args.Length; i++) {
            if (args[i] == "--port") {
                port = Convert.ToUInt16 (args[++i]);
                continue;    
            }
            
            if (args[i] == "--server-name") {
                server_name = args[++i];
                continue;
            }

            if (args[i] == "--database-name") {
                database_name = args[++i];
                continue;
            }

            if (args[i] == "--help") {
                ShowHelp ();
                return;
            }
            
            AddDirectory (db, args[i]);
        }
        
        db.Name = database_name;
   
           server = new Server (server_name);
        server.Collision += OnCollision;
        server.Port = port;
     
        Playlist pl = new Playlist ("foo playlist");
        foreach (Track track in db.Tracks) {
            pl.AddTrack (track);
        }

        db.AddPlaylist (pl);

        Console.WriteLine ("Done adding files");
        Console.WriteLine ("Starting Server '{0}' on Port {1}", 
            server.Name, server.Port);
        server.AddDatabase (db);
        server.Commit ();
        server.Start ();
        Console.ReadLine ();

        server.Stop ();
    }

    private static void OnCollision (object o, EventArgs args) {
        server.Name = server.Name + " foo";
    }

    private static void AddDirectory (Database db, string dir) {
        Console.WriteLine ("Adding files in: " + dir);
        foreach (string file in Directory.GetFiles (dir)) {
            AudioFile afw = null;

            try {
                afw = new AudioFile (file);
            } catch (Exception e) {
                continue;
            }

            Track track = new Track ();
            track.Artist = afw.Artist;
            track.Album = afw.Album;
            track.Title = afw.Title;
            track.Year = afw.Year;
            track.Format = Path.GetExtension (file).Substring (1);
            track.Duration = afw.Duration;
            track.Genre = afw.Genre;
            track.TrackNumber = afw.TrackNumber;
            track.TrackCount = afw.TrackCount;
            track.DateAdded = DateTime.Now;
            track.DateModified = DateTime.Now;
            track.FileName = file;
            track.Size = (int) new FileInfo (track.FileName).Length;

            db.AddTrack (track);
        }

        foreach (string subdir in Directory.GetDirectories (dir)) {
            AddDirectory (db, subdir);
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage: mono server.exe [ options ... ] <directories ...>");
        Console.WriteLine("       where options include:\n");
        Console.WriteLine("  --help                    Show this help");
        Console.WriteLine("  --server-name <name>      Set the server name");
        Console.WriteLine("  --database-name <name>    Set the database name");
        Console.WriteLine("  --port <port>             Set the server port");
        Console.WriteLine("");
    }
}
