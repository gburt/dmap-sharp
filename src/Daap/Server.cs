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

using Dmap;

namespace Daap
{
    public class Server : Dmap.Server
    {
        internal RevisionManager revmgr = new RevisionManager ();

        private static Regex dbItemsRegex = new Regex ("/databases/([0-9]*?)/items$");
        private static Regex dbTrackRegex = new Regex ("/databases/([0-9]*?)/items/([0-9]*).*");
        private static Regex dbContainersRegex = new Regex ("/databases/([0-9]*?)/containers$");
        private static Regex dbContainerItemsRegex = new Regex ("/databases/([0-9]*?)/containers/([0-9]*?)/items$");

        private ArrayList databases = new ArrayList ();
        
        public event TrackRequestedHandler TrackRequested;

        public Server (string name) : base (name)
        {
            ZeroconfType = "_daap._tcp";
        }

        public override void Stop ()
        {
            base.Stop ();

            // get that thread to wake up and exit
            lock (revmgr) {
                Monitor.PulseAll (revmgr);
            }
        }

        public void AddDatabase (Database db)
        {
            databases.Add (db);
        }

        public void RemoveDatabase (Database db)
        {
            databases.Remove (db);
        }

        public void Commit ()
        {
            List<Database> clones = new List<Database> ();
            foreach (Database db in databases) {
                clones.Add ((Database) db.Clone ());
            }

            lock (revmgr) {
                revmgr.AddRevision (clones);
                Monitor.PulseAll (revmgr);
            }
        }

        protected override bool HandleRequest (Socket client, string username, string path, NameValueCollection query, int range, int delta, int clientRev)
        {
            if (path == "/update") {
                int retrev;
                
                lock (revmgr) {
                    // if they have the current revision, wait for a change
                    if (clientRev == revmgr.Current) {
                        Monitor.Wait (revmgr);
                    }

                    retrev = revmgr.Current;
                }

                if (!IsRunning) {
                    ws.WriteResponse (client, HttpStatusCode.NotFound, "server has been stopped");
                } else {
                    ws.WriteResponse (client, GetUpdateNode (retrev));
                }
            } else if (path == "/databases") {
                ws.WriteResponse (client, GetDatabasesNode ());
            } else if (dbItemsRegex.IsMatch (path)) {
                int dbid = Int32.Parse (dbItemsRegex.Match (path).Groups[1].Value);

                Database curdb = revmgr.GetDatabase (clientRev, dbid);

                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                ArrayList deletedIds = new ArrayList ();

                if (delta > 0) {
                    Database olddb = revmgr.GetDatabase (clientRev - delta, dbid);

                    if (olddb != null) {
                        foreach (Track track in olddb.Tracks) {
                            if (curdb.LookupTrackById (track.Id) == null)
                                deletedIds.Add (track.Id);
                        }
                    }
                }

                ContentNode node = curdb.ToTracksNode (query["meta"].Split (','),
                                                      (int[]) deletedIds.ToArray (typeof (int)));
                ws.WriteResponse (client, node);
            } else if (dbTrackRegex.IsMatch (path)) {
                Match match = dbTrackRegex.Match (path);
                int dbid = Int32.Parse (match.Groups[1].Value);
                int trackid = Int32.Parse (match.Groups[2].Value);

                Database db = revmgr.GetDatabase (clientRev, dbid);
                if (db == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                Track track = db.LookupTrackById (trackid);
                if (track == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid track id");
                    return true;
                }

                try {
                    try {
                        if (TrackRequested != null)
                            TrackRequested (this, new TrackRequestedArgs (username,
                                                                        (client.RemoteEndPoint as IPEndPoint).Address,
                                                                        db, track));
                    } catch {}
                    
                    if (track.FileName != null) {
                        ws.WriteResponseFile (client, track.FileName, range);
                    } else if (db.Client != null) {
                        long trackLength = 0;
                        Stream trackStream = db.StreamTrack (track, out trackLength);
                        
                        try {
                            ws.WriteResponseStream (client, trackStream, trackLength);
                        } catch (IOException) {
                        }
                    } else {
                        ws.WriteResponse (client, HttpStatusCode.InternalServerError, "no file");
                    }
                } finally {
                    client.Close ();
                }
            } else if (dbContainersRegex.IsMatch (path)) {
                int dbid = Int32.Parse (dbContainersRegex.Match (path).Groups[1].Value);

                Database db = revmgr.GetDatabase (clientRev, dbid);
                if (db == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                ws.WriteResponse (client, db.ToPlaylistsNode ());
            } else if (dbContainerItemsRegex.IsMatch (path)) {
                Match match = dbContainerItemsRegex.Match (path);
                int dbid = Int32.Parse (match.Groups[1].Value);
                int plid = Int32.Parse (match.Groups[2].Value);

                Database curdb = revmgr.GetDatabase (clientRev, dbid);
                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                Playlist curpl = curdb.LookupPlaylistById (plid);
                if (curdb == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid playlist id");
                    return true;
                }

                ArrayList deletedIds = new ArrayList ();
                if (delta > 0) {
                    Database olddb = revmgr.GetDatabase (clientRev - delta, dbid);

                    if (olddb != null) {
                        Playlist oldpl = olddb.LookupPlaylistById (plid);

                        if (oldpl != null) {
                            IList<Track> oldplTracks = oldpl.Tracks;
                            for (int i = 0; i < oldplTracks.Count; i++) {
                                int id = oldpl.GetContainerId (i);
                                if (curpl.LookupIndexByContainerId (id) < 0) {
                                    deletedIds.Add (id);
                                }
                            }
                        }
                    }
                }
                    
                ws.WriteResponse (client, curpl.ToTracksNode ((int[]) deletedIds.ToArray (typeof (int))));
            } else {
                return false;
            }

            return true;
        }

        internal override ContentNode GetServerInfoNode ()
        {
            return serverInfo.ToNode (databases.Count);
        }


        private ContentNode GetDatabasesNode ()
        {
            ArrayList databaseNodes = new ArrayList ();

            List<Database> dbs = revmgr.GetRevision (revmgr.Current);
            if (dbs != null) {
                foreach (Database db in revmgr.GetRevision (revmgr.Current)) {
                    databaseNodes.Add (db.ToDatabaseNode ());
                }
            }

            ContentNode node = new ContentNode ("daap.serverdatabases",
                                                new ContentNode ("dmap.status", 200),
                                                new ContentNode ("dmap.updatetype", (byte) 0),
                                                new ContentNode ("dmap.specifiedtotalcount", databases.Count),
                                                new ContentNode ("dmap.returnedcount", databases.Count),
                                                new ContentNode ("dmap.listing", databaseNodes));

            return node;
        }
    }

    public class TrackRequestedArgs : EventArgs {

        private string user;
        private IPAddress host;
        private Database db;
        private Track track;

        public string UserName {
            get { return user; }
        }

        public IPAddress Host {
            get { return host; }
        }

        public Database Database {
            get { return db; }
        }

        public Track Track {
            get { return track; }
        }
        
        public TrackRequestedArgs (string user, IPAddress host, Database db, Track track) {
            this.user = user;
            this.host = host;
            this.db = db;
            this.track = track;
        }
    }

    public delegate void TrackRequestedHandler (object o, TrackRequestedArgs args);

    internal class RevisionManager {

        private Dictionary<int, List<Database>> revisions = new Dictionary<int, List<Database>> ();
        private int current = 1;
        private int limit = 3;

        public int Current {
            get { return current; }
        }

        public int HistoryLimit {
            get { return limit; }
            set { limit = value; }
        }
        
        public void AddRevision (List<Database> databases) {
            revisions[++current] = databases;

            if (revisions.Keys.Count > limit) {
                // remove the oldest

                int oldest = current;
                foreach (int rev in revisions.Keys) {
                    if (rev < oldest) {
                        oldest = rev;
                    }
                }

                RemoveRevision (oldest);
            }
        }

        public void RemoveRevision (int rev) {
            revisions.Remove (rev);
        }

        public List<Database> GetRevision (int rev) {
            if (rev == 0)
                return revisions[current];
            else
                return revisions[rev];
        }

        public Database GetDatabase (int rev, int id) {
            List<Database> dbs = GetRevision (rev);

            if (dbs == null)
                return null;
            
            foreach (Database db in dbs) {
                if (db.Id == id)
                    return db;
            }

            return null;
        }
    }
}
