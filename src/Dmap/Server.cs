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

using Mono.Zeroconf;

namespace Dmap
{
    internal delegate bool WebHandler (Socket client, string user, string path, NameValueCollection query, int range);

    internal class WebServer
    {
        private const int ChunkLength = 8192;

        private UInt16 port;
        private Socket server;
        private WebHandler handler;
        private bool running;
        private List<NetworkCredential> creds = new List<NetworkCredential> ();
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

        public IList<NetworkCredential> Credentials {
            get { return new ReadOnlyCollection<NetworkCredential> (creds); }
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

        public void WriteResponseFile (Socket client, string file, long offset) {
            FileInfo info = new FileInfo (file);

            FileStream stream = info.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
            WriteResponseStream (client, stream, info.Length, offset);
        }

        public void WriteResponseStream (Socket client, Stream response, long len) {
            WriteResponseStream (client, response, len, -1);
        }

        public void WriteResponseStream (Socket client, Stream response, long len, long offset) {
            using (BinaryWriter writer = new BinaryWriter (new NetworkStream (client, false))) {

                if (offset > 0) {
                    writer.Write (Encoding.UTF8.GetBytes ("HTTP/1.1 206 Partial Content\r\n"));
                    writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Range: bytes {0}-{1}/{2}\r\n",
                                                                         offset, len, len + 1)));
                    writer.Write (Encoding.UTF8.GetBytes ("Accept-Range: bytes\r\n"));
                    len = len - offset;
                } else {
                    writer.Write (Encoding.UTF8.GetBytes ("HTTP/1.1 200 OK\r\n"));
                }

                writer.Write (Encoding.UTF8.GetBytes (String.Format ("Content-Length: {0}\r\n", len)));
                writer.Write (Encoding.UTF8.GetBytes ("\r\n"));

                using (BinaryReader reader = new BinaryReader (response)) {
                    if (offset > 0) {
                        reader.BaseStream.Seek (offset, SeekOrigin.Begin);
                    }

                    long count = 0;
                    while (count < len) {
                        byte[] buf = reader.ReadBytes (Math.Min (ChunkLength, (int) len - (int) count));
                        if (buf.Length == 0) {
                            break;
                        }
                        
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
                
                Console.WriteLine ("> {0}", request);
                string line = null;
                string user = null;
                string password = null;
                int range = -1;

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
                    } else if (line != null && line.StartsWith ("Range: ")) {
                        // we currently expect 'Range: bytes=<offset>-'
                        string[] splitLine = line.Split ('=');

                        if (splitLine.Length != 2)
                            continue;

                        string rangestr = splitLine[1];
                        if (!rangestr.EndsWith ("-"))
                            continue;

                        try {
                            range = Int32.Parse (rangestr.Substring (0, rangestr.Length - 1));
                        } catch (FormatException) {
                        }
                    }
                } while (line != String.Empty && line != null);
                
                
                string[] splitRequest = request.Split ();
                if (splitRequest.Length < 3) {
                    WriteResponse (client, HttpStatusCode.BadRequest, "Bad Request");
                } else {
                    try {
                        string path = splitRequest[1];
                        if (!path.StartsWith ("daap://")) {
                            path = String.Format ("daap://localhost{0}", path);
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

                        if (authMethod != AuthenticationMethod.None && uri.AbsolutePath == "/login" &&
                            !IsValidAuth (user, password)) {
                            WriteAccessDenied (client);
                            return true;
                        }

                        return handler (client, user, uri.AbsolutePath, query, range);
                    } catch (IOException) {
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
            } catch (IOException) {
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
                } catch (SocketException) {
                    break;
                }
            }
        }
    }

    public abstract class Server
    {
        internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromMinutes (30);
        
        internal WebServer ws;
        private Dictionary<int, User> sessions = new Dictionary<int, User> ();
        private Random random = new Random ();
        private UInt16 port = 3689;
        internal ServerInfo serverInfo = new ServerInfo ();
        private bool publish = true;
        private int maxUsers = 0;
        private bool running;
        private string machineId;

        private RegisterService zc_service;

        private object eglock = new object ();

        public event EventHandler Collision;
        public event UserHandler UserLogin;
        public event UserHandler UserLogout;

        public IList<User> Users {
            get {
                lock (sessions) {
                    return new ReadOnlyCollection<User> (new List<User> (sessions.Values));
                }
            }
        }

        public string Name {
            get { return serverInfo.Name; }
            set {
                serverInfo.Name = value;
                ws.Realm = value;

                if (publish)
                    RegisterService ();
            }
        }

        public string MachineId {
            get { return machineId; }
            set { machineId = value; }
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

        public IList<NetworkCredential> Credentials {
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
        }

        public void Start () {
            running = true;
            ws.Start ();

            if (publish)
                RegisterService ();
        }

        public virtual void Stop () {
            running = false;

            ws.Stop ();
            UnregisterService ();
        }

        public void AddCredential (NetworkCredential cred)
        {
            ws.AddCredential (cred);
        }

        public void RemoveCredential (NetworkCredential cred)
        {
            ws.RemoveCredential (cred);
        }

        protected string ZeroconfType { get; set; }

        private void RegisterService ()
        {
            lock (eglock) {
                if (zc_service != null) {
                    UnregisterService ();
                }
                
                string auth = serverInfo.AuthenticationMethod == AuthenticationMethod.None ? "false" : "true";
                
                zc_service = new RegisterService ();
                zc_service.Name = serverInfo.Name;
                zc_service.RegType = ZeroconfType;
                zc_service.Port = (short)ws.BoundPort;
                zc_service.TxtRecord = new TxtRecord ();
                zc_service.TxtRecord.Add ("Password", auth);
                zc_service.TxtRecord.Add ("Machine Name", serverInfo.Name);

                AddTxtRecords (zc_service.TxtRecord);

                if (machineId != null) {
                    zc_service.TxtRecord.Add ("Machine ID", machineId);
                }
                
                zc_service.TxtRecord.Add ("txtvers", "1");
                zc_service.Response += OnRegisterServiceResponse;
                zc_service.Register ();
            }
        }

        protected virtual void AddTxtRecords (ITxtRecord record)
        {
        }
        
        private void UnregisterService ()
        {
            lock (eglock) {
                if (zc_service == null) {
                    return;
                }
                
                try {
                    zc_service.Dispose ();
                } catch {
                }
                zc_service = null;
            }
        }
        
        private void OnRegisterServiceResponse (object o, RegisterServiceEventArgs args)
        {
            if (args.ServiceError == ServiceErrorCode.AlreadyRegistered && Collision != null) {
                Collision (this, new EventArgs ());
            }
        }

        private void ExpireSessions ()
        {
            lock (sessions) {
                foreach (int s in new List<int> (sessions.Keys)) {
                    User user = sessions[s];
                    
                    if (DateTime.Now - user.LastActionTime > DefaultTimeout) {
                        sessions.Remove (s);
                        OnUserLogout (user);
                    }
                }
            }
        }

        private void OnUserLogin (User user)
        {
            UserHandler handler = UserLogin;
            if (handler != null) {
                try {
                    handler (this, new UserArgs (user));
                } catch (Exception e) {
                    Console.Error.WriteLine ("Exception in UserLogin event handler: " + e);
                }
            }
        }

        private void OnUserLogout (User user)
        {
            UserHandler handler = UserLogout;
            if (handler != null) {
                try {
                    handler (this, new UserArgs (user));
                } catch (Exception e) {
                    Console.Error.WriteLine ("Exception in UserLogout event handler: " + e);
                }
            }
        }

        internal bool OnHandleRequest (Socket client, string username, string path, NameValueCollection query, int range)
        {
            int session = 0;
            if (query["session-id"] != null) {
                session = Int32.Parse (query["session-id"]);
            }

            if (!sessions.ContainsKey (session) && path != "/server-info" && path != "/content-codes" &&
                path != "/login") {
                ws.WriteResponse (client, HttpStatusCode.Forbidden, "invalid session id");
                return true;
            }

            if (session != 0) {
                sessions[session].LastActionTime = DateTime.Now;
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
                ExpireSessions ();
                
                if (maxUsers > 0 && sessions.Count + 1 > maxUsers) {
                    ws.WriteResponse (client, HttpStatusCode.ServiceUnavailable, "too many users");
                    return true;
                }
                
                session = random.Next ();
                User user = new User (DateTime.Now, (client.RemoteEndPoint as IPEndPoint).Address, username);
                
                lock (sessions) {
                    sessions[session] = user;
                }
                
                ws.WriteResponse (client, 
                    new ContentNode ("dmap.loginresponse",
                        new ContentNode ("dmap.status", 200),
                        new ContentNode ("dmap.sessionid", session))
                );
                OnUserLogin (user);
            } else if (path == "/logout") {
                User user = sessions[session];
                
                lock (sessions) {
                    sessions.Remove (session);
                }
                
                ws.WriteResponse (client, HttpStatusCode.OK, new byte[0]);
                OnUserLogout (user);
                
                return false;
            } else if (HandleRequest (client, username, path, query, range, delta, clientRev)) {
                return true;
            } else {
                ws.WriteResponse (client, HttpStatusCode.Forbidden, "GO AWAY");
            }

            return true;
        }

        protected abstract bool HandleRequest (Socket client, string username, string path, NameValueCollection query, int range, int delta, int clientRev);

        internal virtual ContentNode GetServerInfoNode ()
        {
            return serverInfo.ToNode (0);
        }
    }
}
