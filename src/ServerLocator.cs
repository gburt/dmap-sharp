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
using System.Text;
using System.Collections;
using Avahi;

namespace DAAP {

    public delegate void ServiceHandler (object o, Service service);

    public struct Service {
        public IPAddress Address;
        public ushort Port;
        public string Name;
        public bool IsProtected;

        public static Service Zero = new Service ();
    }
    
    public class ServiceLocator {

        private Avahi.Client client;
        private ServiceBrowser browser;
        private Hashtable services = new Hashtable ();
        private ArrayList resolvers = new ArrayList ();
        private bool showLocals = false;

        public event ServiceHandler Found;
        public event ServiceHandler Removed;

        public bool ShowLocalServices {
            get { return showLocals; }
            set { showLocals = value; }
        }
        
        public Service[] Services {
            get {
                ArrayList list = new ArrayList ();

                foreach (Service service in services.Values) {
                    list.Add (service);
                }

                return (Service[]) list.ToArray (typeof (Service));
            }
        }
        
        public ServiceLocator () {
            client = new Avahi.Client ();
            browser = new ServiceBrowser (client, "_daap._tcp");
            browser.ServiceAdded += OnServiceAdded;
            browser.ServiceRemoved += OnServiceRemoved;
        }

        private void OnServiceAdded (object o, ServiceInfo service) {
            if ((service.Flags & LookupResultFlags.Local) > 0)
                return;
            
            ServiceResolver resolver = new ServiceResolver (client, service);
            resolvers.Add (resolver);
            resolver.Found += OnServiceResolved;
            resolver.Timeout += OnServiceTimeout;
        }

        private void OnServiceResolved (object o, ServiceInfo service) {

            resolvers.Remove (o);
            (o as ServiceResolver).Dispose ();

            string name = service.Name;
            bool pwRequired = false;

            // iTunes tacks this on to indicate a passsword protected share.  Ugh.
            if (service.Name.EndsWith ("_PW")) {
                name = service.Name.Substring (0, service.Name.Length - 3);
                pwRequired = true;
            }
            
            foreach (byte[] txt in service.Text) {
                string txtstr = Encoding.UTF8.GetString (txt);

                string[] splitstr = txtstr.Split('=');

                if (splitstr.Length < 2)
                    continue;

                if (splitstr[0].ToLower () == "password")
                    pwRequired = splitstr[1].ToLower () == "true";
                else if (splitstr[0].ToLower () == "machine name")
                    name = splitstr[1];
            }

            Service svc;
            svc.Address = service.Address;
            svc.Port = service.Port;
            svc.Name = name;
            svc.IsProtected = pwRequired;

            services[svc.Name] = svc;

            if (Found != null)
                Found (this, svc);
        }

        private void OnServiceTimeout (object o, EventArgs args) {
            Console.Error.WriteLine ("Failed to resolve");
        }

        private void OnServiceRemoved (object o, ServiceInfo service) {
            Service svc = (Service) services[service.Name];
            if (!svc.Equals (Service.Zero)) {
                services.Remove (svc);

                if (Removed != null)
                    Removed (this, svc);
            }
        }
    }
}
