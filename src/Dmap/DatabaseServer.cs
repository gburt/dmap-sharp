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
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Threading;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Web;

namespace Dmap
{
    public class DatabaseServer<D, P, T> : Dmap.Server
        where D : IDatabase<P, T>
        where P : IPlaylist<T>
        where T : ITrack
    {
        private static Regex dbItemsRegex = new Regex ("/databases/([0-9]+)/items$", RegexOptions.Compiled);
        private static Regex dbTrackRegex = new Regex ("/databases/([0-9]+)/items/([0-9]*).*", RegexOptions.Compiled);
        private static Regex dbContainersRegex = new Regex ("/databases/([0-9]+)/containers$", RegexOptions.Compiled);
        private static Regex dbContainerItemsRegex = new Regex ("/databases/([0-9]+)/containers/([0-9]*?)/items$", RegexOptions.Compiled);

        private List<D> databases = new List<D> ();
        private AutoResetEvent wait_event = new AutoResetEvent (false);
        protected int revision = 0;
        
        public DatabaseServer (string name) : base (name)
        {
        }

        public override void Stop ()
        {
            base.Stop ();
            wait_event.Set ();
            wait_event.Close ();
        }

        public void AddDatabase (D db)
        {
            databases.Add (db);
        }

        public void RemoveDatabase (D db)
        {
            databases.Remove (db);
        }

        public void Commit ()
        {
            revision++;
            wait_event.Set ();
        }

        protected override bool HandleRequest (Socket client, string username, string path, NameValueCollection query, int range, int delta, int clientRev)
        {
            if (path == "/update") {
                if (clientRev == revision) {
                    wait_event.WaitOne ();
                }

                if (!IsRunning) {
                    ws.WriteResponse (client, HttpStatusCode.NotFound, "server has been stopped");
                } else {
                    ws.WriteResponse (client, 
                        new ContentNode ("dmap.updateresponse",
                            new ContentNode ("dmap.status", 200),
                            new ContentNode ("dmap.serverrevision", revision)));
                }
            } else if (path == "/databases") {
                ws.WriteResponse (client, databases.ContainersNode<D, P, T> ());
            } else if (dbItemsRegex.IsMatch (path)) {
                int dbid = Int32.Parse (dbItemsRegex.Match (path).Groups[1].Value);
                var curdb = databases.FirstOrDefault (db => db.Id == dbid);
                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                ws.WriteResponse (client, curdb.ToTracksNode<P, T> (query["meta"].Split (',')));
            } else if (dbTrackRegex.IsMatch (path)) {
                Match match = dbTrackRegex.Match (path);
                int dbid = Int32.Parse (match.Groups[1].Value);
                int trackid = Int32.Parse (match.Groups[2].Value);

                var db = databases.FirstOrDefault (d => d.Id == dbid);
                if (db == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                var track = db.LookupTrackById (trackid);
                if (track == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid track id");
                    return true;
                }

                try {
                    if (track.FileName != null) {
                        ws.WriteResponseFile (client, track.FileName, range);
                    }/* else if (db.Client != null) {
                        long trackLength = 0;
                        Stream trackStream = db.StreamTrack (track, out trackLength);
                        
                        try {
                            ws.WriteResponseStream (client, trackStream, trackLength);
                        } catch (IOException) {
                        }
                    }*/ else {
                        ws.WriteResponse (client, HttpStatusCode.InternalServerError, "no file");
                    }
                } finally {
                    client.Close ();
                }
            } else if (dbContainersRegex.IsMatch (path)) {
                int dbid = Int32.Parse (dbContainersRegex.Match (path).Groups[1].Value);

                var db = databases.FirstOrDefault (d => d.Id == dbid);
                if (db == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                ws.WriteResponse (client, db.PlaylistsNode ());
            } else if (dbContainerItemsRegex.IsMatch (path)) {
                Match match = dbContainerItemsRegex.Match (path);
                int dbid = Int32.Parse (match.Groups[1].Value);
                int plid = Int32.Parse (match.Groups[2].Value);

                var curdb = databases.FirstOrDefault (db => db.Id == dbid);
                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                var curpl = curdb.LookupPlaylistById (plid);
                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid playlist id");
                    return true;
                }

                ws.WriteResponse (client, curpl.ToTracksNode ());
            } else {
                return false;
            }

            return true;
        }

        internal override ContentNode GetServerInfoNode ()
        {
            return serverInfo.ToNode (databases.Count);
        }
    }
}
