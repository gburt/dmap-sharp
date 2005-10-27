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
using DAAP;
using Entagged;

public class SampleServer
{
    private static int nextId = 10;
    
    public static void Main (string[] args) {
        Server server = new Server ("Test Music");
        server.Port = 3891;
        Database db = new Database ("Test Music");

        foreach (string arg in args)
            AddDirectory (db, arg);

        Playlist pl = new Playlist ("foo playlist");
        foreach (Song song in db.Songs) {
            pl.AddSong (song);
        }

        db.AddPlaylist (pl);

        Console.WriteLine ("Done adding files");
        server.AddDatabase (db);
        server.Start ();
        Console.ReadLine ();
    }

    private static void AddDirectory (Database db, string dir) {
        Console.WriteLine ("Adding files in: " + dir);
        foreach (string file in Directory.GetFiles (dir)) {
            AudioFileWrapper afw = null;

            try {
                afw = new AudioFileWrapper (file);
            } catch (Exception e) {
                continue;
            }

            Song song = new Song ();
            song.Artist = afw.Artist;
            song.Album = afw.Album;
            song.Title = afw.Title;
            song.Year = afw.Year;
            song.Format = Path.GetExtension (file).Substring (1);
            song.Duration = TimeSpan.FromSeconds (afw.Duration);
            song.Id = nextId++;
            song.Genre = afw.Genre;
            song.TrackNumber = afw.TrackNumber;
            song.TrackCount = afw.TrackCount;
            song.DateAdded = DateTime.Now;
            song.DateModified = DateTime.Now;
            song.FileName = file;
            song.Size = (int) new FileInfo (song.FileName).Length;

            db.AddSong (song);
        }

        foreach (string subdir in Directory.GetDirectories (dir)) {
            AddDirectory (db, subdir);
        }
    }
}