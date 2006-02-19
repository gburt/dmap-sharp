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
using System.Collections.Specialized;
using System.Net;
using System.Net.Sockets;
using System.Web;

#if ENABLE_MDNSD
using Mono.Zeroconf;
#else
using Avahi;
#endif

namespace DAAP {

    internal delegate bool WebHandler (Socket client, string path, NameValueCollection query);

    internal class WebServer {

        private const int ChunkLength = 8192;

        private UInt16 port;
        private Socket server;
        private WebHandler handler;
        private bool running;
        private ArrayList creds = new ArrayList ();
        private ArrayList clients = new ArrayList ();
        private string realm;
        private AuthenticationMethod authMethod = AuthenticationMethod.None;
        
        public ushort RequestedPort {
            get { return port; }
            set { port = value; }
        }

        public ushort BoundPort {
            get { return (ushort) (server.LocalEndPoint as IPEndPoint).Port; }
        }

        public NetworkCredential[] Credentials {
            get { return (NetworkCredential[]) creds.ToArray (typeof (NetworkCredential)); }
        }

        public AuthenticationMethod AuthenticationMethod {
            get { return authMethod; }
            set { authMethod = value; }
        }

        public string Realm {
            get { return realm; }
            set { realm = value; }
        }
        
        public WebServer (UInt16 port, WebHandler handler) {
            this.port = port;
            this.handler = handler;
        }

        public void Start () {
            server = new Socket (AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP);
            server.Bind (new IPEndPoint (IPAddress.Any, port));
            server.Listen (10);

            running = true;
            Thread thread = new Thread (ServerLoop);
            thread.IsBackground = true;
            thread.Start ();
        }

        public void Stop () {
            running = false;
            
            if (server != null) {
                server.Close ();
                server = null;
            }

            foreach (Socket client in (ArrayList) clients.Clone ()) {
                // do not pass go, do not collect $200...
                client.Close ();
            }
        }

        public void AddCredential (NetworkCredential cred) {
            creds.Add (cred);
        }

        public void RemoveCredential (NetworkCredential cred) {
            creds.Remove (cred);
        }
        
        public void WriteResponse (Socket client, ContentNode node) {
            WriteResponse (client, HttpStatusCode.OK,
                           ContentWriter.Write (ContentCodeBag.Default, node));
        }

        public void WriteResponse (Socket client, HttpStatusCode code, string body) {
            WriteResponse (client, code, Encoding.UTF8.GetBytes (body));
        }
        
        public void WriteResponse (Socket client, HttpStatusCode code, byte[] body) {
            if (!client.Connected)
                return;
            
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("HTTP/1.1 {0} {1}\r\n", (int) code, code.ToString ())));
                writer.Write (Encoding.UTF8.GetBytes ("DAAP-Server: daap-sharp\r\n"));
                writer.Write (Encoding.UTF8.GetBytes ("Content-Type: application/x-dmap-tagged\r\n"));
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Length: {0}\r\n", body.Length)));
                writer.Write (Encoding.UTF8.GetBytes ("\r\n"));
                writer.Write (body);
            }
        }

        public void WriteResponseFile (Socket client, string file) {
            FileInfo info = new FileInfo (file);

            WriteResponseStream (client, info.Open (FileMode.Open, FileAccess.Read), info.Length);
        }

        public void WriteResponseStream (Socket client, Stream response, long len) {
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {
                
                writer.Write (Encoding.UTF8.GetBytes ("HTTP/1.1 200 OK\r\n"));
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Length: {0}\r\n", len)));
                writer.Write (Encoding.UTF8.GetBytes ("\r\n"));

                using (BinaryReader reader = new BinaryReader (response)) {
                    long count = 0;
                    while (count < len) {
                        byte[] buf = reader.ReadBytes (Math.Min (ChunkLength, (int) len - (int) count));
                        writer.Write (buf);
                        count += buf.Length;
                    }
                }
            }
        }

        public void WriteAccessDenied (Socket client) {
            string msg = "Authorization Required";
            
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {
                writer.Write (Encoding.UTF8.GetBytes ("HTTP/1.1 401 Denied\r\n"));
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("WWW-Authenticate: Basic realm=\"{0}\"",
                                                                     realm)));
                writer.Write (Encoding.UTF8.GetBytes ("Content-Type: text/plain\r\n"));
                writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Length: {0}\r\n", msg.Length)));
                writer.Write (Encoding.UTF8.GetBytes ("\r\n"));
                writer.Write (msg);
            }
        }

        private bool IsValidAuth (string user, string pass) {
            if (authMethod == AuthenticationMethod.None)
                return true;

            foreach (NetworkCredential cred in creds) {

                if ((authMethod != AuthenticationMethod.UserAndPassword || cred.UserName == user) &&
                    cred.Password == pass)
                    return true;
            }

            return false;
        }

        private bool HandleRequest (Socket client) {

            if (!client.Connected)
                return false;
            
            bool ret = true;
            
            using (StreamReader reader = new StreamReader (new NetworkStream (client, false))) {

                string request = reader.ReadLine ();
                if (request == null)
                    return false;
                
                string line = null;
                string user = null;
                string password = null;
                
                // read the rest of the request
                do {
                    line = reader.ReadLine ();
                    
                    if (line == "Connection: close") {
                        ret = false;
                    } else if (line != null && line.StartsWith ("Authorization: Basic")) {
                        string[] splitLine = line.Split (' ');

                        if (splitLine.Length != 3)
                            continue;

                        string userpass = Encoding.UTF8.GetString (Convert.FromBase64String (splitLine[2]));

                        string[] splitUserPass = userpass.Split (new char[] {':'}, 2);
                        user = splitUserPass[0];
                        password = splitUserPass[1];
                    }
                } while (line != String.Empty && line != null);
                
                
                string[] splitRequest = request.Split ();
                if (splitRequest.Length < 3) {
                    WriteResponse (client, HttpStatusCode.BadRequest, "Bad Request");
                } else {
                    try {
                        string path = splitRequest[1];
                        if (!path.StartsWith ("daap://")) {
                            path = String.Format ("daap://localhost/{0}", path);
                        }
                        
                        Uri uri = new Uri (path);
                        NameValueCollection query = new NameValueCollection ();


                        if (uri.Query != null && uri.Query != String.Empty) {
                            string[] splitquery = uri.Query.Substring (1).Split ('&');

                            foreach (string queryItem in splitquery) {
                                if (queryItem == String.Empty)
                                    continue;
                                
                                string[] splitQueryItem = queryItem.Split ('=');
                                query[splitQueryItem[0]] = splitQueryItem[1];
                            }
                        }

                        if (authMethod != AuthenticationMethod.None && uri.AbsolutePath != "/server-info" &&
                            !IsValidAuth (user, password)) {
                            WriteAccessDenied (client);
                            return true;
                        }

                        return handler (client, uri.AbsolutePath, query);
                    } catch (IOException e) {
                        ret = false;
                    } catch (Exception e) {
                        ret = false;
                        Console.Error.WriteLine ("Trouble handling request {0}: {1}", splitRequest[1], e);
                    }
                }
            }

            return ret;
        }

        private void HandleConnection (object o) {
            Socket client = (Socket) o;

            try {
                while (HandleRequest (client)) { }
            } catch (IOException e) {
                // ignore
            } catch (Exception e) {
                Console.Error.WriteLine ("Error handling request: " + e);
            } finally {
                clients.Remove (client);
                client.Close ();
            }
        }

        private void ServerLoop () {
            while (true) {
                try {
                    if (!running)
                        break;
                    
                    Socket client = server.Accept ();
                    clients.Add (client);
                    ThreadPool.QueueUserWorkItem (HandleConnection, client);
                } catch (SocketException e) {
                    break;
                }
            }
        }
    }

    internal class RevisionManager {

        private Hashtable revisions = new Hashtable ();
        private int current = 1;

        public int Current {
            get { return current; }
        }
        
        public void AddRevision (Database[] databases) {
            revisions[++current] = databases;
        }

        public void RemoveRevision (int rev) {
            revisions.Remove (rev);
        }

        public Database[] GetRevision (int rev) {
            if (rev == 0)
                return (Database[]) revisions[current];
            else
                return (Database[]) revisions[rev];
        }

        public Database GetDatabase (int rev, int id) {
            Database[] dbs = GetRevision (rev);

            if (dbs == null)
                return null;
            
            foreach (Database db in dbs) {
                if (db.Id == id)
                    return db;
            }

            return null;
        }
    }

    public class Server {

        internal const int DefaultTimeout = 1800;
        
        private static Regex dbItemsRegex = new Regex ("/databases/([0-9]*?)/items$");
        private static Regex dbSongRegex = new Regex ("/databases/([0-9]*?)/items/([0-9]*).*");
        private static Regex dbContainersRegex = new Regex ("/databases/([0-9]*?)/containers$");
        private static Regex dbContainerItemsRegex = new Regex ("/databases/([0-9]*?)/containers/([0-9]*?)/items$");
        
        private WebServer ws;
        private ArrayList databases = new ArrayList ();
        private ArrayList sessions = new ArrayList ();
        private Random random = new Random ();
        private UInt16 port = 3689;
        private ServerInfo serverInfo = new ServerInfo ();
        private bool publish = true;
        private int maxUsers = 0;
        private bool running;

#if !ENABLE_MDNSD
        private Avahi.Client client;
        private EntryGroup eg;
#else
        private RegisterService zc_service;
#endif

        private object eglock = new object ();
        private RevisionManager revmgr = new RevisionManager ();

        public event EventHandler Collision;

        public string Name {
            get { return serverInfo.Name; }
            set {
                serverInfo.Name = value;
                ws.Realm = value;

                if (publish)
                    RegisterService ();
            }
        }

        public UInt16 Port {
            get { return port; }
            set {
                port = value;
                ws.RequestedPort = value;
            }
        }

        public bool IsPublished {
            get { return publish; }
            set {
                publish = value;

                if (running && publish)
                    RegisterService ();
                else if (running && !publish)
                    UnregisterService ();
            }
        }

        public bool IsRunning {
            get { return running; }
        }

        public AuthenticationMethod AuthenticationMethod {
            get { return serverInfo.AuthenticationMethod; }
            set {
                serverInfo.AuthenticationMethod = value;
                ws.AuthenticationMethod = value;
            }
        }

        public NetworkCredential[] Credentials {
            get { return ws.Credentials; }
        }

        public int MaxUsers {
            get { return maxUsers; }
            set { maxUsers = value; }
        }

        public Server (string name) {
            ws = new WebServer (port, OnHandleRequest);
            serverInfo.Name = name;
            ws.Realm = name;
            
#if !ENABLE_MDNSD
            client = new Avahi.Client ();
#endif
        }

        public void Start () {
            running = true;
            ws.Start ();

#if !ENABLE_MDNSD
            client.StateChanged += OnClientStateChanged;
#endif

            if (publish)
                RegisterService ();
        }

        public void Stop () {
            running = false;

            ws.Stop ();
            UnregisterService ();
                
            // get that thread to wake up and exit
            lock (revmgr) {
                Monitor.PulseAll (revmgr);
            }
        }

        public void AddDatabase (Database db) {
            databases.Add (db);
        }

        public void RemoveDatabase (Database db) {
            databases.Remove (db);
        }

        public void AddCredential (NetworkCredential cred) {
            ws.AddCredential (cred);
        }

        public void RemoveCredential (NetworkCredential cred) {
            ws.RemoveCredential (cred);
        }

        public void Commit () {
            ArrayList clones = new ArrayList ();
            foreach (Database db in databases) {
                clones.Add (db.Clone ());
            }

            lock (revmgr) {
                revmgr.AddRevision ((Database[]) clones.ToArray (typeof (Database)));
                Monitor.PulseAll (revmgr);
            }
        }

#if ENABLE_MDNSD
        private void RegisterService () {
            lock (eglock) {
                if (zc_service != null) {
                    UnregisterService ();
                }
                
                string auth = serverInfo.AuthenticationMethod == AuthenticationMethod.None ? "false" : "true";
                
                zc_service = new RegisterService (serverInfo.Name, null, "_daap._tcp");
                zc_service.Port = (short)ws.BoundPort;
                zc_service.TxtRecord = new TxtRecord ();
                zc_service.TxtRecord.Add ("Password", auth);
                zc_service.TxtRecord.Add ("Machine Name", serverInfo.Name);
                zc_service.TxtRecord.Add ("txtvers", "1");
                zc_service.Response += OnRegisterServiceResponse;
                zc_service.AutoRename = false;
                zc_service.RegisterAsync ();
            }
        }
        
        private void UnregisterService () {
            lock (eglock) {
                if (zc_service == null) {
                    return;
                }
                
                zc_service.Dispose ();
                zc_service = null;
            }
        }
        
        private void OnRegisterServiceResponse (object o, RegisterServiceEventArgs args) {
            if (args.NameConflict && Collision != null) {
                Collision (this, new EventArgs ());
            }
        }
#else
        private void OnClientStateChanged (object o, ClientStateArgs args) {
            if (publish && args.State == ClientState.Running) {
                RegisterService ();
            }
        }
        
        private void RegisterService () {
            lock (eglock) {
                
                if (eg != null) {
                    eg.Reset ();
                } else {
                    eg = new EntryGroup (client);
                    eg.StateChanged += OnEntryGroupStateChanged;
                }

                try {
                    string auth = serverInfo.AuthenticationMethod == AuthenticationMethod.None ? "false" : "true";
                    eg.AddService (serverInfo.Name, "_daap._tcp", "", ws.BoundPort,
                                   new string[] { "Password=" + auth, "Machine Name=" + serverInfo.Name,
                                                  "txtvers=1" });
                    eg.Commit ();
                } catch (ClientException e) {
                    if (e.ErrorCode == ErrorCode.Collision && Collision != null) {
                        Collision (this, new EventArgs ());
                    } else {
                        throw e;
                    }
                }
            }
        }

        private void UnregisterService () {
            lock (eglock) {
                if (eg == null)
                    return;

                eg.Reset ();
                eg.Dispose ();
                eg = null;
            }
        }

        private void OnEntryGroupStateChanged (object o, EntryGroupStateArgs args) {
            if (args.State == EntryGroupState.Collision && Collision != null) {
                Collision (this, new EventArgs ());
            }
        }
#endif

        internal bool OnHandleRequest (Socket client, string path, NameValueCollection query) {

            int session = 0;
            if (query["session-id"] != null) {
                session = Int32.Parse (query["session-id"]);
            }

            if (session == 0 && path != "/server-info" && path != "/content-codes" &&
                path != "/login") {
                ws.WriteResponse (client, HttpStatusCode.Forbidden, "invalid session id");
                return true;
            }

            int clientRev = 0;
            if (query["revision-number"] != null) {
                clientRev = Int32.Parse (query["revision-number"]);
            }

            int delta = 0;
            if (query["delta"] != null) {
                delta = Int32.Parse (query["delta"]);
            }

            if (path == "/server-info") {
                ws.WriteResponse (client, GetServerInfoNode ());
            } else if (path == "/content-codes") {
                ws.WriteResponse (client, ContentCodeBag.Default.ToNode ());
            } else if (path == "/login") {
                if (maxUsers > 0 && sessions.Count + 1 > maxUsers) {
                    ws.WriteResponse (client, HttpStatusCode.ServiceUnavailable, "too many users");
                    return true;
                }
                
                session = random.Next ();
                sessions.Add (session);
                ws.WriteResponse (client, GetLoginNode (session));
            } else if (path == "/logout") {
                sessions.Remove (session);
                ws.WriteResponse (client, HttpStatusCode.OK, new byte[0]);
                return false;
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
                        foreach (Song song in olddb.Songs) {
                            if (curdb.LookupSongById (song.Id) == null)
                                deletedIds.Add (song.Id);
                        }
                    }
                }

                ContentNode node = curdb.ToSongsNode (query["meta"].Split (','),
                                                      (int[]) deletedIds.ToArray (typeof (int)));
                ws.WriteResponse (client, node);
            } else if (dbSongRegex.IsMatch (path)) {
                Match match = dbSongRegex.Match (path);
                int dbid = Int32.Parse (match.Groups[1].Value);
                int songid = Int32.Parse (match.Groups[2].Value);

                Database db = revmgr.GetDatabase (clientRev, dbid);
                if (db == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid database id");
                    return true;
                }

                Song song = db.LookupSongById (songid);
                if (song == null) {
                    ws.WriteResponse (client, HttpStatusCode.BadRequest, "invalid song id");
                    return true;
                }

                try {
                    if (song.FileName != null) {
                        ws.WriteResponseFile (client, song.FileName);
                    } else if (db.Client != null) {
                        long songLength = 0;
                        Stream songStream = db.StreamSong (song, out songLength);
                        
                        try {
                            ws.WriteResponseStream (client, songStream, songLength);
                        } catch (IOException e) {
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
                            Song[] oldplSongs = oldpl.Songs;
                            for (int i = 0; i < oldplSongs.Length; i++) {
                                int id = oldpl.GetContainerId (i);
                                if (curpl.LookupIndexByContainerId (id) < 0) {
                                    deletedIds.Add (id);
                                }
                            }
                        }
                    }
                }
                    
                ws.WriteResponse (client, curpl.ToSongsNode ((int[]) deletedIds.ToArray (typeof (int))));
            } else if (path == "/update") {
                int retrev;
                
                lock (revmgr) {
                    // if they have the current revision, wait for a change
                    if (clientRev == revmgr.Current) {
                        Monitor.Wait (revmgr);
                    }

                    retrev = revmgr.Current;
                }

                if (!running) {
                    ws.WriteResponse (client, HttpStatusCode.NotFound, "server has been stopped");
                } else {
                    ws.WriteResponse (client, GetUpdateNode (retrev));
                }
            } else {
                ws.WriteResponse (client, HttpStatusCode.Forbidden, "GO AWAY");
            }

            return true;
        }

        private ContentNode GetLoginNode (int id) {
            return new ContentNode ("dmap.loginresponse",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.sessionid", id));
        }

        private ContentNode GetServerInfoNode () {
            return serverInfo.ToNode (databases.Count);
        }

        private ContentNode GetDatabasesNode () {
            ArrayList databaseNodes = new ArrayList ();

            Database[] dbs = revmgr.GetRevision (revmgr.Current);
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

        private ContentNode GetUpdateNode (int revision) {
            return new ContentNode ("dmap.updateresponse",
                                    new ContentNode ("dmap.status", 200),
                                    new ContentNode ("dmap.serverrevision", revision));
        }
    }
}
