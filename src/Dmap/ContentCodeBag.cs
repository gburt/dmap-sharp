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
using System.Reflection;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace Dmap {

    internal enum ContentType : short {
        Char = 1,
        SignedLong = 2,
        Short = 3,
        Long = 5,
        LongLong = 7,
        String = 9,
        Date = 10,
        Version = 11,
        Container = 12
    }

    internal struct ContentCode {
        public int Number;
        public string Name;
        public ContentType Type;

        public static ContentCode Zero = new ContentCode ();
    }

    internal class ContentCodeBag {

        private const int ChunkLength = 8192;

        private static ContentCodeBag defaultBag;
        private Dictionary <int, ContentCode> codes_by_num = new Dictionary <int, ContentCode> ();
        private Dictionary <string, ContentCode> codes_by_name = new Dictionary <string, ContentCode> ();

        public static ContentCodeBag Default {
            get {
                if (defaultBag == null) {

                    // this is crappy
                    // Alex: Agreed. :)

                    string name = "content-codes";
                    using (BinaryReader reader = new BinaryReader(Assembly.GetExecutingAssembly().GetManifestResourceStream(name))) {
                        MemoryStream buf = new MemoryStream();
                        byte[] bytes = null;

                        do {
                            bytes = reader.ReadBytes(ChunkLength);
                            buf.Write(bytes, 0, bytes.Length);
                        } while (bytes.Length == ChunkLength);

                        defaultBag = ContentCodeBag.ParseCodes(buf.GetBuffer());
                    }
                }

                return defaultBag;
            }
        }

        private ContentCodeBag () {
        }

        public ContentCode Lookup (int number) {
            if (codes_by_num.ContainsKey (number))
                return codes_by_num[number];
            else
                return ContentCode.Zero;
        }

        public ContentCode Lookup (string name) {
            if (codes_by_name.ContainsKey (name))
                return codes_by_name[name];
            else
                return ContentCode.Zero;
        }

        private static int GetIntFormat (string code) {
            return IPAddress.NetworkToHostOrder (BitConverter.ToInt32 (Encoding.ASCII.GetBytes (code), 0));
        }

        internal static string GetStringFormat (int code) {
            return Encoding.ASCII.GetString (BitConverter.GetBytes (IPAddress.HostToNetworkOrder (code)));
        }

        private void AddCode (string num, string name, ContentType type)
        {
            AddCode (new ContentCode () {
                Number = GetIntFormat (num),
                Name = name,
                Type = type
            });
        }

        private void AddCode (ContentCode code)
        {
            if (!codes_by_num.ContainsKey (code.Number)) {
                codes_by_num[code.Number] = code;
            }

            codes_by_name[code.Name] = code;
        }

        internal ContentNode ToNode () {
            List <ContentNode> nodes = new List <ContentNode> ();

            foreach (int number in codes_by_num.Keys) {
                ContentCode code = (ContentCode) codes_by_num[number];

                List <ContentNode> contents = new List <ContentNode> ();
                contents.Add (new ContentNode ("dmap.contentcodesnumber", code.Number));
                contents.Add (new ContentNode ("dmap.contentcodesname", code.Name));
                contents.Add (new ContentNode ("dmap.contentcodestype", code.Type));

                ContentNode dict = new ContentNode ("dmap.dictionary", contents);
                nodes.Add (dict);
            }

            ContentNode status = new ContentNode ("dmap.status", 200);
            return new ContentNode ("dmap.contentcodesresponse", status, nodes);
        }

        public static ContentCodeBag ParseCodes (byte[] buffer) {
            ContentCodeBag bag = new ContentCodeBag ();

            // add some codes to bootstrap us
            bag.AddCode ("mccr", "dmap.contentcodesresponse", ContentType.Container);
            bag.AddCode ("mdcl", "dmap.dictionary", ContentType.Container);
            bag.AddCode ("mcnm", "dmap.contentcodesnumber", ContentType.Long);
            bag.AddCode ("mcna", "dmap.contentcodesname", ContentType.String);
            bag.AddCode ("mcty", "dmap.contentcodestype", ContentType.Short);
            bag.AddCode ("mstt", "dmap.status", ContentType.Long);

            // some photo-specific codes
            bag.AddCode ("ppro", "dpap.protocolversion", ContentType.Long);
            bag.AddCode ("pret", "dpap.blah", ContentType.Container);

            bag.AddCode ("abal", "daap.browsealbumlisting", ContentType.Container);
            bag.AddCode ("abar", "daap.browseartistlisting", ContentType.Container);
            bag.AddCode ("abcp", "daap.browsecomposerlisting", ContentType.Container);
            bag.AddCode ("abgn", "daap.browsegenrelisting", ContentType.Container);
            bag.AddCode ("abpl", "daap.baseplaylist", ContentType.Char);
            bag.AddCode ("abro", "daap.databasebrowse", ContentType.Container);
            bag.AddCode ("adbs", "daap.databasesongs", ContentType.Container);
            bag.AddCode ("aeAI", "com.apple.itunes.itms-artistid", ContentType.Long);
            bag.AddCode ("aeCI", "com.apple.itunes.itms-composerid", ContentType.Long);
            bag.AddCode ("aeCR", "com.apple.itunes.content-rating", ContentType.String);
            bag.AddCode ("aeEN", "com.apple.itunes.episode-num-str", ContentType.String);
            bag.AddCode ("aeES", "com.apple.itunes.episode-sort", ContentType.Long);
            bag.AddCode ("aeFP", "com.apple.itunes.req-fplay", ContentType.Char);
            bag.AddCode ("aeGD", "com.apple.itunes.gapless-enc-dr", ContentType.Long);
            bag.AddCode ("aeGE", "com.apple.itunes.gapless-enc-del", ContentType.Long);
            bag.AddCode ("aeGH", "com.apple.itunes.gapless-heur", ContentType.Long);
            bag.AddCode ("aeGI", "com.apple.itunes.itms-genreid", ContentType.Long);
            bag.AddCode ("aeGR", "com.apple.itunes.gapless-resy", ContentType.LongLong);
            bag.AddCode ("aeGU", "com.apple.itunes.gapless-dur", ContentType.LongLong);
            bag.AddCode ("aeHV", "com.apple.itunes.has-video", ContentType.Char);
            bag.AddCode ("aeMK", "com.apple.itunes.mediakind", ContentType.Char);
            bag.AddCode ("aeNN", "com.apple.itunes.network-name", ContentType.String);
            bag.AddCode ("aeNV", "com.apple.itunes.norm-volume", ContentType.Long);
            bag.AddCode ("aePC", "com.apple.itunes.is-podcast", ContentType.Char);
            bag.AddCode ("aePI", "com.apple.itunes.itms-playlistid", ContentType.Long);
            bag.AddCode ("aePP", "com.apple.itunes.is-podcast-playlist", ContentType.Char);
            bag.AddCode ("aePS", "com.apple.itunes.special-playlist", ContentType.Char);
            bag.AddCode ("aeSF", "com.apple.itunes.itms-storefrontid", ContentType.Long);
            bag.AddCode ("aeSI", "com.apple.itunes.itms-songid", ContentType.Long);
            bag.AddCode ("aeSN", "com.apple.itunes.series-name", ContentType.String);
            bag.AddCode ("aeSP", "com.apple.itunes.smart-playlist", ContentType.Char);
            bag.AddCode ("aeSU", "com.apple.itunes.season-num", ContentType.Long);
            bag.AddCode ("aeSV", "com.apple.itunes.music-sharing-version", ContentType.Long);
            bag.AddCode ("agal", "daap.albumgrouping", ContentType.Container);
            bag.AddCode ("agrp", "daap.songgrouping", ContentType.String);
            bag.AddCode ("aply", "daap.databaseplaylists", ContentType.Container);
            bag.AddCode ("aprm", "daap.playlistrepeatmode", ContentType.Char);
            bag.AddCode ("apro", "daap.protocolversion", ContentType.Version);
            bag.AddCode ("apsm", "daap.playlistshufflemode", ContentType.Char);
            bag.AddCode ("apso", "daap.playlistsongs", ContentType.Container);
            bag.AddCode ("arif", "daap.resolveinfo", ContentType.Container);
            bag.AddCode ("arsv", "daap.resolve", ContentType.Container);
            bag.AddCode ("asaa", "daap.songalbumartist", ContentType.String);
            bag.AddCode ("asai", "daap.songalbumid", ContentType.LongLong);
            bag.AddCode ("asal", "daap.songalbum", ContentType.String);
            bag.AddCode ("asar", "daap.songartist", ContentType.String);
            bag.AddCode ("asbk", "daap.bookmarkable", ContentType.Char);
            bag.AddCode ("asbo", "daap.songbookmark", ContentType.Long);
            bag.AddCode ("asbr", "daap.songbitrate", ContentType.Short);
            bag.AddCode ("asbt", "daap.songbeatsperminute", ContentType.Short);
            bag.AddCode ("ascd", "daap.songcodectype", ContentType.Long);
            bag.AddCode ("ascm", "daap.songcomment", ContentType.String);
            bag.AddCode ("ascn", "daap.songcontentdescription", ContentType.String);
            bag.AddCode ("asco", "daap.songcompilation", ContentType.Char);
            bag.AddCode ("ascp", "daap.songcomposer", ContentType.String);
            bag.AddCode ("ascr", "daap.songcontentrating", ContentType.Char);
            bag.AddCode ("ascs", "daap.songcodecsubtype", ContentType.Long);
            bag.AddCode ("asct", "daap.songcategory", ContentType.String);
            bag.AddCode ("asda", "daap.songdateadded", ContentType.Date);
            bag.AddCode ("asdb", "daap.songdisabled", ContentType.Char);
            bag.AddCode ("asdc", "daap.songdisccount", ContentType.Short);
            bag.AddCode ("asdk", "daap.songdatakind", ContentType.Char);
            bag.AddCode ("asdm", "daap.songdatemodified", ContentType.Date);
            bag.AddCode ("asdn", "daap.songdiscnumber", ContentType.Short);
            bag.AddCode ("asdp", "daap.songdatepurchased", ContentType.Date);
            bag.AddCode ("asdr", "daap.songdatereleased", ContentType.Date);
            bag.AddCode ("asdt", "daap.songdescription", ContentType.String);
            bag.AddCode ("ased", "daap.songextradata", ContentType.Short);
            bag.AddCode ("aseq", "daap.songeqpreset", ContentType.String);
            bag.AddCode ("asfm", "daap.songformat", ContentType.String);
            bag.AddCode ("asgn", "daap.songgenre", ContentType.String);
            bag.AddCode ("asgp", "daap.songgapless", ContentType.Char);
            bag.AddCode ("ashp", "daap.songhasbeenplayed", ContentType.Char);
            bag.AddCode ("asky", "daap.songkeywords", ContentType.String);
            bag.AddCode ("aslc", "daap.songlongcontentdescription", ContentType.String);
            bag.AddCode ("aspu", "daap.songpodcasturl", ContentType.String);
            bag.AddCode ("asrv", "daap.songrelativevolume", ContentType.SignedLong);
            bag.AddCode ("assa", "daap.sortartist", ContentType.String);
            bag.AddCode ("assc", "daap.sortcomposer", ContentType.String);
            bag.AddCode ("assl", "daap.sortalbumartist", ContentType.String);
            bag.AddCode ("assn", "daap.sortname", ContentType.String);
            bag.AddCode ("assp", "daap.songstoptime", ContentType.Long);
            bag.AddCode ("assr", "daap.songsamplerate", ContentType.Long);
            bag.AddCode ("asss", "daap.sortseriesname", ContentType.String);
            bag.AddCode ("asst", "daap.songstarttime", ContentType.Long);
            bag.AddCode ("assu", "daap.sortalbum", ContentType.String);
            bag.AddCode ("assz", "daap.songsize", ContentType.Long);
            bag.AddCode ("astc", "daap.songtrackcount", ContentType.Short);
            bag.AddCode ("astm", "daap.songtime", ContentType.Long);
            bag.AddCode ("astn", "daap.songtracknumber", ContentType.Short);
            bag.AddCode ("asul", "daap.songdataurl", ContentType.String);
            bag.AddCode ("asur", "daap.songuserrating", ContentType.Char);
            bag.AddCode ("asyr", "daap.songyear", ContentType.Short);
            bag.AddCode ("ated", "daap.supportsextradata", ContentType.Short);
            bag.AddCode ("avdb", "daap.serverdatabases", ContentType.Container);
            bag.AddCode ("caar", "dacp.albumrepeat", ContentType.Long);
            bag.AddCode ("caas", "dacp.albumshuffle", ContentType.Long);
            bag.AddCode ("caci", "dacp.controlint", ContentType.Container);
            bag.AddCode ("caia", "dacp.isavailable", ContentType.Char);
            bag.AddCode ("cana", "dacp.nowplayingartist", ContentType.String);
            bag.AddCode ("cang", "dacp.nowplayinggenre", ContentType.String);
            bag.AddCode ("canl", "dacp.nowplayingalbum", ContentType.String);
            bag.AddCode ("cann", "dacp.nowplayingname", ContentType.String);
            bag.AddCode ("canp", "dacp.nowplaying", ContentType.LongLong);
            bag.AddCode ("cant", "dacp.remainingtime", ContentType.Long);
            bag.AddCode ("caps", "dacp.state", ContentType.Long);
            bag.AddCode ("carp", "dacp.repeat", ContentType.Long);
            bag.AddCode ("cash", "dacp.shuffle", ContentType.Long);
            bag.AddCode ("casp", "dacp.speakers", ContentType.Container);
            bag.AddCode ("cass", "dacp.ss", ContentType.Char);
            bag.AddCode ("cast", "dacp.songtime", ContentType.Long);
            bag.AddCode ("casu", "dacp.su", ContentType.Char);
            bag.AddCode ("ceSG", "dacp.sg", ContentType.Char);
            bag.AddCode ("cmcp", "dmcp.controlprompt", ContentType.Container);
            bag.AddCode ("cmgt", "dmcp.getpropertyresponse", ContentType.Container);
            bag.AddCode ("cmik", "dmcp.ik", ContentType.Char);
            bag.AddCode ("cmmk", "dmcp.mediakind", ContentType.Long);
            bag.AddCode ("cmsp", "dmcp.sp", ContentType.Char);
            bag.AddCode ("cmsr", "dmcp.mediarevision", ContentType.Long);
            bag.AddCode ("cmst", "dmcp.status", ContentType.Container);
            bag.AddCode ("cmsv", "dmcp.sv", ContentType.Char);
            bag.AddCode ("cmvo", "dmcp.volume", ContentType.Long);
            bag.AddCode ("mbcl", "dmap.bag", ContentType.Container);
            bag.AddCode ("mccr", "dmap.contentcodesresponse", ContentType.Container);
            bag.AddCode ("mcna", "dmap.contentcodesname", ContentType.String);
            bag.AddCode ("mcnm", "dmap.contentcodesnumber", ContentType.Long);
            bag.AddCode ("mcon", "dmap.container", ContentType.Container);
            bag.AddCode ("mctc", "dmap.containercount", ContentType.Long);
            bag.AddCode ("mcti", "dmap.containeritemid", ContentType.Long);
            bag.AddCode ("mcty", "dmap.contentcodestype", ContentType.Short);
            bag.AddCode ("mdcl", "dmap.dictionary", ContentType.Container);
            bag.AddCode ("medc", "dmap.editdictionary", ContentType.Container);
            bag.AddCode ("meds", "dmap.editstatus", ContentType.Long);
            bag.AddCode ("miid", "dmap.itemid", ContentType.Long);
            bag.AddCode ("mikd", "dmap.itemkind", ContentType.Char);
            bag.AddCode ("mimc", "dmap.itemcount", ContentType.Long);
            bag.AddCode ("minm", "dmap.itemname", ContentType.String);
            bag.AddCode ("mlcl", "dmap.listing", ContentType.Container);
            bag.AddCode ("mlid", "dmap.sessionid", ContentType.Long);
            bag.AddCode ("mlit", "dmap.listingitem", ContentType.Container);
            bag.AddCode ("mlit", "dmap.listingitemstring", ContentType.String);
            bag.AddCode ("mlog", "dmap.loginresponse", ContentType.Container);
            bag.AddCode ("mpco", "dmap.parentcontainerid", ContentType.Long);
            bag.AddCode ("mper", "dmap.persistentid", ContentType.LongLong);
            bag.AddCode ("mpro", "dmap.protocolversion", ContentType.Version);
            bag.AddCode ("mrco", "dmap.returnedcount", ContentType.Long);
            bag.AddCode ("msal", "dmap.supportsautologout", ContentType.Char);
            bag.AddCode ("msas", "dmap.authenticationschemes", ContentType.Long);
            bag.AddCode ("msau", "dmap.authenticationmethod", ContentType.Char);
            bag.AddCode ("msbr", "dmap.supportsbrowse", ContentType.Char);
            bag.AddCode ("msdc", "dmap.databasescount", ContentType.Long);
            bag.AddCode ("msed", "dmap.supportsedit", ContentType.Char);
            bag.AddCode ("msex", "dmap.supportsextensions", ContentType.Char);
            bag.AddCode ("msix", "dmap.supportsindex", ContentType.Char);
            bag.AddCode ("mslr", "dmap.loginrequired", ContentType.Char);
            bag.AddCode ("msma", "dmap.speakermachineaddress", ContentType.LongLong);
            bag.AddCode ("msml", "dmap.speakermachinelist", ContentType.Container);
            bag.AddCode ("mspi", "dmap.supportspersistentids", ContentType.Char);
            bag.AddCode ("msqy", "dmap.supportsquery", ContentType.Char);
            bag.AddCode ("msrs", "dmap.supportsresolve", ContentType.Char);
            bag.AddCode ("msrv", "dmap.serverinforesponse", ContentType.Container);
            bag.AddCode ("mstc", "dmap.utctime", ContentType.Date);
            bag.AddCode ("mstm", "dmap.timeoutinterval", ContentType.Long);
            bag.AddCode ("msts", "dmap.statusstring", ContentType.String);
            bag.AddCode ("mstt", "dmap.status", ContentType.Long);
            bag.AddCode ("msup", "dmap.supportsupdate", ContentType.Char);
            bag.AddCode ("mtco", "dmap.specifiedtotalcount", ContentType.Long);
            bag.AddCode ("mudl", "dmap.deletedidlisting", ContentType.Container);
            bag.AddCode ("mupd", "dmap.updateresponse", ContentType.Container);
            bag.AddCode ("musr", "dmap.serverrevision", ContentType.Long);
            bag.AddCode ("muty", "dmap.updatetype", ContentType.Char);

            ContentNode node = ContentParser.Parse (bag, buffer);

            foreach (ContentNode dictNode in (node.Value as ContentNode[])) {
                if (dictNode.Name != "dmap.dictionary") {
                    continue;
                }

                ContentCode code = new ContentCode ();

                foreach (ContentNode item in (dictNode.Value as ContentNode[])) {
                    switch (item.Name) {
                    case "dmap.contentcodesnumber":
                        code.Number = (int) item.Value;
                        break;
                    case "dmap.contentcodesname":
                        code.Name = (string) item.Value;
                        break;
                    case "dmap.contentcodestype":
                        code.Type = (ContentType) Enum.ToObject (typeof (ContentType), (short) item.Value);
                        break;
                    }
                }

                bag.AddCode (code);
            }

            return bag;
        }
    }
}
